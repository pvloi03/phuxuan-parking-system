using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using MongoDB.Driver;
using PhuXuanParkingSystem.Api.DTOs;
using PhuXuanParkingSystem.Api.Helpers;
using PhuXuanParkingSystem.Api.Services;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Models.Enums;
using PhuXuanParkingSystem.Repositories;
using System;
using System.Collections.Generic;
using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class UsersController : ControllerBase
    {
        private readonly IRepository<User> _userRepo;
        private readonly IAuditLogQueue _auditQueue;

        public UsersController(IRepository<User> userRepo, IAuditLogQueue auditQueue)
        {
            _userRepo = userRepo ?? throw new ArgumentNullException(nameof(userRepo));
            _auditQueue = auditQueue ?? throw new ArgumentNullException(nameof(auditQueue));
        }

        public class CreateUserRequest
        {
            public string Username { get; set; } = string.Empty;
            public string Password { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? PhoneNumber { get; set; }
            public UserRole Role { get; set; } = UserRole.Operator;
            public bool IsActive { get; set; } = true;
        }

        public class UpdateUserRequest
        {
            public string FullName { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? PhoneNumber { get; set; }
            public UserRole Role { get; set; } = UserRole.Operator;
            public bool IsActive { get; set; } = true;
        }

        public class ChangePasswordRequest
        {
            public string? OldPassword { get; set; }
            public string? CurrentPassword { get; set; }
            public string NewPassword { get; set; } = string.Empty;
        }

        private string? GetOldPassword(ChangePasswordRequest request)
        {
            return request.OldPassword ?? request.CurrentPassword;
        }

        public class UserDto
        {
            public string Id { get; set; } = string.Empty;
            public string Username { get; set; } = string.Empty;
            public string FullName { get; set; } = string.Empty;
            public string? Email { get; set; }
            public string? PhoneNumber { get; set; }
            public UserRole Role { get; set; }
            public string RoleLabel { get; set; } = string.Empty;
            public bool IsActive { get; set; }
            public DateTime? LastLoginAt { get; set; }
            public DateTime CreatedAt { get; set; }
        }

        private static string GetRoleLabel(UserRole role)
        {
            return role switch
            {
                UserRole.Admin => "Quản Trị Viên",
                UserRole.Manager => "Quản Lý",
                UserRole.Operator => "Nhân Viên Vận Hành",
                UserRole.Security => "Bảo Vệ Trực Làn",
                UserRole.Viewer => "Người Xem",
                _ => role.ToString()
            };
        }

        private static UserDto MapToDto(User u) => new()
        {
            Id = u.Id,
            Username = u.Username,
            FullName = u.FullName,
            Email = u.Email,
            PhoneNumber = u.PhoneNumber,
            Role = u.Role,
            RoleLabel = GetRoleLabel(u.Role),
            IsActive = u.IsActive,
            LastLoginAt = u.LastLoginAt,
            CreatedAt = u.CreatedAt
        };

        /// <summary>
        /// Lấy danh sách người dùng (Chỉ dành cho Admin & Manager)
        /// </summary>
        [HttpGet]
        [Authorize(Roles = "Admin,Manager")]
        public async Task<IActionResult> GetUsers(
            [FromQuery] string? search = null,
            [FromQuery] UserRole? role = null,
            [FromQuery] bool? isActive = null,
            [FromQuery] int pageNumber = 1,
            [FromQuery] int pageSize = 10)
        {
            pageNumber = Math.Max(1, pageNumber);
            pageSize = Math.Clamp(pageSize, 1, 100);

            var filter = Builders<User>.Filter.Eq(u => u.IsDeleted, false);

            if (role.HasValue)
            {
                filter = Builders<User>.Filter.And(filter, Builders<User>.Filter.Eq(u => u.Role, role.Value));
            }

            if (isActive.HasValue)
            {
                filter = Builders<User>.Filter.And(filter, Builders<User>.Filter.Eq(u => u.IsActive, isActive.Value));
            }

            if (!string.IsNullOrWhiteSpace(search))
            {
                var s = search.Trim();
                var searchFilter = Builders<User>.Filter.Or(
                    Builders<User>.Filter.Regex(u => u.Username, new MongoDB.Bson.BsonRegularExpression(s, "i")),
                    Builders<User>.Filter.Regex(u => u.FullName, new MongoDB.Bson.BsonRegularExpression(s, "i")),
                    Builders<User>.Filter.Regex(u => u.Email, new MongoDB.Bson.BsonRegularExpression(s, "i")),
                    Builders<User>.Filter.Regex(u => u.PhoneNumber, new MongoDB.Bson.BsonRegularExpression(s, "i"))
                );
                filter = Builders<User>.Filter.And(filter, searchFilter);
            }

            var totalCount = await _userRepo.CountAsync(filter);
            var sort = Builders<User>.Sort.Descending(u => u.CreatedAt);
            var users = await _userRepo.FindAsync(filter, sort, (pageNumber - 1) * pageSize, pageSize);

            var items = new List<UserDto>();
            foreach (var u in users) items.Add(MapToDto(u));

            return Ok(new
            {
                success = true,
                data = new
                {
                    items,
                    totalCount = (int)totalCount,
                    pageNumber,
                    pageSize,
                    totalPages = (int)Math.Ceiling((double)totalCount / pageSize)
                }
            });
        }

        /// <summary>
        /// Lấy chi tiết tài khoản
        /// </summary>
        [HttpGet("{id}")]
        public async Task<IActionResult> GetUserById(string id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null || user.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy tài khoản người dùng."));
            return Ok(ApiResponse<UserDto>.Ok(MapToDto(user)));
        }

        /// <summary>
        /// Thêm mới tài khoản người dùng (Chỉ dành cho Admin)
        /// </summary>
        [HttpPost]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> CreateUser([FromBody] CreateUserRequest request)
        {
            if (request == null || string.IsNullOrWhiteSpace(request.Username) || string.IsNullOrWhiteSpace(request.Password))
            {
                return BadRequest(ApiResponse.Fail("Tên đăng nhập và mật khẩu không được để trống."));
            }

            if (request.Password.Length < 6)
            {
                return BadRequest(ApiResponse.Fail("Mật khẩu phải có độ dài tối thiểu 6 ký tự."));
            }

            var cleanUsername = request.Username.Trim().ToLowerInvariant();
            var exists = await _userRepo.FindOneAsync(u => u.Username == cleanUsername && !u.IsDeleted);
            if (exists != null)
            {
                return BadRequest(ApiResponse.Fail($"Tên đăng nhập '{cleanUsername}' đã tồn tại trong hệ thống."));
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var cleanEmail = request.Email.Trim().ToLowerInvariant();
                var emailExists = await _userRepo.FindOneAsync(u => u.Email != null && u.Email.ToLower() == cleanEmail && !u.IsDeleted);
                if (emailExists != null)
                {
                    return BadRequest(ApiResponse.Fail($"Email '{cleanEmail}' đã được sử dụng cho tài khoản khác."));
                }
            }

            var user = new User
            {
                Username = cleanUsername,
                PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
                FullName = request.FullName?.Trim() ?? cleanUsername,
                Email = request.Email?.Trim(),
                PhoneNumber = request.PhoneNumber?.Trim(),
                Role = request.Role,
                IsActive = request.IsActive,
                CreatedAt = DateTime.Now
            };

            await _userRepo.AddAsync(user);

            // Ghi nhận AuditLog Create
            var diff = AuditDiffHelper.ComputeDiff<User>(null, user);
            await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Create, "User", user.Id, user.Username, diff);

            return Ok(ApiResponse<UserDto>.Ok(MapToDto(user), "Tạo tài khoản người dùng thành công!"));
        }

        /// <summary>
        /// Cập nhật thông tin tài khoản & vai trò (Chỉ dành cho Admin)
        /// </summary>
        [HttpPut("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> UpdateUser(string id, [FromBody] UpdateUserRequest request)
        {
            if (request == null) return BadRequest(ApiResponse.Fail("Dữ liệu cập nhật không hợp lệ."));

            var user = await _userRepo.GetByIdAsync(id);
            if (user == null || user.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy tài khoản người dùng."));

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (user.Id == currentUserId && request.Role != UserRole.Admin)
            {
                return BadRequest(ApiResponse.Fail("Bạn không thể tự giáng cấp vai trò Quản Trị Viên của chính mình."));
            }

            if (!string.IsNullOrWhiteSpace(request.Email))
            {
                var cleanEmail = request.Email.Trim().ToLowerInvariant();
                var emailExists = await _userRepo.FindOneAsync(u => u.Id != id && u.Email != null && u.Email.ToLower() == cleanEmail && !u.IsDeleted);
                if (emailExists != null)
                {
                    return BadRequest(ApiResponse.Fail($"Email '{cleanEmail}' đã được sử dụng cho tài khoản khác."));
                }
            }

            // Snapshot trạng thái cũ trước khi cập nhật
            var snapshot = AuditDiffHelper.TakeSnapshot(user);
            var isRoleChanged = user.Role != request.Role;

            user.FullName = request.FullName?.Trim() ?? user.FullName;
            user.Email = request.Email?.Trim();
            user.PhoneNumber = request.PhoneNumber?.Trim();
            user.Role = request.Role;
            user.IsActive = request.IsActive;
            user.UpdatedAt = DateTime.Now;

            await _userRepo.UpdateAsync(user);

            // Ghi nhận AuditLog Update / ChangeRole
            var diff = AuditDiffHelper.ComputeDiffFromSnapshot(snapshot, user);
            if (diff.HasChanges)
            {
                var actionType = isRoleChanged ? AuditActionType.ChangeRole : AuditActionType.Update;
                var reason = isRoleChanged ? $"Thay đổi vai trò từ {snapshot["Role"]} sang {user.Role}" : null;
                await _auditQueue.LogActivityAsync(User, HttpContext, actionType, "User", user.Id, user.Username, diff, reason: reason);
            }

            return Ok(ApiResponse<UserDto>.Ok(MapToDto(user), "Cập nhật tài khoản người dùng thành công!"));
        }

        /// <summary>
        /// Đổi / Reset mật khẩu người dùng
        /// </summary>
        [HttpPut("{id}/password")]
        public async Task<IActionResult> ChangePassword(string id, [FromBody] ChangePasswordRequest request)
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

            var user = await _userRepo.GetByIdAsync(id);
            if (user == null || user.IsDeleted)
            {
                return NotFound(ApiResponse.Fail("Không tìm thấy tài khoản người dùng."));
            }

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier)
                             ?? User.FindFirstValue(JwtRegisteredClaimNames.Sub)
                             ?? User.FindFirstValue("sub")
                             ?? User.FindFirst("http://schemas.xmlsoap.org/ws/2005/05/identity/claims/nameidentifier")?.Value;

            var roleClaim = User.FindFirstValue(ClaimTypes.Role)
                         ?? User.FindFirstValue("http://schemas.microsoft.com/ws/2008/06/identity/claims/role")
                         ?? User.FindFirstValue("role")
                         ?? string.Empty;

            var isCurrentUserAdmin = string.Equals(roleClaim, "Admin", StringComparison.OrdinalIgnoreCase)
                                  || roleClaim == "1";

            var oldPassword = GetOldPassword(request);

            if (!isCurrentUserAdmin)
            {
                var isSameUser = !string.IsNullOrEmpty(user.Id)
                              && !string.IsNullOrEmpty(currentUserId)
                              && string.Equals(user.Id, currentUserId, StringComparison.OrdinalIgnoreCase);

                if (!isSameUser)
                {
                    return StatusCode(StatusCodes.Status403Forbidden,
                        ApiResponse.Fail("Bạn không có quyền đổi mật khẩu cho tài khoản người dùng khác!"));
                }

                if (string.IsNullOrWhiteSpace(oldPassword))
                {
                    return BadRequest(ApiResponse.Fail("Vui lòng nhập mật khẩu hiện tại của bạn!"));
                }

                bool isOldValid;
                try
                {
                    isOldValid = BCrypt.Net.BCrypt.Verify(oldPassword, user.PasswordHash);
                }
                catch
                {
                    isOldValid = user.PasswordHash == oldPassword;
                }

                if (!isOldValid)
                {
                    return BadRequest(ApiResponse.Fail("Mật khẩu hiện tại không chính xác!"));
                }
            }

            // Cập nhật mật khẩu mới
            user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword.Trim());
            user.UpdatedAt = DateTime.Now;

            var updateResult = await _userRepo.UpdateAsync(user);
            if (!updateResult)
            {
                return StatusCode(StatusCodes.Status500InternalServerError, ApiResponse.Fail("Không thể cập nhật mật khẩu vào database. Vui lòng thử lại."));
            }

            // Ghi nhận AuditLog ChangePassword
            await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.ChangePassword, "User", user.Id, user.Username);

            return Ok(ApiResponse.Ok("Đổi mật khẩu tài khoản thành công!"));
        }

        /// <summary>
        /// Chuyển đổi trạng thái Khóa / Mở khóa tài khoản (Chỉ dành cho Admin)
        /// </summary>
        [HttpPatch("{id}/toggle-status")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> ToggleStatus(string id)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null || user.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy tài khoản người dùng."));

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (user.Id == currentUserId)
            {
                return BadRequest(ApiResponse.Fail("Bạn không thể tự khóa tài khoản của chính mình."));
            }

            user.IsActive = !user.IsActive;
            user.UpdatedAt = DateTime.Now;

            await _userRepo.UpdateAsync(user);

            // Ghi nhận AuditLog ToggleStatus
            var reason = user.IsActive ? "Mở khóa tài khoản" : "Khóa tài khoản";
            await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Update, "User", user.Id, user.Username, reason: reason);

            return Ok(ApiResponse<UserDto>.Ok(MapToDto(user), user.IsActive ? "Đã mở khóa tài khoản thành công." : "Đã khóa tài khoản thành công."));
        }

        /// <summary>
        /// Xóa mềm tài khoản người dùng (Chỉ dành cho Admin)
        /// </summary>
        [HttpDelete("{id}")]
        [Authorize(Roles = "Admin")]
        public async Task<IActionResult> DeleteUser(string id, [FromQuery] string? reason = null)
        {
            var user = await _userRepo.GetByIdAsync(id);
            if (user == null || user.IsDeleted) return NotFound(ApiResponse.Fail("Không tìm thấy tài khoản người dùng."));

            var currentUserId = User.FindFirstValue(ClaimTypes.NameIdentifier) ?? User.FindFirstValue("sub");
            if (user.Id == currentUserId)
            {
                return BadRequest(ApiResponse.Fail("Bạn không thể tự xóa tài khoản đang đăng nhập của chính mình."));
            }

            if (user.Role == UserRole.Admin)
            {
                var adminCount = await _userRepo.CountAsync(u => u.Role == UserRole.Admin && !u.IsDeleted);
                if (adminCount <= 1)
                {
                    return BadRequest(ApiResponse.Fail("Không thể xóa tài khoản Quản Trị Viên cuối cùng của hệ thống."));
                }
            }

            await _userRepo.DeleteAsync(id);

            // Ghi nhận AuditLog Delete
            var diff = AuditDiffHelper.ComputeDiff<User>(user, null);
            await _auditQueue.LogActivityAsync(User, HttpContext, AuditActionType.Delete, "User", user.Id, user.Username, diff, reason: reason);

            return Ok(ApiResponse.Ok("Đã xóa tài khoản người dùng thành công (dữ liệu được chuyển vào thùng rác)."));
        }
    }
}
