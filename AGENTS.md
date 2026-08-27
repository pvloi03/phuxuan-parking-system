# Quy Tắc Dự Án (Project Rules)

## 1. Ưu tiên dùng MCP server / công cụ có sẵn trước khi tự xử lý

Trước khi bắt đầu bất kỳ tác vụ nào, kiểm tra xem các MCP server đã kết nối có thể giúp không, thay vì tự làm thủ công:

- **memory** — dùng để lưu trữ và truy xuất ngữ cảnh dài hạn, tiến độ dự án, **Bắt buộc:** Mỗi khi bắt đầu một phiên làm việc mới (phiên chat mới), luôn chủ động gọi tool từ MCP `memory` để nắm bắt và đồng bộ lại toàn bộ ngữ cảnh trước đó trước khi trả lời hoặc thực hiện tác vụ.
- **codegraph** — dùng để phân tích cấu trúc code, tìm quan hệ giữa các file/function/class trong project. Ưu tiên dùng khi cần hiểu kiến trúc code trước khi sửa.
- **context7** — dùng để tra cứu tài liệu / API mới nhất của thư viện, framework đang dùng trong project. Ưu tiên dùng thay vì suy đoán từ kiến thức cũ khi làm việc với thư viện bên ngoài.

## 2. Trước khi sửa code

- Dùng `codegraph` để hiểu tác động của thay đổi (những gì gọi đến / bị gọi bởi đoạn code sắp sửa).
- Không tự đoán API của thư viện ngoài — tra cứu qua `context7` (hoặc công cụ tra cứu tài liệu) nếu không chắc chắn.

## 3. Quy chuẩn kiến trúc & Thực hiện Task

- Luôn tuân thủ kiến trúc **Clean Architecture**
- **Khởi động phiên làm việc mới:** Luôn đọc lại ngữ cảnh từ MCP `memory` để nắm rõ trạng thái hiện tại, kiến trúc và các task đã/đang làm.

## 4. Quy chuẩn Tài liệu & Quản lý Task (Documentation & Tasks)

- **Vị trí lưu trữ:**
  - Mọi tài liệu kỹ thuật, thiết kế, kiến trúc, hướng dẫn: lưu trong thư mục `docs/` (`C:\Users\ADMIN\source\repos\HPParkingSystem\docs`).
  - Mọi file quản lý task, checklist, kế hoạch thực hiện: lưu trong thư mục `docs/tasks/` (`C:\Users\ADMIN\source\repos\HPParkingSystem\docs\tasks`).
- **Nguyên tắc tổ chức file Markdown (.md):**
  - **Chia nhỏ file:** Tránh gom nội dung quá lớn vào 1 file duy nhất. Chia nhỏ thành các file `.md` theo module, phân hệ, hoặc từng task cụ thể để dễ theo dõi và quản lý.
  - **Đầy đủ thông tin:** Mỗi file `.md` phải có cấu trúc rõ ràng, đầy đủ thông tin cần thiết (Mục tiêu, Bối cảnh/Phạm vi, Chi tiết thiết kế/thực hiện, Checklist tiến độ, Lưu ý kỹ thuật) để có thể hiểu và làm việc độc lập.

## 5. Xác nhận với người dùng trước khi bắt đầu Task

- **Bắt buộc:** Luôn trình bày kế hoạch, phạm vi thay đổi và **xác nhận với người dùng trước khi bắt đầu thực hiện bất kỳ task nào**.
- Không tự ý thực hiện code hay thay đổi lớn khi chưa có sự xác nhận/duyệt từ người dùng.

## 6. Quy chuẩn Git & Branching

- **Luôn tạo branch riêng cho từng task:** Trước khi bắt đầu thực hiện bất kỳ task nào, luôn tạo và chuyển sang branch tương ứng từ `main` (quy ước đặt tên: `task/<id>-<ten-task>`, ví dụ: `task/001-setup-solution-skeleton`).
- **Cam kết và đẩy mã nguồn:** Sau khi hoàn thành và kiểm thử task, thực hiện commit với thông điệp rõ ràng (theo chuẩn Conventional Commits) và đẩy branch lên GitHub.

