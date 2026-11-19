using System.Net.Http.Json;
using System.Text.Json;
using VocabularyBot.Application.Conversations;

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

    public async Task Send(long id, BotReply reply, CancellationToken ct)
    {
        var chunks = SplitMessage(reply.Text).ToArray();
        for (var index = 0; index < chunks.Length; index++)
        {
            var payload = new Dictionary<string, object> { ["chat_id"] = id, ["text"] = chunks[index] };
            if (index == chunks.Length - 1 && reply.Keyboard is { } keyboard)
                payload["reply_markup"] = Markup(keyboard);
            await Call("sendMessage", payload, ct);
        }
    }

    private static object Markup(BotKeyboard keyboard)
    {
        if (keyboard.Rows.Count == 0) return new { remove_keyboard = true };

        return keyboard.Inline
            ? new
            {
                inline_keyboard = keyboard.Rows.Select(row =>
                    row.Select(button => new { text = button.Text, callback_data = button.CallbackData }))
            }
            : new
            {
                keyboard = keyboard.Rows.Select(row => row.Select(button => new { text = button.Text })),
                resize_keyboard = true,
                is_persistent = true,
                input_field_placeholder = keyboard.Placeholder
            };
    }

    // Prefer paragraph boundaries and never split a UTF-16 surrogate pair (for example, an emoji).
    public static IEnumerable<string> SplitMessage(string text)
    {
        const int limit = 3500;
        while (text.Length > limit)
        {
            var end = text.LastIndexOf("\n\n", limit, StringComparison.Ordinal);
            if (end < limit / 2) end = text.LastIndexOf('\n', limit);
            if (end < limit / 2) end = limit;
            if (char.IsHighSurrogate(text[end - 1])) end--;
            yield return text[..end];
            text = text[end..];
        }

        if (text.Length > 0) yield return text;
    }
}