using System;
using System.Collections.Concurrent;
using System.Net;
using System.Net.Sockets;
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
    // TODO: Triển khai logic xử lý gói tin (Ghi danh, Cập nhật IP, Truy xuất danh bạ) tại đây
    Console.WriteLine("[DEBUG] Thread started successfully for the new client.");
}