using System;
using System.Collections.Concurrent;
using System.IO;
using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Threading;

// Yêu cầu thiết lập Port để khởi tạo Server Instance (Khuyến nghị: 8888 hoặc 8889)
Console.Write("Enter Port to run this Server Instance (e.g., 8888 or 8889): ");
int port = int.Parse(Console.ReadLine() ?? "8888");

Console.WriteLine($"=== DIRECTORY SERVER IS RUNNING ON PORT {port} ===");

// Cấu trúc dữ liệu lưu trữ danh bạ: [Username] -> [IP:Port]
// Sử dụng ConcurrentDictionary để đảm bảo an toàn luồng (Thread-safe) trong môi trường đa luồng
ConcurrentDictionary<string, string> activeDirectory = new ConcurrentDictionary<string, string>();

// Khởi tạo và lắng nghe các kết nối TCP đến Server
TcpListener listener = new TcpListener(IPAddress.Any, port);
listener.Start();

Console.WriteLine("[INFO] Waiting for incoming Client connections...");

while (true)
{
    // Chấp nhận yêu cầu kết nối từ Client mới
    TcpClient client = listener.AcceptTcpClient();
    string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

    Console.WriteLine($"\n[+] Client connected from IP: {clientIP}");

    // Cấp phát một luồng (Thread) độc lập để xử lý Client, tránh nghẽn luồng chính của Server
    Thread clientThread = new Thread(() => HandleClient(client, activeDirectory));
    clientThread.Start();
}

// --- Phương thức xử lý luồng độc lập cho từng Client ---
static void HandleClient(TcpClient client, ConcurrentDictionary<string, string> directory)
{
    try
    {
        NetworkStream stream = client.GetStream();
        using StreamReader reader = new StreamReader(stream, Encoding.UTF8);
        using StreamWriter writer = new StreamWriter(stream, Encoding.UTF8) { AutoFlush = true };

        // 1. Đọc tin nhắn đầu tiên từ Client gửi lên
        string incomingMessage = reader.ReadLine();
        if (string.IsNullOrEmpty(incomingMessage)) return;

        // 2. Quy ước Giao thức (Protocol)
        string[] protocolParts = incomingMessage.Split('|');
        string command = protocolParts[0];

        if (command == "REGISTER" && protocolParts.Length == 3)
        {
            string username = protocolParts[1];
            string clientListeningPort = protocolParts[2];
            string clientIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

            // Lắp ráp địa chỉ hoàn chỉnh của Client (IP:Port)
            string fullAddress = $"{clientIP}:{clientListeningPort}";

            // 3. Thêm mới hoặc Cập nhật thông tin người dùng vào danh bạ an toàn (Thread-safe)
            directory.AddOrUpdate(username, fullAddress, (key, oldValue) => fullAddress);

            Console.ForegroundColor = ConsoleColor.Green;
            Console.WriteLine($"[REGISTER] User '{username}' is online at {fullAddress}");
            Console.ResetColor();

            // 4. Trích xuất danh sách tất cả người dùng đang Online (trừ bản thân)
            var onlineUsers = directory.Keys;
            string userListStr = string.Join(",", onlineUsers);

            // 5. Phản hồi trạng thái thành công kèm theo danh sách bạn bè
            writer.WriteLine($"SUCCESS|{userListStr}");

            Console.WriteLine($"[INFO] Sent active user list to '{username}'");
        }
        else if (command == "LOGOUT" && protocolParts.Length == 2)
        {
            string username = protocolParts[1];

            // Xóa người dùng khỏi danh bạ khi họ thoát
            if (directory.TryRemove(username, out _))
            {
                Console.ForegroundColor = ConsoleColor.DarkYellow;
                Console.WriteLine($"[LOGOUT] User '{username}' has left the network. Directory cleaned.");
                Console.ResetColor();

                writer.WriteLine("LOGOUT_SUCCESS");
            }
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARNING] Invalid protocol format received: {incomingMessage}");
            Console.ResetColor();
        }
    }
    catch (IOException ioEx) when (ioEx.InnerException is SocketException socketEx &&
                                  (socketEx.SocketErrorCode == SocketError.ConnectionAborted ||
                                   socketEx.SocketErrorCode == SocketError.ConnectionReset))
    {
        // Bỏ qua lỗi và không in ra màn hình nếu Client tự ngắt kết nối hoặc rớt mạng
        // Giúp màn hình Console của Server luôn sạch sẽ
    }
    catch (Exception ex)
    {
        // Chỉ in ra màn hình những lỗi hệ thống nghiêm trọng thực sự
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] Unexpected server error: {ex.Message}");
        Console.ResetColor();
    }
    finally
    {
        // 6. Đóng kết nối sau khi xử lý xong (Cơ chế Stateless giống HTTP)
        client.Close();
    }
}