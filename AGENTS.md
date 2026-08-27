# AGENTS.md — Quy Tắc Agent

> **Mục đích**: Hướng dẫn agent làm việc với codebase một cách nhất quán.

---

## Agent Skills

### Issue tracker

GitHub Issues (pvloi03/phuxuan-parking-system). Xem `docs/agents/issue-tracker.md`.

### Domain docs

Single-context layout. Xem `docs/agents/domain.md`.

---

## 1. MCP Tools - Ưu Tiên Dùng Trước

Trước khi tự xử lý, kiểm tra MCP tools:

| MCP Tool | Dùng khi nào |
|---------|--------------|
| **codegraph** | Phân tích cấu trúc code, tìm relationships giữa files/classes/functions |
| **context7** | Tra cứu tài liệu API mới nhất của thư viện |
| **memory** | Lưu/truy xuất context dài hạn, tiến độ dự án |

---

## 2. Trước Khi Sửa Code

- **Dùng `codegraph`** để hiểu blast radius (những gì gọi đến / bị gọi bởi code sắp sửa)
- **Không tự đoán API** của thư viện ngoài — tra cứu qua `context7`
- **Đọc `CONTEXT.md`** để hiểu domain language

---

## 3. Xác Nhận Trước Khi Thực Hiện

**Bắt buộc:** Trình bày kế hoạch và xác nhận với user trước khi bắt đầu task.

---

## 4. Git & Branching

- **Branch cho mỗi task:** `task/<id>-<ten-task>` (ví dụ: `task/022-license-key-system`)
- **Commit message:** Conventional Commits (`feat:`, `fix:`, `docs:`)
- **Push:** Sau khi hoàn thành và test

---

## 5. Tài Liệu & Task

| Location | Nội dung |
|----------|----------|
| `docs/` | Tài liệu kỹ thuật, thiết kế, hướng dẫn |
| `docs/tasks/` | Task specifications, checklists |
| `CONTEXT.md` | Domain glossary, business rules |

---

## 6. Build & Test

```bash
# Build
dotnet build PhuXuanParkingSystem.slnx

# Test
dotnet test tests/PhuXuanParkingSystem.Tests/
dotnet test tests/PhuXuanParkingSystem.Api.Tests/

# Run API
dotnet run --project PhuXuanParkingSystem.Api/PhuXuanParkingSystem.Api.csproj

# Run WinForms
dotnet run --project PhuXuanParkingSystem/PhuXuanParkingSystem.csproj
```
