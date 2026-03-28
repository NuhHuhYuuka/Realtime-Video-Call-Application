using System;
using System.IO;
using System.Media;
using System.Net;
using System.Net.Http;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// Cấu hình bảng mã UTF-8 cho Console để hiển thị chính xác ngôn ngữ
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

Console.ForegroundColor = ConsoleColor.Cyan;
Console.WriteLine("=== UITI-CHAN AI BOT INITIALIZATION ===");
Console.ResetColor();

// Cấu hình cổng mạng P2P và Endpoint của Local LLM
int p2pPort = 5555;
string ollamaEndpoint = "http://localhost:11434/api/generate";

// Khởi tạo Task chạy ngầm để lắng nghe các kết nối TCP (P2P Listener)
_ = Task.Run(() => StartP2PListener(p2pPort, ollamaEndpoint));

// Tạm dừng luồng chính để đảm bảo P2P Listener khởi động hoàn tất trước khi hiển thị UI
await Task.Delay(500);

// Vòng lặp chính: Xử lý tương tác của người dùng trên Local Console
while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("\nSenpai: ");
    Console.ResetColor();

    string userPrompt = Console.ReadLine() ?? string.Empty;

    // Xử lý lệnh kết thúc phiên làm việc
    if (userPrompt.Trim().ToLower() == "exit" || userPrompt.Trim().ToLower() == "quit")
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("Uiti đi ngủ đây, tạm biệt Senpai.");
        Console.ResetColor();
        break;
    }

    // Bỏ qua nếu dữ liệu đầu vào rỗng
    if (string.IsNullOrWhiteSpace(userPrompt))
    {
        continue;
    }

    // Gửi yêu cầu đến Local LLM và nhận về chuỗi phản hồi song ngữ
    string aiRawResponse = await AskOllamaAsync(userPrompt, ollamaEndpoint);

    string vnText = aiRawResponse;
    string jpText = "";

    // Phân tích cú pháp chuỗi phản hồi (Format: [Vietnamese] | [Japanese])
    if (aiRawResponse.Contains("|"))
    {
        string[] parts = aiRawResponse.Split('|');
        vnText = parts[0].Trim();

        if (parts.Length > 1)
        {
            jpText = parts[1].Trim();
        }
    }
    else
    {
        jpText = aiRawResponse;
    }

    // Hiển thị phần văn bản Tiếng Việt lên giao diện người dùng
    Console.WriteLine();
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.Write("Uiti-chan: ");
    Console.ResetColor();
    Console.WriteLine(vnText);

    // Xử lý tổng hợp giọng nói (Text-to-Speech) thông qua VoiceVox API
    try
    {
        if (!string.IsNullOrWhiteSpace(jpText))
        {
            byte[] audioData = await GetVoiceVoxAudioAsync(jpText);
            PlayAudio(audioData);
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[VOICEVOX ERROR] System audio failure: {ex.Message}");
        Console.ResetColor();
    }
}

// --- CÁC PHƯƠNG THỨC XỬ LÝ ĐỘC LẬP (METHODS) ---

// Khởi tạo máy chủ TCP để chấp nhận các kết nối P2P đến
static async Task StartP2PListener(int port, string endpoint)
{
    TcpListener listener = new TcpListener(IPAddress.Any, port);
    listener.Start();

    while (true)
    {
        TcpClient peerClient = await listener.AcceptTcpClientAsync();
        // Cấp phát luồng xử lý riêng biệt cho mỗi kết nối Client
        _ = Task.Run(() => HandlePeerConnectionAsync(peerClient, endpoint));
    }
}

