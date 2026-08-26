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

        public string FormattedValue => FormatDisplay(Value);

        public static string Clean(string? input)
        {
            if (string.IsNullOrWhiteSpace(input)) return string.Empty;
            return Regex.Replace(input!.Trim().ToUpperInvariant(), @"[^A-Z0-9]", "");
        }

        public static string FormatDisplay(string? input)
        {
            string clean = Clean(input);
            if (string.IsNullOrEmpty(clean)) return string.Empty;

            // 1. Biển 5 số có ký hiệu đặc biệt (LD, NN, NG, QT, DA) e.g. 88LD00122 -> 88LD-001.22
            var matchSpecial5 = Regex.Match(clean, @"^([0-9]{2})(LD|NN|NG|QT|DA)([0-9]{3})([0-9]{2})$");
            if (matchSpecial5.Success)
            {
                return $"{matchSpecial5.Groups[1].Value}{matchSpecial5.Groups[2].Value}-{matchSpecial5.Groups[3].Value}.{matchSpecial5.Groups[4].Value}";
            }

            // 2. Biển 5 số tiêu chuẩn (ô tô hoặc xe máy) e.g. 29A12345 -> 29A-123.45, 29B112345 -> 29B1-123.45
            var matchStd5 = Regex.Match(clean, @"^([0-9]{2}[A-Z]{1,2}[0-9]?)([0-9]{3})([0-9]{2})$");
            if (matchStd5.Success)
            {
                return $"{matchStd5.Groups[1].Value}-{matchStd5.Groups[2].Value}.{matchStd5.Groups[3].Value}";
            }

            // 3. Biển 4 số tiêu chuẩn e.g. 29A1234 -> 29A-1234, 29B11234 -> 29B1-1234
            var matchStd4 = Regex.Match(clean, @"^([0-9]{2}[A-Z]{1,2}[0-9]?)([0-9]{4})$");
            if (matchStd4.Success)
            {
                return $"{matchStd4.Groups[1].Value}-{matchStd4.Groups[2].Value}";
            }

            return clean;
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
