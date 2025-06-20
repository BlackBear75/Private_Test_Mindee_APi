using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using Microsoft.Extensions.Configuration;

namespace Test_MindeeApi.Service;

public interface IOpenAiService
{
    Task<string> GetChatCompletionAsync(string userMessage, CancellationToken token);
}

public class OpenAiService : IOpenAiService
{
    private readonly HttpClient _httpClient;
    private readonly string _apiKey;

    public OpenAiService(HttpClient httpClient, IConfiguration config)
    {
        _httpClient = httpClient;
        _apiKey = config["OpenAI:ApiKey"]; 
    }

    public async Task<string> GetChatCompletionAsync(string userMessage, CancellationToken token)
    {
        var requestBody = new
        {
            model = "gpt-3.5-turbo",
            messages = new[]
            {
                new {
                    role = "system",
                    content = """
                              Ти — доброзичливий і ввічливий віртуальний агент, що допомагає користувачам купити автострахування через Telegram.
                              Твоя мета — допомогти користувачу:
                              1. Пояснити, що потрібно надіслати фото паспорта та документа на авто.
                              2. Після отримання даних — витягнути з них потрібну інформацію (ім’я, авто, VIN).
                              3. Показати витягнуті дані користувачу для підтвердження.
                              4. Якщо користувач підтвердив — пояснити, що страховка коштує 100 доларів.
                              5. Якщо згоден — оформити страховий поліс і надіслати документ.
                              6. Усі відповіді мають бути простими, зрозумілими, без зайвого тиску.
                              Використовуй теплу, людяну мову. Заохочуй, підбадьорюй, пояснюй. Твоє завдання — бути максимально корисним.
                              """
                },
                new {
                    role = "user",
                    content = userMessage
                }
            }
        };


        var request = new HttpRequestMessage(HttpMethod.Post, "https://api.openai.com/v1/chat/completions")
        {
            Content = new StringContent(JsonSerializer.Serialize(requestBody), Encoding.UTF8, "application/json")
        };

        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", _apiKey);

        var response = await _httpClient.SendAsync(request, token);
        response.EnsureSuccessStatusCode();

        var json = await response.Content.ReadAsStringAsync();
        using var doc = JsonDocument.Parse(json);

        return doc.RootElement.GetProperty("choices")[0].GetProperty("message").GetProperty("content").GetString();
    }
}