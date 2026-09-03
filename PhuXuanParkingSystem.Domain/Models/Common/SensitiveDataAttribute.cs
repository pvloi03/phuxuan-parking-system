using System;

namespace PhuXuanParkingSystem.Models.Common
{
    /// <summary>
    /// Attribute đánh dấu các thuộc tính chứa dữ liệu nhạy cảm (mật khẩu, token, khóa bí mật)
    /// để bộ tạo Audit Diff tự động che giấu (masking) thành "******".
    /// </summary>
    [AttributeUsage(AttributeTargets.Property, AllowMultiple = false, Inherited = true)]
    public class SensitiveDataAttribute : Attribute
    {
    }
}
