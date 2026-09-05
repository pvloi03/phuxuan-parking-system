using PhuXuanParkingSystem.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Text.Json;

namespace PhuXuanParkingSystem.Api.Helpers
{
    public class AuditDiffResult
    {
        public string? OldValues { get; set; }
        public string? NewValues { get; set; }
        public List<string> ChangedProperties { get; set; } = new();
        public bool HasChanges => ChangedProperties.Count > 0;
    }

    public static class AuditDiffHelper
    {
        private static readonly HashSet<string> SensitivePropertyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "PasswordHash", "Password", "SecretKey", "Token", "RefreshToken", "PrivateKey", "MachineKey"
        };

        private static readonly HashSet<string> IgnoredPropertyNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "UpdatedAt", "DeletedAt"
        };

        /// <summary>
        /// Tạo snapshot thuộc tính thô của entity trước khi chỉnh sửa trong bộ nhớ
        /// </summary>
        public static Dictionary<string, object?> TakeSnapshot(object entity)
        {
            var dict = new Dictionary<string, object?>(StringComparer.OrdinalIgnoreCase);
            if (entity == null) return dict;

            var properties = entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !IgnoredPropertyNames.Contains(p.Name));

            foreach (var prop in properties)
            {
                dict[prop.Name] = prop.GetValue(entity);
            }

            return dict;
        }

        /// <summary>
        /// Tính toán Diff giữa Snapshot cũ và thực thể mới đã được cập nhật
        /// Tự động che giấu dữ liệu nhạy cảm
        /// </summary>
        public static AuditDiffResult ComputeDiffFromSnapshot(Dictionary<string, object?>? oldSnapshot, object? newEntity)
        {
            var result = new AuditDiffResult();
            if (oldSnapshot == null && newEntity != null)
            {
                var (dict, props) = ExtractProperties(newEntity);
                result.NewValues = JsonSerializer.Serialize(dict);
                result.ChangedProperties = props;
                return result;
            }

            if (oldSnapshot != null && newEntity == null)
            {
                var oldChanges = new Dictionary<string, object?>();
                var props = new List<string>();
                foreach (var kvp in oldSnapshot)
                {
                    props.Add(kvp.Key);
                    var isSensitive = SensitivePropertyNames.Contains(kvp.Key);
                    oldChanges[kvp.Key] = isSensitive ? "******" : FormatValue(kvp.Value);
                }
                result.OldValues = JsonSerializer.Serialize(oldChanges);
                result.ChangedProperties = props;
                return result;
            }

            if (oldSnapshot == null || newEntity == null) return result;

            var properties = newEntity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !IgnoredPropertyNames.Contains(p.Name));

            var oldDiff = new Dictionary<string, object?>();
            var newDiff = new Dictionary<string, object?>();

            foreach (var prop in properties)
            {
                oldSnapshot.TryGetValue(prop.Name, out var oldVal);
                var newVal = prop.GetValue(newEntity);

                if (!AreValuesEqual(oldVal, newVal))
                {
                    result.ChangedProperties.Add(prop.Name);
                    var isSensitive = IsSensitive(prop);
                    oldDiff[prop.Name] = isSensitive ? "******" : FormatValue(oldVal);
                    newDiff[prop.Name] = isSensitive ? "******" : FormatValue(newVal);
                }
            }

            if (result.ChangedProperties.Count > 0)
            {
                result.OldValues = JsonSerializer.Serialize(oldDiff);
                result.NewValues = JsonSerializer.Serialize(newDiff);
            }

            return result;
        }

        public static AuditDiffResult ComputeDiff(object? oldEntity, object? newEntity)
        {
            var result = new AuditDiffResult();

            if (oldEntity == null && newEntity != null)
            {
                var (dict, props) = ExtractProperties(newEntity);
                result.NewValues = JsonSerializer.Serialize(dict);
                result.ChangedProperties = props;
                return result;
            }

            if (oldEntity != null && newEntity == null)
            {
                var (dict, props) = ExtractProperties(oldEntity);
                result.OldValues = JsonSerializer.Serialize(dict);
                result.ChangedProperties = props;
                return result;
            }

            if (oldEntity == null || newEntity == null) return result;

            var properties = oldEntity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !IgnoredPropertyNames.Contains(p.Name));

            var oldChanges = new Dictionary<string, object?>();
            var newChanges = new Dictionary<string, object?>();

            foreach (var prop in properties)
            {
                var oldVal = prop.GetValue(oldEntity);
                var newVal = newEntity.GetType().GetProperty(prop.Name)?.GetValue(newEntity);

                if (!AreValuesEqual(oldVal, newVal))
                {
                    result.ChangedProperties.Add(prop.Name);
                    var isSensitive = IsSensitive(prop);
                    oldChanges[prop.Name] = isSensitive ? "******" : FormatValue(oldVal);
                    newChanges[prop.Name] = isSensitive ? "******" : FormatValue(newVal);
                }
            }

            if (result.ChangedProperties.Count > 0)
            {
                result.OldValues = JsonSerializer.Serialize(oldChanges);
                result.NewValues = JsonSerializer.Serialize(newChanges);
            }

            return result;
        }

        public static AuditDiffResult ComputeDiff<T>(T? oldEntity, T? newEntity) where T : class
        {
            var result = new AuditDiffResult();

            if (oldEntity == null && newEntity != null)
            {
                var (dict, props) = ExtractProperties(newEntity);
                result.NewValues = JsonSerializer.Serialize(dict);
                result.ChangedProperties = props;
                return result;
            }

            if (oldEntity != null && newEntity == null)
            {
                var (dict, props) = ExtractProperties(oldEntity);
                result.OldValues = JsonSerializer.Serialize(dict);
                result.ChangedProperties = props;
                return result;
            }

            if (oldEntity == null || newEntity == null) return result;

            var properties = typeof(T).GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !IgnoredPropertyNames.Contains(p.Name));

            var oldChanges = new Dictionary<string, object?>();
            var newChanges = new Dictionary<string, object?>();

            foreach (var prop in properties)
            {
                var oldVal = prop.GetValue(oldEntity);
                var newVal = prop.GetValue(newEntity);

                if (!AreValuesEqual(oldVal, newVal))
                {
                    result.ChangedProperties.Add(prop.Name);
                    var isSensitive = IsSensitive(prop);
                    oldChanges[prop.Name] = isSensitive ? "******" : FormatValue(oldVal);
                    newChanges[prop.Name] = isSensitive ? "******" : FormatValue(newVal);
                }
            }

            if (result.ChangedProperties.Count > 0)
            {
                result.OldValues = JsonSerializer.Serialize(oldChanges);
                result.NewValues = JsonSerializer.Serialize(newChanges);
            }

            return result;
        }

        private static (Dictionary<string, object?> Dict, List<string> Props) ExtractProperties(object entity)
        {
            var dict = new Dictionary<string, object?>();
            var props = new List<string>();

            var properties = entity.GetType().GetProperties(BindingFlags.Public | BindingFlags.Instance)
                .Where(p => p.CanRead && !IgnoredPropertyNames.Contains(p.Name));

            foreach (var prop in properties)
            {
                props.Add(prop.Name);
                dict[prop.Name] = IsSensitive(prop) ? "******" : FormatValue(prop.GetValue(entity));
            }

            return (dict, props);
        }

        private static bool IsSensitive(PropertyInfo prop) =>
            SensitivePropertyNames.Contains(prop.Name) || prop.GetCustomAttribute<SensitiveDataAttribute>() != null;

        private static bool AreValuesEqual(object? val1, object? val2)
        {
            if (val1 == null && val2 == null) return true;
            if (val1 == null || val2 == null) return false;
            if (val1 is DateTime dt1 && val2 is DateTime dt2) return Math.Abs((dt1 - dt2).TotalSeconds) < 1;
            return val1.Equals(val2) || val1.ToString() == val2.ToString();
        }

        private static object? FormatValue(object? value) => value switch
        {
            null => null,
            DateTime dt => dt.ToString("yyyy-MM-dd HH:mm:ss"),
            Enum e => e.ToString(),
            _ => value
        };
    }
}
