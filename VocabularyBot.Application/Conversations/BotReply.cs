namespace VocabularyBot.Application.Conversations;

public sealed record BotButton(string Text, string? CallbackData = null);

public sealed record BotKeyboard(IReadOnlyList<IReadOnlyList<BotButton>> Rows, bool Inline = false,
    string Placeholder = "یک گزینه انتخاب کنید");

public sealed record BotReply(string Text, BotKeyboard? Keyboard = null);

public static class BotMenu
{
    public const string Word = "✍️ ثبت واژه";
    public const string Words = "📚 واژه‌های من";
    public const string Classes = "🏫 کلاس‌های من";
    public const string Join = "➕ عضویت در کلاس";
    public const string Register = "👤 شماره دانشجویی";
    public const string Help = "❔ راهنما";
    public const string Course = "🏫 ساخت کلاس";
    public const string Admin = "🔑 افزودن ادمین";
    public const string Browse = "📋 مرور کلاس‌ها";
    public const string SkipFamily = "⏭ بدون خانواده واژه";
    public const string Back = "↩️ مرحله قبل";
    public const string Cancel = "✖️ لغو";
    public const string Skip = "⏭ بدون مترادف";
    public const string Done = "✅ ثبت واژه";
    public const string Home = "🏠 منوی اصلی";

    public static string Command(string input) => input switch
    {
        Word => "/word", Words => "/words", Classes => "/classes", Join => "/join",
        Register => "/register", Help => "/help", Course => "/course", Admin => "/admin", Browse => "/browse",
        Back => "/back", Cancel => "/cancel", Skip => "/skip", SkipFamily => "/skip", Done => "/done", Home => "/menu",
        _ => input
    };

    public static BotKeyboard Reply(params string[][] rows) =>
        new(rows.Select(row => (IReadOnlyList<BotButton>)row.Select(text => new BotButton(text)).ToArray()).ToArray());
}
