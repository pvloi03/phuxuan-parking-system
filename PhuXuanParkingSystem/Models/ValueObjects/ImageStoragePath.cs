using System;

namespace PhuXuanParkingSystem.Models.ValueObjects
{
    /// <summary>
    /// Value Object quản lý đường dẫn lưu trữ file ảnh Snapshot trên ổ đĩa / mạng UNC
    /// </summary>
    public class ImageStoragePath : IEquatable<ImageStoragePath>
    {
        public string Path { get; set; } = string.Empty;

        public static ImageStoragePath Empty => new(string.Empty);

        public bool IsEmpty => string.IsNullOrWhiteSpace(Path);

        public ImageStoragePath() { }

        public ImageStoragePath(string path)
        {
            Path = path ?? string.Empty;
        }

        public static ImageStoragePath Create(string? path) => new(path ?? string.Empty);

        public override string ToString() => Path;

        public bool Equals(ImageStoragePath? other)
        {
            if (other is null) return false;
            return string.Equals(Path, other.Path, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj) => obj is ImageStoragePath other && Equals(other);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Path);

        public static implicit operator string(ImageStoragePath imagePath) => imagePath?.Path ?? string.Empty;
        public static implicit operator ImageStoragePath(string path) => Create(path);
    }
}
