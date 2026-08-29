using FluentAssertions;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.Extensions.DependencyInjection;
using PhuXuanParkingSystem.Api.DTOs;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using System.Net;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using Xunit;

namespace PhuXuanParkingSystem.Api.Tests
{
    public class ApiIntegrationTests : IClassFixture<WebApplicationFactory<Program>>
    {
        private readonly WebApplicationFactory<Program> _factory;
        private readonly HttpClient _client;

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

            var apiRes = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
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
            var apiRes = await response.Content.ReadFromJsonAsync<ApiResponse<LoginResponse>>();
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
            var apiRes = await response.Content.ReadFromJsonAsync<ApiResponse<UserProfileDto>>();
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
            var apiRes = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ParkingSession>>>();
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
            var apiRes = await response.Content.ReadFromJsonAsync<ApiResponse<PagedResult<ParkingSession>>>();
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
            var createdApiRes = await createRes.Content.ReadFromJsonAsync<ApiResponse<Vehicle>>();
            createdApiRes.Should().NotBeNull();
            createdApiRes!.Success.Should().BeTrue();
            createdApiRes.Data.Should().NotBeNull();
            createdApiRes.Data!.Id.Should().NotBeNullOrWhiteSpace();

            // 2. Query Vehicle
            var getReq = new HttpRequestMessage(HttpMethod.Get, $"/api/vehicles/{createdApiRes.Data.Id}");
            getReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var getRes = await _client.SendAsync(getReq);
            getRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var getApiRes = await getRes.Content.ReadFromJsonAsync<ApiResponse<Vehicle>>();
            getApiRes.Should().NotBeNull();
            getApiRes!.Success.Should().BeTrue();
            getApiRes.Data!.PlateNumber.Should().Be(createdApiRes.Data.PlateNumber);

            // 3. Delete Vehicle
            var delReq = new HttpRequestMessage(HttpMethod.Delete, $"/api/vehicles/{createdApiRes.Data.Id}");
            delReq.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
            var delRes = await _client.SendAsync(delReq);
            delRes.StatusCode.Should().Be(HttpStatusCode.OK);
            var delApiRes = await delRes.Content.ReadFromJsonAsync<ApiResponse>();
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
            var apiRes = await response.Content.ReadFromJsonAsync<ApiResponse>();
            apiRes.Should().NotBeNull();
            apiRes!.Success.Should().BeFalse();
            apiRes.Message.Should().NotBeNullOrWhiteSpace();
        }
    }
}
