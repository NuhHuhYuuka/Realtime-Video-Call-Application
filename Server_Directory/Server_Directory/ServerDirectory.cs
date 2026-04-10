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

        // 2. Quy ước Giao thức (Protocol): "REGISTER|Username|ClientListeningPort"
        string[] protocolParts = incomingMessage.Split('|');
        if (protocolParts.Length == 3 && protocolParts[0] == "REGISTER")
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
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine($"[WARNING] Invalid protocol format received: {incomingMessage}");
            Console.ResetColor();
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] Client connection lost: {ex.Message}");
        Console.ResetColor();
    }
    finally
    {
        // 6. Đóng kết nối sau khi xử lý xong (Cơ chế Stateless giống HTTP)
        client.Close();
    }
}