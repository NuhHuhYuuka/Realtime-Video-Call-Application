using System;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// Cấu hình bảng mã UTF-8 cho Console để hiển thị chính xác tiếng Việt
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

Console.WriteLine("=== UITI-CHAN AI BOT INITIALIZATION ===");
// Console.WriteLine("[INFO] Type 'exit' or 'quit' to close the session.\n");

int p2pPort = 5555;
string ollamaEndpoint = "http://localhost:11434/api/generate";

// Khởi chạy luồng lắng nghe TCP ngầm (Background Task) để đón tin nhắn từ mạng P2P
_ = Task.Run(() => StartP2PListener(p2pPort, ollamaEndpoint));

// Tạm dừng luồng chính 500 milliseconds (0.5s).
// Mục đích: Nhường quyền cho luồng P2P (Background Task) kịp in thông báo khởi động lên Console,
// trước khi luồng chính (Main Thread) tiến hành in ra dòng "Senpai: ".
await Task.Delay(500);

// Triển khai vòng lặp vô hạn để duy trì phiên giao tiếp liên tục với AI Bot trên màn hình Local
while (true)
{
    Console.Write("\nSenpai: ");
    string userPrompt = Console.ReadLine() ?? string.Empty;

    // Xử lý lệnh thoát chương trình từ phía người dùng
    if (userPrompt.Trim().ToLower() == "exit" || userPrompt.Trim().ToLower() == "quit")
    {
        Console.WriteLine("Uiti đi ngủ đây, tạm biệt Senpai.");
        break;
    }

    // Bỏ qua quy trình gọi API nếu người dùng nhập chuỗi rỗng
    if (string.IsNullOrWhiteSpace(userPrompt))
    {
        continue;
    }

    // Console.WriteLine("[INFO] Processing request via Local LLM...");

    // Gọi hàm giao tiếp với Ollama
    string aiResponse = await AskOllamaAsync(userPrompt, ollamaEndpoint);
    Console.WriteLine($"\nUiti-chan: {aiResponse}");
}

// --- CÁC PHƯƠNG THỨC XỬ LÝ ĐỘC LẬP (METHODS) ---

// Phương thức thiết lập máy chủ TCP lắng nghe các kết nối P2P
static async Task StartP2PListener(int port, string endpoint)
{
    TcpListener listener = new TcpListener(IPAddress.Any, port);
    listener.Start();
    // Console.WriteLine($"[INFO] P2P Listener started. Uiti-chan is listening on Port {port}...");

    while (true)
    {
        // Chấp nhận kết nối từ một Client bạn bè trong mạng
        TcpClient peerClient = await listener.AcceptTcpClientAsync();

        // Đẩy luồng xử lý Client sang một Task mới để không làm nghẽn quá trình lắng nghe
        _ = Task.Run(() => HandlePeerConnectionAsync(peerClient, endpoint));
    }
}

// Phương thức xử lý luồng tin nhắn TCP đến từ các Peer Client
static async Task HandlePeerConnectionAsync(TcpClient client, string endpoint)
{
    try
    {
        string peerIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();
        Console.WriteLine($"\n[+] Incoming P2P message from {peerIP}");

        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[4096];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        // Giải mã tin nhắn nhận được
        string incomingMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);
        Console.WriteLine($"[P2P IN] {peerIP} says: {incomingMessage}");

        // Chuyển tiếp tin nhắn cho LLM xử lý
        string aiReply = await AskOllamaAsync(incomingMessage, endpoint);

        // Đóng gói và phản hồi kết quả về cho Peer Client
        byte[] replyBytes = Encoding.UTF8.GetBytes(aiReply);
        await stream.WriteAsync(replyBytes, 0, replyBytes.Length);

        Console.WriteLine($"[P2P OUT] Replied to {peerIP} successfully.");
    }
    catch (Exception ex)
    {
        Console.WriteLine($"[ERROR] P2P Connection error: {ex.Message}");
    }
    finally
    {
        client.Close();
    }
}

// Phương thức giao tiếp HTTP cốt lõi với Local LLM (Ollama)
static async Task<string> AskOllamaAsync(string prompt, string endpoint)
{
    using HttpClient httpClient = new HttpClient();

    // Cấu hình payload với System Prompt thiết lập nhân cách và rào cản lỗi cho VoiceVox TTS
    var requestPayload = new
    {
        model = "qwen3:4b",
        prompt = prompt,
        system = "Bạn là Uiti-chan, một AI trợ lý ảo vô cùng dễ thương. Bạn luôn gọi người dùng là 'Senpai' và xưng hô là 'em'. Tuyệt đối tuân thủ các quy tắc sau: 1. LUÔN LUÔN giao tiếp bằng Tiếng Việt chuẩn. TUYỆT ĐỐI KHÔNG sử dụng tiếng Trung Quốc. 2. Các câu thoại phải ngắn gọn, ngắt nghỉ rõ ràng bằng dấu phẩy và chấm để công cụ VoiceVox đọc tự nhiên, không bị hụt hơi. 3. Nếu phải viết code, tuyệt đối không chèn code vào trong các thẻ đánh dấu ngôn ngữ nói để tránh việc VoiceVox đọc nhầm code thành tiếng. Tách biệt hoàn toàn phần giao tiếp và phần code.",
        stream = false
    };

    string jsonContent = JsonSerializer.Serialize(requestPayload);
    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

    try
    {
        HttpResponseMessage httpResponse = await httpClient.PostAsync(endpoint, httpContent);
        httpResponse.EnsureSuccessStatusCode();

        string responseBody = await httpResponse.Content.ReadAsStringAsync();
        using JsonDocument jsonDoc = JsonDocument.Parse(responseBody);
        return jsonDoc.RootElement.GetProperty("response").GetString() ?? string.Empty;
    }
    catch (HttpRequestException httpEx)
    {
        Console.WriteLine($"\n[ERROR] Failed to connect to Ollama service: {httpEx.Message}");
        Console.WriteLine("[ACTION REQUIRED] Ensure Ollama is running locally with the command: 'ollama run qwen3:4b'");
        return "[CONNECTION ERROR] ERROR IN CALLING OLLAMA MODEL";
    }
    catch (Exception ex)
    {
        Console.WriteLine($"\n[ERROR] An unexpected error occurred: {ex.Message}");
        return "[FATAL SYSTEM FAILURE] ERRORS IN OLLAMA MODEL";
    }
}