using System;
using System.Diagnostics;
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
Console.WriteLine("=== UITI-CHAN AI BOT INITIALIZATION (CLOUD EDITION) ===");
Console.ResetColor();

// Kích hoạt VoiceVox Engine chạy ngầm
StartVoiceVoxEngine();

// Cấu hình cổng mạng P2P
int p2pPort = 5555;

// Khởi tạo Task chạy ngầm để lắng nghe các kết nối TCP
_ = Task.Run(() => StartP2PListener(p2pPort));

// Tạm dừng luồng chính để đảm bảo P2P Listener & VoiceVox khởi động hoàn tất
await Task.Delay(1000);

// Vòng lặp chính: Xử lý tương tác của người dùng trên Local Console
while (true)
{
    Console.ForegroundColor = ConsoleColor.Green;
    Console.Write("\nOnii-chan: ");
    Console.ResetColor();

    string userPrompt = Console.ReadLine() ?? string.Empty;

    // Xử lý lệnh kết thúc phiên làm việc
    if (userPrompt.Trim().ToLower() == "exit" || userPrompt.Trim().ToLower() == "quit")
    {
        Console.ForegroundColor = ConsoleColor.Magenta;
        Console.WriteLine("Hứ, Uiti đi ngủ đây, đồ ngốc Onii-chan!");
        Console.ResetColor();
        break;
    }

    // Bỏ qua nếu dữ liệu đầu vào rỗng
    if (string.IsNullOrWhiteSpace(userPrompt))
    {
        continue;
    }

    // Xuống một dòng cho khung chat thoáng đãng
    Console.WriteLine();

    // Hiển thị trạng thái AI đang xử lý
    Console.ForegroundColor = ConsoleColor.DarkGray;
    Console.Write("Uiti-chan đang suy nghĩ (¬_¬ )...");
    Console.ResetColor();

    // Gửi yêu cầu đến OpenRouter API và nhận về chuỗi phản hồi
    string aiRawResponse = await AskOpenRouterAsync(userPrompt);

    // Xóa dòng "đang suy nghĩ" đi để giao diện gọn gàng
    Console.SetCursorPosition(0, Console.CursorTop);
    Console.Write(new string(' ', 80));
    Console.SetCursorPosition(0, Console.CursorTop);

    string vnText = aiRawResponse;
    string jpText = "";

    // Phân tích cú pháp chuỗi phản hồi
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

    // Hiển thị phần văn bản Tiếng Việt
    Console.ForegroundColor = ConsoleColor.Magenta;
    Console.Write("Uiti-chan: ");
    Console.ResetColor();
    Console.WriteLine(vnText);

    // Xử lý tổng hợp giọng nói thông qua VoiceVox API (Chỉ phát ở Local Console)
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

static void StartVoiceVoxEngine()
{
    try
    {
        string voiceVoxPath = @"Your VoiceVox Path Here";

        if (File.Exists(voiceVoxPath))
        {
            ProcessStartInfo startInfo = new ProcessStartInfo
            {
                FileName = voiceVoxPath,
                WindowStyle = ProcessWindowStyle.Hidden,
                CreateNoWindow = true
            };
            Process.Start(startInfo);
        }
        else
        {
            Console.ForegroundColor = ConsoleColor.Yellow;
            Console.WriteLine("[CẢNH BÁO] Không tìm thấy file chạy VoiceVox.");
            Console.ResetColor();
        }
    }
    catch (Exception ex)
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine($"[CẢNH BÁO] Không thể tự động bật VoiceVox: {ex.Message}");
        Console.ResetColor();
    }
}

static async Task StartP2PListener(int port)
{
    TcpListener listener = new TcpListener(IPAddress.Any, port);
    listener.Start();

    while (true)
    {
        TcpClient peerClient = await listener.AcceptTcpClientAsync();
        _ = Task.Run(() => HandlePeerConnectionAsync(peerClient));
    }
}

static async Task HandlePeerConnectionAsync(TcpClient client)
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

        // Gửi thông điệp qua AI
        string aiRawResponse = await AskOpenRouterAsync(incomingMessage);

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

        // Tạo mảng byte âm thanh
        byte[] audioData = Array.Empty<byte>();
        if (!string.IsNullOrWhiteSpace(jpText))
        {
            audioData = await GetVoiceVoxAudioAsync(jpText);
        }

        // Đóng gói dữ liệu thành chuẩn chung qua BinaryWriter
        using BinaryWriter writer = new BinaryWriter(stream, Encoding.UTF8, leaveOpen: true);

        // Gửi đoạn văn bản Tiếng Việt
        writer.Write(vnText);

        // Gửi kích thước file âm thanh
        writer.Write(audioData.Length);

        // Gửi nội dung file âm thanh nếu có
        if (audioData.Length > 0)
        {
            writer.Write(audioData);
        }

        Console.ForegroundColor = ConsoleColor.DarkGray;
        Console.WriteLine($"[P2P OUT] Đã đóng gói và gửi thành công tới {peerIP}.");
        Console.ResetColor();
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

