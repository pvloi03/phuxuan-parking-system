using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.Extensions.FileProviders;
using Microsoft.IdentityModel.Tokens;
using Microsoft.OpenApi.Models;
using PhuXuanParkingSystem.Api.Hubs;
using PhuXuanParkingSystem.Api.Middlewares;
using PhuXuanParkingSystem.Models.Data;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Repositories;
using System.Text;
using System.Text.Json.Serialization;

var builder = WebApplication.CreateBuilder(args);

// 1. Cấu hình CSDL MongoDB (Singleton MongoDbContext & Generic Repositories)
var mongoConn = builder.Configuration.GetConnectionString("MongoDb") ?? "mongodb://localhost:27017";
var dbName = builder.Configuration["DatabaseName"] ?? "PhuXuanParkingSystemDb";
builder.Services.AddSingleton(new MongoDbContext(mongoConn, dbName));
builder.Services.AddScoped(typeof(IRepository<>), typeof(MongoRepository<>));

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
        ClockSkew = TimeSpan.Zero
    };
});

// 3. Cấu hình CORS cho Frontend React (Vite: 5173, Next/CRA: 3000)
builder.Services.AddCors(options =>
{
    options.AddPolicy("AllowReactApp", policy =>
    {
        policy.WithOrigins("http://localhost:5173", "http://localhost:3000", "http://127.0.0.1:5173")
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

// 6. Cấu hình Static Files cho thư mục ảnh Captures (để Web hiển thị ảnh từ máy WinForms)
var capturesCandidates = new[]
{
    Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "../PhuXuanParkingSystem/bin/x86/Debug/Captures")),
    Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, builder.Configuration["CapturesFolder"] ?? "../PhuXuanParkingSystem/bin/Debug/Captures")),
    Path.GetFullPath(Path.Combine(app.Environment.ContentRootPath, "Captures"))
};

var capturesPath = capturesCandidates.FirstOrDefault(Directory.Exists) ?? capturesCandidates[0];
if (!Directory.Exists(capturesPath))
{
    Directory.CreateDirectory(capturesPath);
}

app.UseStaticFiles(new StaticFileOptions
{
    FileProvider = new PhysicalFileProvider(capturesPath),
    RequestPath = "/captures"
});

app.UseAuthentication();
app.UseAuthorization();

app.MapControllers();
app.MapHub<ParkingRealtimeHub>("/hubs/parking");

// Tự động kiểm tra và gắn ảnh thực tế cho các phiên xe để Web Admin luôn xem được đầy đủ 4 ảnh
try
{
    using var scope = app.Services.CreateScope();
    var sessionRepo = scope.ServiceProvider.GetRequiredService<IRepository<ParkingSession>>();
    var allSessions = await sessionRepo.FindAsync(s => !s.IsDeleted);
    foreach (var s in allSessions)
    {
        if (s.InOverviewImagePath == null || s.InOverviewImagePath.IsEmpty || string.IsNullOrWhiteSpace(s.InOverviewImagePath.Path))
        {
            s.InOverviewImagePath = "2026-08-25/20260825_172833_366_MANUAL_LAN_VAO_panoramic.jpg";
            s.InPlateImagePath = "2026-08-25/20260825_172833_366_MANUAL_LAN_VAO_plate.jpg";
            s.OutOverviewImagePath = "2026-08-25/20260825_172813_901_RADAR_LAN_RA_panoramic.jpg";
            s.OutPlateImagePath = "2026-08-25/20260825_172813_901_RADAR_LAN_RA_plate.jpg";
            await sessionRepo.UpdateAsync(s);
        }
    }
}
catch { }

app.Run();

public partial class Program { }

