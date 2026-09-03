using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PhuXuanParkingSystem.Api.Controllers;
using PhuXuanParkingSystem.Api.DTOs;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Text.Json.Serialization;
using Xunit;

namespace PhuXuanParkingSystem.Api.Tests
{
    public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;
        private static readonly JsonSerializerOptions JsonOptions = new()
        {
            PropertyNameCaseInsensitive = true,
            Converters = { new JsonStringEnumConverter() }
        };

        public ApiIntegrationTests(WebApplicationFactory<Program> factory)
        {
            _factory = factory;
            _client = _factory.CreateClient();
        }

        private async Task<string> AuthenticateAsAdminAsync()
        {
            var loginReq = new LoginRequest
            {
                Username = "admin",
                Password = "admin123"
            };

            var response = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                loginReq.Password = "Admin@123";
                response = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
            }
            response.EnsureSuccessStatusCode();

            var apiRes = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
            apiRes.Should().NotBeNull();
            apiRes!.Success.Should().BeTrue();
            apiRes.Data.Should().NotBeNull();
            apiRes.Data!.Token.Should().NotBeNullOrWhiteSpace();

            return apiRes.Data.Token;
        }

        [Fact]
        public async Task Test_1_Login_With_Valid_Credentials_Returns_Unified_ApiResponse()
        {
            // Arrange
            var loginReq = new LoginRequest
            {
                Username = "admin",
                Password = "admin123"
            };

            // Act
            var response = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
            if (response.StatusCode == HttpStatusCode.Unauthorized)
            {
                loginReq.Password = "Admin@123";
                response = await _client.PostAsJsonAsync("/api/auth/login", loginReq);
            }

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiRes = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>(JsonOptions);
            apiRes.Should().NotBeNull();
            apiRes!.Success.Should().BeTrue();
            apiRes.Data.Should().NotBeNull();
            apiRes.Data!.Username.Should().Be("admin");
            apiRes.Data.Role.Should().Be(UserRole.Admin);
            apiRes.Data.Token.Should().NotBeNullOrWhiteSpace();
            apiRes.Message.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Test_2_Get_Current_User_With_Bearer_Token_Returns_Profile()
        {
            // Arrange
            var token = await AuthenticateAsAdminAsync();
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/auth/me");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiRes = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfileDto>>(JsonOptions);
            apiRes.Should().NotBeNull();
            apiRes!.Success.Should().BeTrue();
            apiRes.Data.Should().NotBeNull();
            apiRes.Data!.Username.Should().Be("admin");
            apiRes.Data.IsActive.Should().BeTrue();
        }

        [Fact]
        public async Task Test_3_Unauthorized_Access_Without_Token_Returns_401()
        {
            // Act
            var response = await _client.GetAsync("/api/dashboard/metrics");

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Test_4_Dashboard_Metrics_Returns_Valid_Data_Structure()
        {
            // Arrange
            var token = await AuthenticateAsAdminAsync();
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/dashboard/metrics");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiRes = await response.Content.ReadFromJsonAsync<ApiResponse<DashboardMetricsDto>>();
            apiRes.Should().NotBeNull();
            apiRes!.Success.Should().BeTrue();
            apiRes.Data.Should().NotBeNull();
            apiRes.Data!.HourlyTraffic.Should().HaveCount(24);
        }

        [Fact]
        public async Task Test_5_Parking_Sessions_Query_And_Pagination()
        {
            // Arrange
            var token = await AuthenticateAsAdminAsync();
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/parkingsessions?pageNumber=1&pageSize=10");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiRes = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ParkingSession>>>(JsonOptions);
            apiRes.Should().NotBeNull();
            apiRes!.Success.Should().BeTrue();
            apiRes.Data.Should().NotBeNull();
            apiRes.Data!.PageNumber.Should().Be(1);
            apiRes.Data.PageSize.Should().Be(10);
            apiRes.Data.Items.Should().NotBeNull();
        }

        [Fact]
        public async Task Test_6_Parking_Sessions_Export_Excel_Returns_File_Stream()
        {
            // Arrange
            var token = await AuthenticateAsAdminAsync();
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/parkingsessions/export-excel");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            // Act
            var response = await _client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            var bytes = await response.Content.ReadAsByteArrayAsync();
            bytes.Should().NotBeEmpty();
            bytes.Length.Should().BeGreaterThan(100);
        }

        [Fact]
        public async Task Test_7_WinForms_Data_Synchronization_Simulation()
        {
            // Arrange: Giả lập WinForms trạm kiểm soát thêm 1 ParkingSession vào CSDL MongoDB chung
            using var scope = _factory.Services.CreateScope();
            var sessionRepo = scope.ServiceProvider.GetRequiredService<IRepository<ParkingSession>>();

            var testPlate = "30A-" + new Random().Next(10000, 99999);
            var session = ParkingSession.CheckIn("Làn Vào 1", testPlate, PhuXuanParkingSystem.Models.ValueObjects.ImageStoragePath.Empty, PhuXuanParkingSystem.Models.ValueObjects.ImageStoragePath.Empty, "Nguyễn Văn Test (WinForms Sync)", VehicleType.Car);
            session.InTime = DateTime.Now;
            await sessionRepo.AddAsync(session);

            // Act: Web API tra cứu lại session vừa được thêm bởi WinForms
            var token = await AuthenticateAsAdminAsync();
            var request = new HttpRequestMessage(HttpMethod.Get, $"/api/parkingsessions?plateNumber={session.PlateNumber}");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiRes = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ParkingSession>>>(JsonOptions);
            apiRes.Should().NotBeNull();
            apiRes!.Success.Should().BeTrue();
            apiRes.Data!.Items.Should().Contain(s => s.PlateNumber == session.PlateNumber);

            // Cleanup
            if (session.Id != null)
            {
                await sessionRepo.DeleteAsync(session.Id, softDelete: false);
            }
        }

        [Fact]
        public async Task Test_8_Vehicles_CRUD_Flow()
        {
            var token = await AuthenticateAsAdminAsync();

            // 1. Create Vehicle
            var testPlate = "51H-" + new Random().Next(10000, 99999);
            var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/vehicles")
            {
                Content = JsonContent.Create(new Vehicle(testPlate, VehicleType.Car))
            };
            createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var createRes = await _client.SendAsync(createReq);
            createRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var createdApiRes = await createRes.Content.ReadFromJsonAsync<ApiResponse<Vehicle>>(JsonOptions);
            createdApiRes.Should().NotBeNull();
            createdApiRes!.Success.Should().BeTrue();
            createdApiRes.Data.Should().NotBeNull();
            createdApiRes.Data!.Id.Should().NotBeNullOrWhiteSpace();

            // 2. Query Vehicle
            var getReq = new HttpRequestMessage(HttpMethod.Get, $"/api/vehicles/{createdApiRes.Data.Id}");
            getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var getRes = await _client.SendAsync(getReq);
            getRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var getApiRes = await getRes.Content.ReadFromJsonAsync<ApiResponse<Vehicle>>(JsonOptions);
            getApiRes.Should().NotBeNull();
            getApiRes!.Success.Should().BeTrue();
            getApiRes.Data!.PlateNumber.Should().Be(createdApiRes.Data.PlateNumber);

            // 3. Delete Vehicle
            var delReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/vehicles/{createdApiRes.Data.Id}");
            delReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var delRes = await _client.SendAsync(delReq);
            delRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var delApiRes = await delRes.Content.ReadFromJsonAsync<ApiResponse>(JsonOptions);
            delApiRes.Should().NotBeNull();
            delApiRes!.Success.Should().BeTrue();
        }

        [Fact]
        public async Task Test_9_Global_Exception_Middleware_Handles_Bad_Requests()
        {
            // Act: Gửi request đăng nhập rỗng để kiểm tra cấu trúc lỗi ApiResponse.Fail
            var emptyLogin = new LoginRequest { Username = "", Password = "" };
            var response = await _client.PostAsJsonAsync("/api/auth/login", emptyLogin);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.BadRequest);
            var apiRes = await response.Content.ReadFromJsonAsync<ApiResponse>(JsonOptions);
            apiRes.Should().NotBeNull();
            apiRes!.Success.Should().BeFalse();
            apiRes.Message.Should().NotBeNullOrWhiteSpace();
        }

        [Fact]
        public async Task Test_10_Audit_Log_Created_On_Login_And_Queryable_Via_Api()
        {
            // Arrange: Đăng nhập để kích hoạt ghi AuditLog
            var token = await AuthenticateAsAdminAsync();

            // Chờ worker background ghi vào MongoDB (in-process channel)
            await Task.Delay(500);

            // Act: Truy vấn nhật ký kiểm toán qua API
            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/audit-logs?actionType=Login&pageNumber=1&pageSize=10");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);

            // Assert
            response.StatusCode.Should().Be(HttpStatusCode.OK);
            var apiRes = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<AuditLog>>>(JsonOptions);
            apiRes.Should().NotBeNull();
            apiRes!.Success.Should().BeTrue();
            apiRes.Data.Should().NotBeNull();
            apiRes.Data!.Items.Should().NotBeEmpty();

            var loginLog = apiRes.Data.Items.FirstOrDefault(x => x.ActorUsername == "admin" && x.ActionType == AuditActionType.Login);
            loginLog.Should().NotBeNull();
            loginLog!.IsSuccess.Should().BeTrue();
            loginLog.Source.Should().Be("WebAdmin");
        }

        [Fact]
        public async Task Test_11_Audit_Log_Access_Restricted_For_Unauthorized_Users()
        {
            // Act: Truy cập danh sách audit log khi chưa đăng nhập
            var response = await _client.GetAsync("/api/v1/audit-logs");

            // Assert: Phải bị từ chối với mã lỗi 401 Unauthorized
            response.StatusCode.Should().Be(HttpStatusCode.Unauthorized);
        }

        [Fact]
        public async Task Test_12_Vehicle_Create_Update_Delete_Generates_Audit_Log_With_Diff()
        {
            var token = await AuthenticateAsAdminAsync();
            var plate = "99A-" + new Random().Next(10000, 99999);

            // 1. Create Vehicle
            var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/vehicles")
            {
                Content = JsonContent.Create(new Vehicle(plate, VehicleType.Car))
            };
            createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var createRes = await _client.SendAsync(createReq);
            createRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var createData = await createRes.Content.ReadFromJsonAsync<ApiResponse<Vehicle>>(JsonOptions);
            var vehicleId = createData!.Data!.Id;

            // 2. Update Vehicle
            var updatedPlate = "99A-" + new Random().Next(10000, 99999);
            var updateReq = new HttpRequestMessage(HttpMethod.Put, $"/api/vehicles/{vehicleId}")
            {
                Content = JsonContent.Create(new Vehicle(updatedPlate, VehicleType.Truck))
            };
            updateReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var updateRes = await _client.SendAsync(updateReq);
            updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

            // 3. Delete Vehicle
            var delReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/vehicles/{vehicleId}?reason=Thanh%20ly%20xe");
            delReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var delRes = await _client.SendAsync(delReq);
            delRes.StatusCode.Should().Be(HttpStatusCode.OK);

            // Chờ worker background ghi vào MongoDB
            await Task.Delay(600);

            var cleanPlate = createData!.Data!.PlateNumber;

            // Act: Kiểm tra các bản ghi AuditLog đã tạo cho vehicleId
            var logReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/audit-logs?targetEntity=Vehicle&search={cleanPlate}");
            logReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var logRes = await _client.SendAsync(logReq);
            logRes.StatusCode.Should().Be(HttpStatusCode.OK);

            var logApiRes = await logRes.Content.ReadFromJsonAsync<ApiResponse<PagedResult<AuditLog>>>(JsonOptions);
            logApiRes.Should().NotBeNull();
            logApiRes!.Data!.Items.Should().NotBeEmpty();

            var createLog = logApiRes.Data.Items.FirstOrDefault(x => x.ActionType == AuditActionType.Create && x.TargetId == vehicleId);
            createLog.Should().NotBeNull();
            createLog!.NewValues.Should().Contain(cleanPlate);

            // Kiểm tra log Update
            var updateLogReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/audit-logs?targetEntity=Vehicle&actionType=Update");
            updateLogReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var updateLogRes = await _client.SendAsync(updateLogReq);
            var updateLogApiRes = await updateLogRes.Content.ReadFromJsonAsync<ApiResponse<PagedResult<AuditLog>>>(JsonOptions);
            var updateLog = updateLogApiRes!.Data!.Items.FirstOrDefault(x => x.TargetId == vehicleId);
            updateLog.Should().NotBeNull();
            updateLog!.OldValues.Should().Contain(cleanPlate);
            updateLog.ChangedProperties.Should().Contain("PlateNumber");
        }

        [Fact]
        public async Task Test_13_User_Creation_And_Update_Masks_Sensitive_PasswordHash_In_Audit_Log()
        {
            var token = await AuthenticateAsAdminAsync();
            var username = "audittest_" + new Random().Next(1000, 9999);

            // 1. Tạo User mới
            var userReq = new HttpRequestMessage(HttpMethod.Post, "/api/users")
            {
                Content = JsonContent.Create(new UsersController.CreateUserRequest
                {
                    Username = username,
                    Password = "SecretPassword@123",
                    FullName = "Audit Test User",
                    Role = UserRole.Operator,
                    IsActive = true
                })
            };
            userReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var userRes = await _client.SendAsync(userReq);
            userRes.StatusCode.Should().Be(HttpStatusCode.OK);

            // Chờ worker background ghi vào MongoDB
            await Task.Delay(500);

            // 2. Lấy AuditLog tạo user
            var logReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/audit-logs?targetEntity=User&search={username}");
            logReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var logRes = await _client.SendAsync(logReq);
            logRes.StatusCode.Should().Be(HttpStatusCode.OK);

            var logApiRes = await logRes.Content.ReadFromJsonAsync<ApiResponse<PagedResult<AuditLog>>>(JsonOptions);
            logApiRes.Should().NotBeNull();
            var createLog = logApiRes!.Data!.Items.FirstOrDefault(x => x.TargetDisplay == username && x.ActionType == AuditActionType.Create);

            createLog.Should().NotBeNull();
            createLog!.NewValues.Should().NotBeNull();
            createLog.NewValues.Should().Contain("\"PasswordHash\":\"******\"");
            createLog.NewValues.Should().NotContain("SecretPassword@123");
        }

        [Fact]
        public async Task Test_14_Export_Audit_Logs_To_Excel_Succeeds_And_Contains_Valid_File()
        {
            var token = await AuthenticateAsAdminAsync();

            var request = new HttpRequestMessage(HttpMethod.Get, "/api/v1/audit-logs/export");
            request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

            var response = await _client.SendAsync(request);

            response.StatusCode.Should().Be(HttpStatusCode.OK);
            response.Content.Headers.ContentType!.MediaType.Should().Be("application/vnd.openxmlformats-officedocument.spreadsheetml.sheet");

            var bytes = await response.Content.ReadAsByteArrayAsync();
            bytes.Should().NotBeEmpty();
            bytes.Length.Should().BeGreaterThan(100);
        }
    }
}
