## NIHAHAHAHA, THIS IS JUST A TEST MARKDOWN

## 🔐 Hướng dẫn tích hợp Bảo mật & Dữ liệu (Thành viên 2)

Mọi người sau khi **Pull** code về, vui lòng đọc kỹ hướng dẫn này để ráp logic vào UI và LAN:

### 1. Thư viện & Cấu hình (Fix lỗi DLL)
-Thư viện: Cài đặt NuGet System.Data.SQLite và Stub.System.Data.SQLite.Core.NetFramework cho Project Client và Bot.
- Database `ChatApp.db` được lưu tự động tại thư mục `ApplicationData` của máy để tránh lỗi phân quyền.

### 2. Cách dùng SecurityService (Mã hóa AES-256)
Sử dụng IV và Salt ngẫu nhiên cho từng tin nhắn để đảm bảo an toàn tối đa:
- **Mã hóa:** `string encrypted = SecurityService.Encrypt("Nội dung tin nhắn");`
- **Giải mã:** `string decrypted = SecurityService.Decrypt(encryptedFromServer);`

### 3. Cách dùng DatabaseService (Lưu lịch sử)
Mọi tin nhắn nên được lưu vào máy local để xem lại:
- **Lưu tin nhắn:** `dbService.SaveMessage(chatMessageObject);`
- **Lấy lịch sử:** `var history = dbService.GetHistory("Tên người nhận");`
- **Xóa/Sửa:** Đã có sẵn các hàm `DeleteMessage(id)` và `UpdateMessage(id, content)`.

### 4. Truyền File qua LAN (Chunking 64KB)
Để gửi ảnh/file dung lượng lớn mà không nghẽn mạng:
- **Bên gửi:** Dùng `FileTransferService.SplitFile(path)` để chia file thành các `FileChunk`.
- **Bên nhận:** Dùng `FileTransferService.BytesToFile(name, data)` sau khi đã nhận đủ các mảnh byte.
- **Kiểm tra:** Có thể dùng `ComputeSha256(path)` để so sánh mã băm, đảm bảo file không bị lỗi khi truyền.

---
