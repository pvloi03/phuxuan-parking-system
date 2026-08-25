using HPParkingThaiThuy.Models.Common;
using System;
using System.Collections.Generic;
using System.Linq.Expressions;
using System.Threading;
using System.Threading.Tasks;

namespace HPParkingThaiThuy.Repositories
{
    /// <summary>
    /// Interface tổng quát định nghĩa các thao tác dữ liệu cơ bản (CRUD, Query, Soft-Delete)
    /// </summary>
    /// <typeparam name="T">Thực thể kế thừa BaseEntity</typeparam>
    public interface IRepository<T> where T : BaseEntity
    {
        // =========================================================================
        // --- 1. TRUY VẤN DỮ LIỆU (READ) ---
        // =========================================================================
        
        /// <summary>
        /// Lấy một bản ghi theo Id (tự động bỏ qua bản ghi đã xóa mềm)
        /// </summary>
        Task<T?> GetByIdAsync(string id, CancellationToken cancellationToken = default);

        /// <summary>
        /// Lấy toàn bộ danh sách bản ghi chưa bị xóa mềm
        /// </summary>
        Task<IReadOnlyList<T>> GetAllAsync(CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm kiếm danh sách bản ghi theo biểu thức điều kiện (Predicate)
        /// </summary>
        Task<IReadOnlyList<T>> FindAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Tìm bản ghi đầu tiên khớp điều kiện
        /// </summary>
        Task<T?> FindOneAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Kiểm tra xem có tồn tại bản ghi khớp điều kiện hay không
        /// </summary>
        Task<bool> ExistsAsync(Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default);

        /// <summary>
        /// Đếm số lượng bản ghi thỏa mãn điều kiện (hoặc toàn bộ nếu predicate = null)
        /// </summary>
        Task<long> CountAsync(Expression<Func<T, bool>>? predicate = null, CancellationToken cancellationToken = default);

        // =========================================================================
        // --- 2. THAO TÁC GHI (WRITE) ---
        // =========================================================================

        /// <summary>
        /// Thêm mới một bản ghi
        /// </summary>
        Task<T> AddAsync(T entity, CancellationToken cancellationToken = default);

        /// <summary>
        /// Thêm mới hàng loạt bản ghi
        /// </summary>
        Task AddRangeAsync(IEnumerable<T> entities, CancellationToken cancellationToken = default);

        /// <summary>
        /// Cập nhật thông tin toàn bộ bản ghi theo Id
        /// </summary>
        Task<bool> UpdateAsync(T entity, CancellationToken cancellationToken = default);

        // =========================================================================
        // --- 3. THAO TÁC XÓA (DELETE / SOFT-DELETE) ---
        // =========================================================================

        /// <summary>
        /// Xóa bản ghi theo Id.
        /// </summary>
        /// <param name="id">Mã định danh bản ghi</param>
        /// <param name="softDelete">True: đánh dấu IsDeleted = true, False: xóa vĩnh viễn khỏi CSDL</param>
        Task<bool> DeleteAsync(string id, bool softDelete = true, CancellationToken cancellationToken = default);
    }
}
