using System;
using MongoDB.Bson;
using MongoDB.Bson.Serialization;
using MongoDB.Bson.Serialization.Attributes;
using MongoDB.Bson.Serialization.Serializers;

namespace PhuXuanParkingSystem.Models.ValueObjects
{
    /// <summary>
    /// Value Object quản lý đường dẫn lưu trữ file ảnh Snapshot trên ổ đĩa / mạng UNC
    /// </summary>
    [BsonSerializer(typeof(ImageStoragePathBsonSerializer))]
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

    /// <summary>
    /// Custom BSON Serializer cho ImageStoragePath để serialize/deserialize trực tiếp với BSON String trong MongoDB
    /// </summary>
    public class ImageStoragePathBsonSerializer : SerializerBase<ImageStoragePath>
    {
        public override ImageStoragePath Deserialize(BsonDeserializationContext context, BsonDeserializationArgs args)
        {
            var bsonType = context.Reader.CurrentBsonType;
            if (bsonType == BsonType.Null)
            {
                context.Reader.ReadNull();
                return ImageStoragePath.Empty;
            }
            if (bsonType == BsonType.String)
            {
                return new ImageStoragePath(context.Reader.ReadString());
            }

            var doc = BsonSerializer.Deserialize<BsonDocument>(context.Reader);
            if (doc != null && doc.Contains("Path"))
            {
                return new ImageStoragePath(doc["Path"].AsString);
            }

            return ImageStoragePath.Empty;
        }

        public override void Serialize(BsonSerializationContext context, BsonSerializationArgs args, ImageStoragePath value)
        {
            context.Writer.WriteString(value?.Path ?? string.Empty);
        }
    }
}
