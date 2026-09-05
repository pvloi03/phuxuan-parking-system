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

        [Fact]
        public async Task Test_15_User_Role_Change_Generates_ChangeRole_Audit_Log_With_Reason()
        {
            var token = await AuthenticateAsAdminAsync();
            var username = "roleuser_" + new Random().Next(1000, 9999);

            // 1. Tạo user với vai trò Operator
            var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/users")
            {
                Content = JsonContent.Create(new UsersController.CreateUserRequest
                {
                    Username = username,
                    Password = "InitialPassword@123",
                    FullName = "Role Test User",
                    Role = UserRole.Operator,
                    IsActive = true
                })
            };
            createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var createRes = await _client.SendAsync(createReq);
            createRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var createData = await createRes.Content.ReadFromJsonAsync<ApiResponse<UsersController.UserDto>>(JsonOptions);
            var userId = createData!.Data!.Id;

            await Task.Delay(400);

            // 2. Cập nhật vai trò sang Manager
            var updateReq = new HttpRequestMessage(HttpMethod.Put, $"/api/users/{userId}")
            {
                Content = JsonContent.Create(new UsersController.UpdateUserRequest
                {
                    FullName = "Role Test User Updated",
                    Role = UserRole.Manager,
                    IsActive = true
                })
            };
            updateReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var updateRes = await _client.SendAsync(updateReq);
            updateRes.StatusCode.Should().Be(HttpStatusCode.OK);

            await Task.Delay(500);

            // 3. Truy vấn AuditLog với ActionType = ChangeRole
            var logReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/audit-logs?targetEntity=User&actionType=ChangeRole&search={username}");
            logReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var logRes = await _client.SendAsync(logReq);
            logRes.StatusCode.Should().Be(HttpStatusCode.OK);

            var logApiRes = await logRes.Content.ReadFromJsonAsync<ApiResponse<PagedResult<AuditLog>>>(JsonOptions);
            logApiRes.Should().NotBeNull();
            logApiRes!.Data!.Items.Should().NotBeEmpty();

            var roleLog = logApiRes.Data.Items.FirstOrDefault(x => x.TargetDisplay == username && x.ActionType == AuditActionType.ChangeRole);
            roleLog.Should().NotBeNull();
            roleLog!.ChangedProperties.Should().Contain("Role");
            roleLog.Reason.Should().NotBeNullOrWhiteSpace();
            roleLog.Reason.Should().Contain("Manager");
        }

        [Fact]
        public async Task Test_16_Department_And_Company_Crud_Generates_Audit_Log_With_Diff()
        {
            var token = await AuthenticateAsAdminAsync();
            var rand = new Random().Next(1000, 9999);

            // 1. Tạo Department
            var deptName = "Phong KT " + rand;
            var deptReq = new HttpRequestMessage(HttpMethod.Post, "/api/departments")
            {
                Content = JsonContent.Create(new Department
                {
                    Name = deptName,
                    Code = "PKT_" + rand,
                    IsActive = true
                })
            };
            deptReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var deptRes = await _client.SendAsync(deptReq);
            deptRes.StatusCode.Should().Be(HttpStatusCode.OK);

            // 2. Tạo Company
            var compName = "Cong Ty " + rand;
            var compReq = new HttpRequestMessage(HttpMethod.Post, "/api/companies")
            {
                Content = JsonContent.Create(new Company
                {
                    Name = compName,
                    Code = "CTY_" + rand,
                    IsActive = true
                })
            };
            compReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var compRes = await _client.SendAsync(compReq);
            compRes.StatusCode.Should().Be(HttpStatusCode.OK);

            await Task.Delay(600);

            // 3. Kiểm tra AuditLog Department
            var deptLogReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/audit-logs?targetEntity=Department&search={deptName}");
            deptLogReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var deptLogRes = await _client.SendAsync(deptLogReq);
            deptLogRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var deptLogApiRes = await deptLogRes.Content.ReadFromJsonAsync<ApiResponse<PagedResult<AuditLog>>>(JsonOptions);
            deptLogApiRes!.Data!.Items.Should().NotBeEmpty();
            var deptLog = deptLogApiRes.Data.Items.FirstOrDefault(x => x.TargetDisplay == deptName);
            deptLog.Should().NotBeNull();
            deptLog!.ActionType.Should().Be(AuditActionType.Create);
            deptLog.NewValues.Should().Contain(deptName);

            // 4. Kiểm tra AuditLog Company
            var compLogReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/audit-logs?targetEntity=Company&search={compName}");
            compLogReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var compLogRes = await _client.SendAsync(compLogReq);
            compLogRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var compLogApiRes = await compLogRes.Content.ReadFromJsonAsync<ApiResponse<PagedResult<AuditLog>>>(JsonOptions);
            compLogApiRes!.Data!.Items.Should().NotBeEmpty();
            var compLog = compLogApiRes.Data.Items.FirstOrDefault(x => x.TargetDisplay == compName);
            compLog.Should().NotBeNull();
            compLog!.ActionType.Should().Be(AuditActionType.Create);
            compLog.NewValues.Should().Contain(compName);
        }

        [Fact]
        public async Task Test_14_HardDelete_And_Restore_In_RecycleBin_Generates_PermanentDelete_And_Restore_AuditLogs()
        {
            var token = await AuthenticateAsAdminAsync();
            var rand = Guid.NewGuid().ToString("N")[..6].ToUpper();
            var plate = $"75AHARD{rand}";

            // 1. Tạo Vehicle mới
            var createReq = new HttpRequestMessage(HttpMethod.Post, "/api/vehicles")
            {
                Content = JsonContent.Create(new Vehicle
                {
                    PlateNumber = plate,
                    Type = VehicleType.Car,
                    IsActive = true
                })
            };
            createReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var createRes = await _client.SendAsync(createReq);
            createRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var createApiRes = await createRes.Content.ReadFromJsonAsync<ApiResponse<Vehicle>>(JsonOptions);
            var vehicleId = createApiRes!.Data!.Id;

            // 2. Xóa mềm (đưa vào thùng rác)
            var softDelReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/vehicles/{vehicleId}?reason=DuaVaoThungRac");
            softDelReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var softDelRes = await _client.SendAsync(softDelReq);
            softDelRes.StatusCode.Should().Be(HttpStatusCode.OK);

            // 3. Khôi phục từ thùng rác kèm reason
            var restoreReq = new HttpRequestMessage(HttpMethod.Post, "/api/recycle-bin/restore?reason=KhoiPhucKiemThu")
            {
                Content = JsonContent.Create(new { itemType = "Vehicle", id = vehicleId })
            };
            restoreReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var restoreRes = await _client.SendAsync(restoreReq);
            restoreRes.StatusCode.Should().Be(HttpStatusCode.OK);

            await Task.Delay(800);

            // 4. Kiểm tra AuditLog Khôi phục (Restore)
            var restoreAuditReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/audit-logs?targetEntity=Vehicle&actionType=Restore&search={plate}");
            restoreAuditReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var restoreAuditRes = await _client.SendAsync(restoreAuditReq);
            restoreAuditRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var restoreAuditApiRes = await restoreAuditRes.Content.ReadFromJsonAsync<ApiResponse<PagedResult<AuditLog>>>(JsonOptions);
            restoreAuditApiRes!.Data!.Items.Should().NotBeEmpty();
            var restoreLog = restoreAuditApiRes.Data.Items.FirstOrDefault(x => x.TargetId == vehicleId);
            restoreLog.Should().NotBeNull();
            restoreLog!.ActionType.Should().Be(AuditActionType.Restore);
            restoreLog.Reason.Should().Be("KhoiPhucKiemThu");

            // 5. Xóa mềm lần 2 để chuẩn bị xóa vĩnh viễn
            var softDelReq2 = new HttpRequestMessage(HttpMethod.Delete, $"/api/vehicles/{vehicleId}?reason=DuaVaoThungRac2");
            softDelReq2.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var softDelRes2 = await _client.SendAsync(softDelReq2);
            softDelRes2.StatusCode.Should().Be(HttpStatusCode.OK);

            // 6. Xóa vĩnh viễn (Hard Delete) từ Thùng rác kèm reason
            var hardDelReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/recycle-bin/hard-delete/Vehicle/{vehicleId}?reason=XoaVinhVienKiemToan");
            hardDelReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var hardDelRes = await _client.SendAsync(hardDelReq);
            hardDelRes.StatusCode.Should().Be(HttpStatusCode.OK);

            await Task.Delay(800);

            // 7. Kiểm tra AuditLog Xóa vĩnh viễn (PermanentDelete)
            var hardDelAuditReq = new HttpRequestMessage(HttpMethod.Get, $"/api/v1/audit-logs?targetEntity=Vehicle&actionType=PermanentDelete&search={plate}");
            hardDelAuditReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var hardDelAuditRes = await _client.SendAsync(hardDelAuditReq);
            hardDelAuditRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var hardDelAuditApiRes = await hardDelAuditRes.Content.ReadFromJsonAsync<ApiResponse<PagedResult<AuditLog>>>(JsonOptions);
            hardDelAuditApiRes!.Data!.Items.Should().NotBeEmpty();
            var hardDelLog = hardDelAuditApiRes.Data.Items.FirstOrDefault(x => x.TargetId == vehicleId);
            hardDelLog.Should().NotBeNull();
            hardDelLog!.ActionType.Should().Be(AuditActionType.PermanentDelete);
            hardDelLog.Reason.Should().Be("XoaVinhVienKiemToan");
            hardDelLog.OldValues.Should().NotBeNullOrWhiteSpace();
            hardDelLog.OldValues.Should().Contain(plate);
        }

        [Fact]
        public async Task Test_18_Unknown_Api_Endpoint_Returns_404_Json_Not_Spa_Html()
        {
            var response = await _client.GetAsync("/api/this-endpoint-does-not-exist-12345");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().Contain("không tồn tại");
            response.Content.Headers.ContentType?.MediaType.Should().Be("application/json");
        }

        [Fact]
        public async Task Test_19_Unknown_Captures_Endpoint_Returns_404_Not_Spa_Html()
        {
            var response = await _client.GetAsync("/captures/unknown-year/non-existent-plate-image.jpg");
            response.StatusCode.Should().Be(HttpStatusCode.NotFound);
            var content = await response.Content.ReadAsStringAsync();
            content.Should().NotContain("<!DOCTYPE html>");
            content.Should().NotContain("<html");
        }
    }
}