static async Task<string> AskOpenRouterAsync(string prompt)
{
    using HttpClient httpClient = new HttpClient();

    string apiKey = Environment.GetEnvironmentVariable("OPENROUTER_API_KEY");

    if (string.IsNullOrEmpty(apiKey))
    {
        Console.ForegroundColor = ConsoleColor.Red;
        Console.WriteLine("\n[LỖI BẢO MẬT] Không tìm thấy OPENROUTER_API_KEY!");
        Console.ResetColor();
        return "Baka Onii-chan! Anh chưa cài API Key kìa! | ばかお兄ちゃん！APIキーを設定してないじゃない！";
    }

    httpClient.DefaultRequestHeaders.Add("Authorization", $"Bearer {apiKey}");
    httpClient.DefaultRequestHeaders.Add("HTTP-Referer", "http://localhost");

    string openRouterEndpoint = "https://openrouter.ai/api/v1/chat/completions";

    var requestPayload = new
    {
        model = "arcee-ai/trinity-large-preview:free",
        messages = new[]
        {
            new { role = "system", content = @"Bạn là Uiti-chan, nữ trợ lý ảo anime Tsundere. 
QUY TẮC SỐNG CÒN BẮT BUỘC TUÂN THỦ:
1. XƯNG HÔ: LUÔN LUÔN xưng là 'em' và gọi người dùng là 'Onii-chan'. CẤM TUYỆT ĐỐI việc xưng 'tôi', 'mình', hay 'ta' trong mọi tình huống.
2. TÍNH CÁCH: Thể hiện sự quan tâm một cách ngại ngùng. CẤM lặp lại từ 'baka' hoặc 'đồ ngốc' liên tục. Chỉ dùng đúng 1 lần nếu bị trêu chọc quá mức. Hãy giao tiếp linh hoạt như con người.
3. ĐỊNH DẠNG BẮT BUỘC: '<Câu tiếng Việt> | <Câu dịch sang Tiếng Nhật thuần túy>'.
4. NGÔN NGỮ: CẤM dùng Romaji/Latinh (A-Z) ở phần tiếng Nhật. Trả lời cực kỳ ngắn gọn 1 câu duy nhất." },
            new { role = "user", content = prompt }
        },
        stream = false
    };

    string jsonContent = JsonSerializer.Serialize(requestPayload);
    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

    try
    {
        HttpResponseMessage httpResponse = await httpClient.PostAsync(openRouterEndpoint, httpContent);
        httpResponse.EnsureSuccessStatusCode();

        string responseBody = await httpResponse.Content.ReadAsStringAsync();
        using JsonDocument jsonDoc = JsonDocument.Parse(responseBody);

        return jsonDoc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString() ?? string.Empty;
    }
    catch (Exception ex)
    {
        return $"[LỖI CLOUD] {ex.Message} | クラウドエラーが発生しました";
    }
}

static async Task<byte[]> GetVoiceVoxAudioAsync(string text, int speaker = 14)
{
    using HttpClient httpClient = new HttpClient();
    string encodedText = Uri.EscapeDataString(text);

    string queryUrl = $"http://127.0.0.1:50021/audio_query?text={encodedText}&speaker={speaker}";
    HttpResponseMessage queryResponse = await httpClient.PostAsync(queryUrl, null);
    queryResponse.EnsureSuccessStatusCode();
    string queryJson = await queryResponse.Content.ReadAsStringAsync();

    string synthUrl = $"http://127.0.0.1:50021/synthesis?speaker={speaker}";
    var synthContent = new StringContent(queryJson, Encoding.UTF8, "application/json");
    HttpResponseMessage synthResponse = await httpClient.PostAsync(synthUrl, synthContent);
    synthResponse.EnsureSuccessStatusCode();

    return await synthResponse.Content.ReadAsByteArrayAsync();
}

#pragma warning disable CA1416 
static void PlayAudio(byte[] audioData)
{
    using MemoryStream ms = new MemoryStream(audioData);
    using SoundPlayer player = new SoundPlayer(ms);
    player.Play();
}
#pragma warning restore CA1416