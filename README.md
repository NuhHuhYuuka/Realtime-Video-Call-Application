## NIHAHAHAHA, THIS IS JUST A TEST MARKDOWN
Hướng dẫn cho phần Security & Local Data
Thư viện: Cài đặt NuGet System.Data.SQLite và Stub.System.Data.SQLite.Core.NetFramework cho Project Client và Bot.

Sử dụng: Thêm using SecurityData.Services; ở đầu các file cần dùng hàm mã hóa/DB.

1. Cách sử dụng các hàm (Services)
Bảo mật (SecurityService):

Dùng SecurityService.Encrypt(plainText) để mã hóa tin nhắn trước khi gửi qua LAN hoặc lưu vào DB.

Dùng SecurityService.Decrypt(cipherText) để giải mã tin nhắn nhận được.

Lưu ý: Bản mới này sử dụng Salt và IV ngẫu nhiên cho mỗi tin nhắn để đảm bảo an toàn tối đa.

Dữ liệu (DatabaseService):

Khi cần lưu tin nhắn: Khởi tạo đối tượng ChatMessage rồi gọi dbService.SaveMessage(msg).

Để lấy lịch sử: Dùng dbService.GetHistory(peer) hoặc dbService.GetAllHistory().

Đã hỗ trợ đầy đủ các hàm Xóa (DeleteMessage/DeleteConversation) và Sửa (UpdateMessage) dựa trên ID.

Truyền file (FileTransferService):

Mình đã xử lý Chunking (chia nhỏ 64KB/gói) để gửi file lớn không bị nghẽn mạng.

Bên gửi dùng FileTransferService.SplitFile(path) để lấy các mảnh byte gửi đi.

Bên nhận dùng FileTransferService.BytesToFile(name, data) để lưu file vào thư mục Documents.

3. Các Model mới
Mọi người tham khảo các class ChatMessage, FileChunk và NetworkPacket trong folder Models để đồng bộ kiểu dữ liệu khi truyền nhận qua LAN nhé.
