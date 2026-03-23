using System.Net.Http.Json;
using System.Text.Json;
using System.Net.Http;

public class GroqClient
{
    private readonly HttpClient _http = new();
    private readonly string _apiKey;

    public GroqClient(string apiKey)
    {
        _apiKey = apiKey;
        _http.DefaultRequestHeaders.Add("Authorization", $"Bearer {_apiKey}");
    }

    public async Task<string> ReviewAsync(string prompt)
    {
        var url = "https://api.groq.com/openai/v1/chat/completions";

        var body = new
        {
            model = "llama-3.3-70b-versatile",
            messages = new[]
            {
                new { role = "user", content = prompt }
            },
            temperature = 0.2
        };

        var response = await _http.PostAsJsonAsync(url, body);
        var json = await response.Content.ReadAsStringAsync();

        if (!response.IsSuccessStatusCode)
            return $"[Ошибка API] {response.StatusCode}: {json}";

        try
        {
            using var doc = JsonDocument.Parse(json);
            return doc.RootElement
                .GetProperty("choices")[0]
                .GetProperty("message")
                .GetProperty("content")
                .GetString() ?? "Нет ответа";
        }
        catch
        {
            return $"Не удалось разобрать ответ: {json}";
        }
    }
}