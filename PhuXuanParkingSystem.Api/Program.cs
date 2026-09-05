using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PhuXuanParkingSystem.Api.Hubs;
using PhuXuanParkingSystem.Api.Middlewares;
using PhuXuanParkingSystem.Api.Services;
using PhuXuanParkingSystem.Models.Data;
using PhuXuanParkingSystem.Repositories;
using Serilog;
using Serilog.Events;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// Cấu hình chạy ngầm Windows Service (Hỗ trợ graceful shutdown, tự động nhận diện SCM hoặc NSSM)
builder.Host.UseWindowsService();

// Cấu hình Serilog: Ghi log ra Console và File xoay vòng theo ngày trong logs/
var logDir = Path.Combine(AppContext.BaseDirectory, "logs");
if (!Directory.Exists(logDir))
{
    try { Directory.CreateDirectory(logDir); } catch { }
}
var logFilePath = Path.Combine(logDir, "api-.log");

Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .MinimumLevel.Override("Microsoft", LogEventLevel.Warning)
    .MinimumLevel.Override("Microsoft.Hosting.Lifetime", LogEventLevel.Information)
    .Enrich.FromLogContext()
    .WriteTo.Console()
    .WriteTo.File(
        logFilePath,
        rollingInterval: RollingInterval.Day,
        retainedFileCountLimit: 30,
        fileSizeLimitBytes: 10 * 1024 * 1024,
        rollOnFileSizeLimit: true,
        outputTemplate: "[{Timestamp:yyyy-MM-dd HH:mm:ss.fff zzz} {Level:u3}] {Message:lj}{NewLine}{Exception}")
    .CreateLogger();

builder.Host.UseSerilog();

// 1. Cấu hình CSDL MongoDB (Chỉ nạp duy nhất qua builder.Configuration - tự động thừa hưởng từ Biến Môi Trường và appsettings)
var mongoConn = builder.Configuration.GetConnectionString("MongoDb")
    ?? builder.Configuration["MongoDb_ConnectionString"]
    ?? "mongodb://127.0.0.1:27017";

var dbName = builder.Configuration["DatabaseName"]
    ?? builder.Configuration["MongoDb_DatabaseName"]
    ?? "PhuXuanParkingSystemDb";

builder.Services.AddSingleton(new MongoDbContext(mongoConn, dbName));
builder.Services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));
builder.Services.AddScoped(typeof(MongoRepository<>));

// 1.1. Cấu hình Hàng đợi AuditLog Channel & Background Worker
builder.Services.AddSingleton<IAuditLogQueue>(sp =>
{
    var capacity = builder.Configuration.GetValue<int>("AuditLog:ChannelCapacity", 5000);
    return new AuditLogQueue(capacity);
});
builder.Services.AddHostedService<AuditLogBackgroundWorker>();

// 1.2. Cấu hình Background Worker tự động dọn dẹp ảnh Captures cũ
builder.Services.AddHostedService<CapturesCleanupBackgroundWorker>();

// 2. Cấu hình JWT Authentication
var jwtSecret = builder.Configuration["JwtSettings:SecretKey"] ?? "PhuXuanParkingSystem_Super_Secret_Key_2026_For_JWT_Authentication_Secure!";
var jwtIssuer = builder.Configuration["JwtSettings:Issuer"] ?? "PhuXuanParkingSystem.Api";
var jwtAudience = builder.Configuration["JwtSettings:Audience"] ?? "PhuXuanParkingSystem.Web";

builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = JwtBearerDefaults.AuthenticationScheme;
    options.DefaultChallengeScheme = JwtBearerDefaults.AuthenticationScheme;
})
.AddJwtBearer(options =>
{
    options.RequireHttpsMetadata = false;
    options.SaveToken = true;
    options.TokenValidationParameters = new TokenValidationParameters
    {
        ValidateIssuerSigningKey = true,
        IssuerSigningKey = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(jwtSecret)),
        ValidateIssuer = true,
        ValidIssuer = jwtIssuer,
        ValidateAudience = true,
        ValidAudience = jwtAudience,
        ClockSkew = TimeSpan.Zero,
        // Map đúng claim chứa role để User.IsInRole() và [Authorize(Roles)] hoạt động
        RoleClaimType = System.Security.Claims.ClaimTypes.Role
    };
});

