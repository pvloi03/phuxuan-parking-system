using System;
using System.Text.RegularExpressions;

namespace PhuXuanParkingSystem.Models.ValueObjects
{
    /// <summary>
    /// Value Object đại diện cho Biển số xe đã được chuẩn hóa
    /// Tự động loại bỏ dấu chấm, dấu gạch ngang, khoảng trắng thừa
    /// </summary>
    public class PlateNumber : IEquatable<PlateNumber>
    {
        public string Value { get; set; } = string.Empty;

        public PlateNumber() { }

        public PlateNumber(string value)
        {
            Value = Clean(value);
        }

        public static PlateNumber Create(string? value)
        {
            return new PlateNumber(value ?? string.Empty);
        }

        public static string Clean(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return Regex.Replace(input!.Trim().ToUpperInvariant(), @"[^A-Z0-9]", "");
        }

        public override string ToString() => Value;

        public bool Equals(PlateNumber? other)
        {
            if (other is null) return false;
            return string.Equals(Value, other.Value, StringComparison.OrdinalIgnoreCase);
        }

        public override bool Equals(object? obj) => obj is PlateNumber other && Equals(other);

        public override int GetHashCode() => StringComparer.OrdinalIgnoreCase.GetHashCode(Value);

        public static bool operator ==(PlateNumber? left, PlateNumber? right)
        {
            if (left is null) return right is null;
            return left.Equals(right);
        }

        public static bool operator !=(PlateNumber? left, PlateNumber? right) => !(left == right);

        public static implicit operator string(PlateNumber? plateNumber) => plateNumber?.Value ?? string.Empty;
        public static implicit operator PlateNumber(string value) => Create(value);
    }
}
