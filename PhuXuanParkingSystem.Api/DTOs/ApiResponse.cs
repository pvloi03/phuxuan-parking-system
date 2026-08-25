using System;
using System.Collections.Generic;

namespace PhuXuanParkingSystem.Api.DTOs
{
    /// <summary>
    /// Chuẩn cấu trúc phản hồi thống nhất cho toàn bộ API hệ thống
    /// </summary>
    /// <typeparam name="T">Kiểu dữ liệu của payload trả về</typeparam>
    public class ApiResponse<T>
    {
        public bool Success { get; set; }
        public string Message { get; set; } = string.Empty;
        public T? Data { get; set; }
        public List<string>? Errors { get; set; }
        public DateTime Timestamp { get; set; } = DateTime.UtcNow;

        public static ApiResponse<T> Ok(T data, string message = "Thao tác thành công.") =>
            new() { Success = true, Data = data, Message = message };

        public static ApiResponse<T> Fail(string message, List<string>? errors = null) =>
            new() { Success = false, Message = message, Errors = errors };
    }

    /// <summary>
    /// Chuẩn cấu trúc phản hồi không kèm kiểu Generic
    /// </summary>
    public class ApiResponse : ApiResponse<object>
    {
        public static ApiResponse Ok(string message = "Thao tác thành công.") =>
            new() { Success = true, Message = message };

        public static new ApiResponse Fail(string message, List<string>? errors = null) =>
            new() { Success = false, Message = message, Errors = errors };
    }
}
