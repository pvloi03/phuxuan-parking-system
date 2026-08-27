using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.IdentityModel.Tokens;
using PhuXuanParkingSystem.Api.DTOs;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;

namespace PhuXuanParkingSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    public class AuthController : ControllerBase
    {
        private readonly IRepository<User> _userRepo;
        private readonly IConfiguration _config;

        public AuthController(IRepository<User> userRepo, IConfiguration config)
        {
            _userRepo = userRepo;
            _config = config;
        }

        [HttpPost("login")]
        [AllowAnonymous]
        public async Task<IActionResult> Login([FromBody] LoginRequest request)
        {
            if (string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(ApiResponse.Fail("Vui lòng nhập đầy đủ tên đăng nhập và mật khẩu."));
            }

            var trimmedUsername = request.Username.Trim().ToLowerInvariant();

            // Kiểm tra xem đã có user nào trong hệ thống chưa, hoặc đảm bảo có tài khoản admin
            // IMPORTANT: Case-insensitive comparison để tránh bug không đăng nhập được
            var user = await _userRepo.FindOneAsync(u => u.Username.ToLower() == trimmedUsername && !u.IsDeleted);

            if (user == null && trimmedUsername.Equals("admin", StringComparison.OrdinalIgnoreCase))
            {
                user = new User(
                    "admin",
                    BCrypt.Net.BCrypt.HashPassword("admin123"),
                    "Quản Trị Viên Hệ Thống",
                    UserRole.Admin)
                {
                    Email = "admin@phuxuan.vn",
                    IsActive = true
                };
                await _userRepo.AddAsync(user);
            }

            if (user == null || !user.IsActive)
            {
                return Unauthorized(ApiResponse.Fail("Tài khoản không tồn tại hoặc đã bị khóa."));
            }

            // Xác thực mật khẩu BCrypt
            bool isPasswordValid = false;
            try
            {
                isPasswordValid = BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash);
            }
            catch
            {
                // Fallback nếu password lưu dạng plain text cũ
                isPasswordValid = user.PasswordHash == request.Password;
            }

            if (!isPasswordValid)
            {
                return Unauthorized(ApiResponse.Fail("Mật khẩu không chính xác."));
            }

            user.LastLoginAt = DateTime.Now;
            await _userRepo.UpdateAsync(user);

            // Sinh JWT Token
            var secretKey = _config["JwtSettings:SecretKey"] ?? "PhuXuanParkingSystem_Super_Secret_Key_2026_For_JWT_Authentication_Secure!";
            var issuer = _config["JwtSettings:Issuer"] ?? "PhuXuanParkingSystem.Api";
            var audience = _config["JwtSettings:Audience"] ?? "PhuXuanParkingSystem.Web";
            var expiryHours = int.TryParse(_config["JwtSettings:ExpiryHours"], out int h) ? h : 12;

            var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(secretKey));
            var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);

            var claims = new[]
            {
                new Claim(JwtRegisteredClaimNames.Sub, user.Id ?? ""),
                new Claim(JwtRegisteredClaimNames.Name, user.Username),
                new Claim("FullName", user.FullName),
                new Claim(ClaimTypes.Role, user.Role.ToString()),
                new Claim(JwtRegisteredClaimNames.Jti, Guid.NewGuid().ToString())
            };

            var expiresAt = DateTime.UtcNow.AddHours(expiryHours);

            var token = new JwtSecurityToken(
                issuer: issuer,
                audience: audience,
                claims: claims,
                expires: expiresAt,
                signingCredentials: creds
            );

            var loginResponse = new LoginResponse
            {
                Token = new JwtSecurityTokenHandler().WriteToken(token),
                UserId = user.Id ?? "",
                Username = user.Username,
                FullName = user.FullName,
                Role = user.Role,
                ExpiresAt = expiresAt
            };

            return Ok(ApiResponse<LoginResponse>.Ok(loginResponse, "Đăng nhập thành công."));
        }

        [HttpGet("me")]
        [Authorize]
        public async Task<IActionResult> GetCurrentUser()
        {
            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Fail("Chưa đăng nhập."));
            }

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null || !user.IsActive)
            {
                return Unauthorized(ApiResponse.Fail("Tài khoản không tồn tại hoặc đã bị khóa."));
            }

            var profile = new UserProfileDto
            {
                Id = user.Id ?? "",
                Username = user.Username,
                FullName = user.FullName,
                Email = user.Email,
                PhoneNumber = user.PhoneNumber,
                Role = user.Role,
                IsActive = user.IsActive,
                LastLoginAt = user.LastLoginAt
            };

            return Ok(ApiResponse<UserProfileDto>.Ok(profile, "Lấy thông tin người dùng thành công."));
        }

        [HttpPut("change-password")]
        [Authorize]
        public async Task<IActionResult> ChangeMyPassword([FromBody] ChangePasswordRequest request)
        {
            if (request == null)
            {
                return BadRequest(ApiResponse.Fail("Dữ liệu không hợp lệ."));
            }

            if (string.IsNullOrWhiteSpace(request.NewPassword))
            {
                return BadRequest(ApiResponse.Fail("Vui lòng nhập mật khẩu mới."));
            }

            if (request.NewPassword.Trim().Length < 6)
            {
                return BadRequest(ApiResponse.Fail("Mật khẩu mới phải có tối thiểu 6 ký tự."));
            }

            var userId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub);
            if (string.IsNullOrEmpty(userId))
            {
                return Unauthorized(ApiResponse.Fail("Chưa đăng nhập."));
            }

            var user = await _userRepo.GetByIdAsync(userId);
            if (user == null || user.IsDeleted)
            {
                return NotFound(ApiResponse.Fail("Không tìm thấy tài khoản người dùng."));
            }

            // Lấy mật khẩu cũ (hỗ trợ cả OldPassword và CurrentPassword từ frontend)
            var oldPassword = request.OldPassword ?? request.CurrentPassword ?? string.Empty;

            if (string.IsNullOrWhiteSpace(oldPassword))
            {
                return BadRequest(ApiResponse.Fail("Vui lòng nhập mật khẩu hiện tại để xác thực."));
            }

            // Xác thực mật khẩu cũ
            bool isOldValid = false;
            try
            {
                isOldValid = BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash);
            }
            catch
            {
                // Fallback: nếu mật khẩu lưu dạng plain text cũ
                isOldValid = user.PasswordHash == oldPassword;
            }

            if (!isOldValid)
            {
                return BadRequest(ApiResponse.Fail("Mật khẩu hiện tại không chính xác. Vui lòng kiểm tra lại."));
            }

            // Cập nhật mật khẩu mới
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword.Trim());
            user.UpdatedAt = DateTime.Now;

            var updateResult = await _userRepo.UpdateAsync(user);
            if (!updateResult)
            {
                return StatusCode(StatusCodes.Status500InternalServerError,
                    ApiResponse.Fail("Không thể cập nhật mật khẩu. Vui lòng thử lại."));
            }

            return Ok(ApiResponse.Ok("Đổi mật khẩu thành công!"));
        }
    }
}
