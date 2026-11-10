using System.Net.Http.Json;
using System.Text.Json;

public sealed class TelegramClient(string token)
{
    private readonly HttpClient http = new() { Timeout = TimeSpan.FromSeconds(45) };

    public async Task<JsonElement> Call(string method, object data, CancellationToken ct)
    {
        using var response = await http.PostAsJsonAsync($"https://api.telegram.org/bot{token}/{method}", data, ct);
        response.EnsureSuccessStatusCode();
        using var doc = JsonDocument.Parse(await response.Content.ReadAsStringAsync(ct));
        if (!doc.RootElement.GetProperty("ok").GetBoolean())
            throw new InvalidOperationException("Telegram API request failed.");
        return doc.RootElement.GetProperty("result").Clone();
    }

    public async Task Send(long id, string text, CancellationToken ct)
    {
        foreach (var chunk in text.Chunk(3500))
            await Call("sendMessage", new { chat_id = id, text = new string(chunk) }, ct);
    }
}