# Tích hợp FE - API Xóa Skill (Admin)

Tài liệu hướng dẫn FE tích hợp API Xóa Skill (Soft Delete) trong trang quản trị Admin.

---

## 1. Chi tiết Endpoint

```http
DELETE /api/admin/skills/{skillId}
```

- **Mô tả**: Vô hiệu hóa (soft delete) một skill khỏi hệ thống.
- **Quyền**: Yêu cầu `Bearer Token` và Role `admin`.
- **Tham số Path**: `skillId` (Guid) - ID của skill cần xóa.

### Behavior của Backend
- API không xóa vĩnh viễn dữ liệu (hard delete) khỏi Database.
- Thay vào đó, API cập nhật trường `isDeleted = true`, `isActive = false` và cập nhật `updatedAt`.
- Nếu skill đã có trạng thái `isDeleted = true` từ trước, API vẫn trả về thành công (200 OK) mà không báo lỗi.
- Skill sau khi bị xóa sẽ không hiển thị trên bất kỳ API nào, kể cả trang Admin, và không cho phép gắn mới vào profile.

---

## 2. Response từ Backend

### 2.1. Xóa thành công
```http
HTTP/1.1 200 OK
```
*(Không có response body)*

### 2.2. Lỗi không tìm thấy (404)
```http
HTTP/1.1 404 Not Found
```
```json
{
  "errorCode": "SKILL_NOT_FOUND",
  "errorMessage": "The specified skill does not exist."
}
```

### 2.3. Lỗi xác thực / Phân quyền (401 & 403)
```http
HTTP/1.1 401 Unauthorized
```
```http
HTTP/1.1 403 Forbidden
```

---

## 3. Khuyến nghị UX/UI cho FE

1. **Hiển thị hộp thoại xác nhận**: 
   - Trước khi gọi API xóa, cần hiện popup xác nhận (VD: *"Bạn có chắc chắn muốn xóa kỹ năng này? Người dùng sẽ không thể chọn kỹ năng này nữa."*).
2. **Xử lý Response**:
   - Nhận được HTTP 200: Thông báo Toast thành công, gỡ bỏ skill khỏi UI (hoặc cập nhật lại trạng thái nếu UI hiển thị cả skill bị ẩn).
   - Nhận được mã lỗi HTTP 404: Báo lỗi *"Kỹ năng không tồn tại hoặc đã bị xóa."*
3. **Mối quan hệ với tính năng ẩn skill (Hide)**:
   - Thay vì chỉ cập nhật `isActive = false` như chức năng Hide, tính năng Xóa đánh dấu cờ `isDeleted = true`. Khi đó skill biến mất khỏi danh sách Admin, trong khi chức năng Hide (ẩn) thì skill vẫn nằm trong danh sách nhưng trạng thái `isActive = false`. FE có thể gọi API này cho nút "Thùng rác" (Delete button) để cung cấp trải nghiệm rõ ràng cho Admin.