// 3. Cấu hình CORS cho Frontend React (Hỗ trợ cả Localhost và toàn bộ dải IP mạng LAN)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.SetIsOriginAllowed(origin => true) // Cho phép tất cả thiết bị/máy tính trong mạng LAN kết nối
              .AllowAnyHeader()
              .AllowAnyMethod()
              .AllowCredentials();
    });
});

builder.Services.AddControllers()
    .AddJsonOptions(options =>
    {
        options.JsonSerializerOptions.Converters.Add(new JsonStringEnumConverter());
    });
builder.Services.AddSignalR();
builder.Services.AddEndpointsApiExplorer();

// 4. Cấu hình Swagger UI có nút Authorize Bearer Token
builder.Services.AddSwaggerGen(c =>
{
    c.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "PhuXuan Parking System API",
        Version = "v1",
        Description = "RESTful API và Realtime Hub cho Cổng Quản Trị Web Admin Bãi Xe Phú Xuân"
    });

    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Name = "Authorization",
        Type = SecuritySchemeType.Http,
        Scheme = "Bearer",
        BearerFormat = "JWT",
        In = ParameterLocation.Header,
        Description = "Nhập JWT Token theo định dạng: Bearer {token}"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "Bearer"
                }
            },
            Array.Empty<string>()
        }
    });
});

var app = builder.Build();

// 5. Cấu hình Middleware xử lý lỗi toàn cục
app.UseMiddleware<GlobalExceptionMiddleware>();

// 6. Cấu hình HTTP Pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(c =>
    {
        c.SwaggerEndpoint("/swagger/v1/swagger.json", "PhuXuan Parking API v1");
        c.RoutePrefix = "swagger";
    });
}

app.UseCors("AllowReactApp");

// 6. Cấu hình Static Files cho thư mục ảnh Captures (Nạp hoàn toàn động từ builder.Configuration, không hardcode)
var capturesFolder = builder.Configuration["CapturesSettings:FolderPath"]
    ?? builder.Configuration["CapturesFolder"]
    ?? builder.Configuration["CaptureSavePath"];

if (!string.IsNullOrWhiteSpace(capturesFolder))
{
    var resolvedCapturesPath = Path.IsPathRooted(capturesFolder)
        ? capturesFolder
        : Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, capturesFolder));

    if (Directory.Exists(resolvedCapturesPath))
    {
        app.UseStaticFiles(new StaticFileOptions
        {
            FileProvider = new PhysicalFileProvider(resolvedCapturesPath),
            RequestPath = "/captures"
        });
    }
}

// 7. Phục vụ Web Admin SPA tĩnh từ wwwroot (nếu có bản build)
app.UseDefaultFiles();
app.UseStaticFiles();

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ParkingRealtimeHub>("/hubs/parking");

// 8. SPA Fallback cho React Router khi truy cập route con hoặc F5
// Chặn Fallback cho các tiền tố kỹ thuật: /api, /captures, /hubs trả về đúng 404 thay vì trả về HTML
app.MapFallback(async context =>
{
    var path = context.Request.Path.Value ?? string.Empty;
    if (path.StartsWith("/api", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/captures", StringComparison.OrdinalIgnoreCase) ||
        path.StartsWith("/hubs", StringComparison.OrdinalIgnoreCase))
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
        context.Response.ContentType = "application/json";
        await context.Response.WriteAsync("{\"success\":false,\"message\":\"Endpoint hoặc tài nguyên không tồn tại.\"}");
        return;
    }

    var indexPath = Path.Combine(app.Environment.ContentRootPath, "wwwroot", "index.html");
    if (File.Exists(indexPath))
    {
        context.Response.ContentType = "text/html";
        await context.Response.SendFileAsync(indexPath);
    }
    else
    {
        context.Response.StatusCode = StatusCodes.Status404NotFound;
    }
});

try
{
    Log.Information("Khởi động PhuXuanParkingSystem.Api thành công.");
    app.Run();
}
catch (Exception ex)
{
    Log.Fatal(ex, "PhuXuanParkingSystem.Api dừng đột ngột do lỗi nghiêm trọng.");
}
finally
{
    Log.CloseAndFlush();
}

public partial class Program { }

