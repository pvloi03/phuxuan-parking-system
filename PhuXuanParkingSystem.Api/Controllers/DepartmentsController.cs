using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using PhuXuanParkingSystem.Api.DTOs;
using PhuXuanParkingSystem.Models.Entities;
using PhuXuanParkingSystem.Repositories;

namespace PhuXuanParkingSystem.Api.Controllers
{
    [ApiController]
    [Route("api/[controller]")]
    [Authorize]
    public class DepartmentsController : ControllerBase
    {
        private readonly IRepository<Department> _deptRepo;

        public DepartmentsController(IRepository<Department> deptRepo)
        {
            _deptRepo = deptRepo;
        }

        [HttpGet]
        public async Task<IActionResult> GetList()
        {
            var items = await _deptRepo.FindAsync(d => !d.IsDeleted);
            return Ok(ApiResponse<IReadOnlyList<Department>>.Ok(items, "Lấy danh sách phòng ban thành công."));
        }

        [HttpPost]
        public async Task<IActionResult> Create([FromBody] Department dept)
        {
            if (string.IsNullOrWhiteSpace(dept.Name))
            {
                return BadRequest(ApiResponse.Fail("Tên phòng ban không được để trống."));
            }

            await _deptRepo.AddAsync(dept);
            return Ok(ApiResponse<Department>.Ok(dept, "Thêm phòng ban thành công."));
        }
    }
}
