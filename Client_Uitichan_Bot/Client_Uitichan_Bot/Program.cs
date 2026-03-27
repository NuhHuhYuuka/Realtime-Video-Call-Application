using System;
using System.Net.Http;
using System.Text;
using System.Text.Json;
using System.Threading.Tasks;

// Cấu hình bảng mã UTF-8 cho Console để hiển thị chính xác tiếng Việt
Console.OutputEncoding = Encoding.UTF8;
Console.InputEncoding = Encoding.UTF8;

Console.WriteLine("=== UITI-CHAN AI BOT NITIALIZATION ===");
Console.WriteLine("[INFO] Type 'exit' or 'quit' to close the session.\n");

string ollamaEndpoint = "http://localhost:11434/api/generate";
using HttpClient httpClient = new HttpClient();

// Triển khai vòng lặp vô hạn để duy trì phiên giao tiếp liên tục với AI Bot
while (true)
{
    Console.Write("\nUser: ");
    string userPrompt = Console.ReadLine() ?? string.Empty;

    // Xử lý lệnh thoát chương trình từ phía người dùng
    if (userPrompt.Trim().ToLower() == "exit" || userPrompt.Trim().ToLower() == "quit")
    {
        Console.WriteLine("[INFO] Terminating Uiti-chan session. Goodbye!");
        break;
    }

    // Bỏ qua quy trình gọi API nếu người dùng nhập chuỗi rỗng
    if (string.IsNullOrWhiteSpace(userPrompt))
    {
        continue;
    }

    // Cấu hình payload với System Prompt thiết lập nhân cách và rào cản lỗi cho VoiceVox TTS
    var requestPayload = new
    {
        model = "qwen3:4b",
        prompt = userPrompt,
        system = "Bạn là Uiti-chan, một AI trợ lý ảo vô cùng dễ thương. Bạn luôn gọi người dùng là 'Senpai' và xưng hô là 'em'. Tuyệt đối tuân thủ các quy tắc sau: 1. LUÔN LUÔN giao tiếp bằng Tiếng Việt chuẩn. TUYỆT ĐỐI KHÔNG sử dụng tiếng Trung Quốc. 2. Các câu thoại phải ngắn gọn, ngắt nghỉ rõ ràng bằng dấu phẩy và chấm để công cụ VoiceVox đọc tự nhiên, không bị hụt hơi. 3. Nếu phải viết code, tuyệt đối không chèn code vào trong các thẻ đánh dấu ngôn ngữ nói để tránh việc VoiceVox đọc nhầm code thành tiếng. Tách biệt hoàn toàn phần giao tiếp và phần code.",
        stream = false
    };

    string jsonContent = JsonSerializer.Serialize(requestPayload);
    var httpContent = new StringContent(jsonContent, Encoding.UTF8, "application/json");

    Console.WriteLine("[INFO] Processing request via Local LLM...");

    try
    {
        // Thực thi HTTP POST request đến Ollama
        HttpResponseMessage httpResponse = await httpClient.PostAsync(ollamaEndpoint, httpContent);
        httpResponse.EnsureSuccessStatusCode();

        string responseBody = await httpResponse.Content.ReadAsStringAsync();

        // Phân tích cú pháp JSON để trích xuất phản hồi
        using JsonDocument jsonDoc = JsonDocument.Parse(responseBody);
        string generatedText = jsonDoc.RootElement.GetProperty("response").GetString() ?? string.Empty;

        Console.WriteLine($"\n[UITI-CHAN RESPONSE]: {generatedText}");
    }
    catch (HttpRequestException httpEx)
    {
        // Xử lý ngoại lệ kết nối cục bộ
        Console.WriteLine($"\n[ERROR] Failed to connect to Ollama service: {httpEx.Message}");
        Console.WriteLine("[ACTION REQUIRED] Ensure Ollama is running locally with the command: 'ollama run qwen3:4b'");
        break; // Thoát vòng lặp để tránh treo hệ thống khi mất kết nối API
    }
    catch (Exception ex)
    {
        // Bắt các ngoại lệ không xác định khác
        Console.WriteLine($"\n[ERROR] An unexpected error occurred: {ex.Message}");
    }
}