// Xử lý luồng dữ liệu của mạng P2P
static async Task HandlePeerConnectionAsync(TcpClient client, string endpoint)
{
    try
    {
        string peerIP = ((IPEndPoint)client.Client.RemoteEndPoint).Address.ToString();

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"\n[+] Incoming P2P message from {peerIP}");
        Console.ResetColor();

        NetworkStream stream = client.GetStream();
        byte[] buffer = new byte[4096];
        int bytesRead = await stream.ReadAsync(buffer, 0, buffer.Length);

        string incomingMessage = Encoding.UTF8.GetString(buffer, 0, bytesRead);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"[P2P IN] {peerIP} says: {incomingMessage}");
        Console.ResetColor();

        // Tích hợp luồng P2P với AI: Chuyển tiếp tin nhắn mạng đến LLM
        string aiRawResponse = await AskOllamaAsync(incomingMessage, endpoint);

        // [FIXED] Phân tích cú pháp chuỗi phản hồi cho luồng P2P
        string vnText = aiRawResponse;
        string jpText = "";

        if (aiRawResponse.Contains("|"))
        {
            string[] parts = aiRawResponse.Split('|');
            vnText = parts[0].Trim();
            if (parts.Length > 1)
            {
                jpText = parts[1].Trim();
            }
        }
        else
        {
            jpText = aiRawResponse;
        }

        // Phản hồi KẾT QUẢ TIẾNG VIỆT về phía Client Test
        byte[] replyBytes = Encoding.UTF8.GetBytes(vnText);
        await stream.WriteAsync(replyBytes, 0, replyBytes.Length);

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"[P2P OUT] Replied to {peerIP} successfully.");
        Console.ResetColor();

        // [FIXED] Kích hoạt VoiceVox bằng Tiếng Nhật ngay trên máy Local
        if (!string.IsNullOrWhiteSpace(jpText))
        {
            byte[] audioData = await GetVoiceVoxAudioAsync(jpText);
            PlayAudio(audioData);
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[ERROR] P2P Connection error: {ex.Message}");
        Console.ResetColor();
    }
    finally
    {
        client.Close();
    }
}

// Giao tiếp với API của Local LLM (Ollama)
static async Task<string> AskOllamaAsync(string prompt, string endpoint)
{
    using HttpClient httpClient = new HttpClient();

    // Thiết lập System Prompt định hướng phản hồi song ngữ cho ứng dụng AI Bot
    var requestPayload = new
    {
        model = "qwen2.5:7b", // Cập nhật model theo yêu cầu
        prompt = prompt,
        system = "Bạn là Uiti-chan, một nữ trợ lý ảo anime dễ thương. QUY TẮC BẮT BUỘC: Bạn PHẢI trả lời theo đúng định dạng CỐ ĐỊNH có chứa dấu gạch đứng '|' như sau: '<Câu tiếng Việt> | <Câu dịch sang Tiếng Nhật thuần túy>'. VÍ DỤ: 'Chào Senpai, em ở đây! | せんぱい、ここにいます！'. LƯU Ý: Phần tiếng Nhật CẤM TUYỆT ĐỐI dùng chữ Latinh/Romaji (A-Z). KHÔNG sinh ra code. Trả lời cực kỳ ngắn gọn 1 câu duy nhất.",
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
    catch (Exception ex)
    {
        return $"[LỖI] {ex.Message} | エラーが発生しました";
    }
}

// Chuyển đổi văn bản thành âm thanh thông qua API VoiceVox (Local Text-to-Speech)
static async Task<byte[]> GetVoiceVoxAudioAsync(string text, int speaker = 2)
{
    using HttpClient httpClient = new HttpClient();
    string encodedText = Uri.EscapeDataString(text);

    // Giai đoạn 1: Phân tích ngữ điệu âm thanh (Audio Query)
    string queryUrl = $"http://127.0.0.1:50021/audio_query?text={encodedText}&speaker={speaker}";
    HttpResponseMessage queryResponse = await httpClient.PostAsync(queryUrl, null);
    queryResponse.EnsureSuccessStatusCode();
    string queryJson = await queryResponse.Content.ReadAsStringAsync();

    // Giai đoạn 2: Tổng hợp dữ liệu âm thanh (Synthesis)
    string synthUrl = $"http://127.0.0.1:50021/synthesis?speaker={speaker}";
    var synthContent = new StringContent(queryJson, Encoding.UTF8, "application/json");
    HttpResponseMessage synthResponse = await httpClient.PostAsync(synthUrl, synthContent);
    synthResponse.EnsureSuccessStatusCode();

    return await synthResponse.Content.ReadAsByteArrayAsync();
}

// Phát dữ liệu âm thanh dưới dạng mảng byte trên hệ thống Windows
#pragma warning disable CA1416 
static void PlayAudio(byte[] audioData)
{
    using MemoryStream ms = new MemoryStream(audioData);
    using SoundPlayer player = new SoundPlayer(ms);
    player.Play();
}
#pragma warning restore CA1416