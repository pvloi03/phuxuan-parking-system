using Humanizer;
using MongoDB.Driver;
using PhuXuanParkingSystem.Models.Common;
using PhuXuanParkingSystem.Models.Data;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace PhuXuanParkingSystem.Repositories
{
    /// <summary>
    /// Lớp cơ sở triển khai IRepository&lt;T&gt; cho MongoDB Driver
    /// Tự động lấy tên Collection qua thư viện Humanizer (Pluralize),
    /// xử lý bộ lọc Xóa mềm (Soft Delete) và quản lý dấu thời gian (Timestamps).
    /// </summary>
    /// <typeparam name="T">Thực thể kế thừa BaseEntity</typeparam>
    public class MongoRepository<T> : IRepository<T> where T : BaseEntity
    {
        protected readonly IMongoCollection<T> _collection;
        protected readonly MongoDbContext _context;

        /// <summary>
        /// Khởi tạo MongoRepository với MongoDbContext, tự động suy luận tên Collection qua Humanizer (Pluralize)
        /// </summary>
        public MongoRepository(MongoDbContext context)
            : this(context, typeof(T).Name.Pluralize())
        {
        }

        /// <summary>
        /// Khởi tạo MongoRepository với tên Collection tùy chỉnh
        /// </summary>
        public MongoRepository(MongoDbContext context, string collectionName)
        {
            _context = context ?? MongoDbContext.Instance;
            string colName = string.IsNullOrWhiteSpace(collectionName)
                ? typeof(T).Name.Pluralize()
                : collectionName;
            _collection = _context.Database.GetCollection<T>(colName);
        }

        /// <summary>
        /// Constructor mặc định sử dụng MongoDbContext.Instance
        /// </summary>
        public MongoRepository()
            : this(MongoDbContext.Instance)
        {
        }

        public MongoRepository(IMongoCollection<T> collection, MongoDbContext? context = null)
        {
            _collection = collection ?? throw new ArgumentNullException(nameof(collection));
            _context = context ?? MongoDbContext.Instance;
        }

        public IMongoCollection<T> Collection => _collection;

        /// <summary>
        /// Tạo bộ lọc tự động kết hợp điều kiện chưa bị xóa mềm (IsDeleted == false)
        /// </summary>
        protected FilterDefinition<T> CombineSoftDeleteFilter(FilterDefinition<T>? filter = null)
        {
            var notDeleted = Builders<T>.Filter.Eq(x => x.IsDeleted, false);
            return filter != null ? Builders<T>.Filter.And(notDeleted, filter) : notDeleted;
        }

        public virtual async Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id)) return null;

            FilterDefinition<T> idFilter;
            if (MongoDB.Bson.ObjectId.TryParse(id, out var objectId))
            {
                idFilter = Builders<T>.Filter.Or(
                    Builders<T>.Filter.Eq(x => x.Id, id),
                    Builders<T>.Filter.Eq("_id", objectId),
                    Builders<T>.Filter.Eq("_id", id)
                );
            }
            else
            {
                idFilter = Builders<T>.Filter.Or(
                    Builders<T>.Filter.Eq(x => x.Id, id),
                    Builders<T>.Filter.Eq("_id", id)
                );
            }

            var filter = CombineSoftDeleteFilter(idFilter);
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public virtual async Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default)
        {
            var filter = CombineSoftDeleteFilter();
            return await _collection.Find(filter).ToListAsync(cancellationToken);
        }

        public virtual async Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var filter = CombineSoftDeleteFilter(Builders<T>.Filter.Where(predicate));
            return await _collection.Find(filter).ToListAsync(cancellationToken);
        }

        public virtual async Task<IReadOnlyList<T>> FindAsync(FilterDefinition<T> filter, SortDefinition<T>? sort = null, int skip = 0, int limit = 0, CancellationToken cancellationToken = default)
        {
            var combinedFilter = CombineSoftDeleteFilter(filter);
            var query = _collection.Find(combinedFilter);
            if (sort != null) query = query.Sort(sort);
            if (skip > 0) query = query.Skip(skip);
            if (limit > 0) query = query.Limit(limit);
            return await query.ToListAsync(cancellationToken);
        }

        public virtual async Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var filter = CombineSoftDeleteFilter(Builders<T>.Filter.Where(predicate));
            return await _collection.Find(filter).FirstOrDefaultAsync(cancellationToken);
        }

        public virtual async Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
        {
            var filter = CombineSoftDeleteFilter(Builders<T>.Filter.Where(predicate));
            return await _collection.Find(filter).AnyAsync(cancellationToken);
        }

        public virtual async Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default)
        {
            var filter = predicate != null
                ? CombineSoftDeleteFilter(Builders<T>.Filter.Where(predicate))
                : CombineSoftDeleteFilter();

            return await _collection.CountDocumentsAsync(filter, null, cancellationToken);
        }

        public virtual async Task<long> CountAsync(FilterDefinition<T> filter, CancellationToken cancellationToken = default)
        {
            var combinedFilter = CombineSoftDeleteFilter(filter);
            return await _collection.CountDocumentsAsync(combinedFilter, null, cancellationToken);
        }

        public virtual async Task<T> AddAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));

            if (entity.CreatedAt == default) entity.CreatedAt = DateTime.Now;
            entity.IsDeleted = false;

            await _collection.InsertOneAsync(entity, null, cancellationToken);
            return entity;
        }

        public virtual async Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default)
        {
            if (entities == null) throw new ArgumentNullException(nameof(entities));
            var list = new List<T>(entities);
            if (list.Count == 0) return;

            foreach (var item in list)
            {
                if (item.CreatedAt == default) item.CreatedAt = DateTime.Now;
                item.IsDeleted = false;
            }

            await _collection.InsertManyAsync(list, null, cancellationToken);
        }

        public virtual async Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default)
        {
            if (entity == null) throw new ArgumentNullException(nameof(entity));
            entity.UpdatedAt = DateTime.Now;

            var filter = CombineSoftDeleteFilter(Builders<T>.Filter.Eq(x => x.Id, entity.Id));
            var result = await _collection.ReplaceOneAsync(filter, entity, (ReplaceOptions?)null, cancellationToken);
            return result.MatchedCount > 0;
        }

        public virtual async Task<bool> DeleteAsync(string id, bool softDelete = true, CancellationToken cancellationToken = default)
        {
            if (string.IsNullOrWhiteSpace(id)) return false;

            if (softDelete)
            {
                var filter = CombineSoftDeleteFilter(Builders<T>.Filter.Eq(x => x.Id, id));
                var update = Builders<T>.Update
                    .Set(x => x.IsDeleted, true)
                    .Set(x => x.DeletedAt, DateTime.Now)
                    .Set(x => x.UpdatedAt, DateTime.Now);

                var result = await _collection.UpdateOneAsync(filter, update, null, cancellationToken);
                return result.MatchedCount > 0;
            }
            else
            {
                var filter = Builders<T>.Filter.Eq(x => x.Id, id);
                var result = await _collection.DeleteOneAsync(filter, cancellationToken);
                return result.DeletedCount > 0;
            }
        }
    }
}
