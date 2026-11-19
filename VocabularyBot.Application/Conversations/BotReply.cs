namespace VocabularyBot.Application.Conversations;

public sealed record BotButton(string Text, string? CallbackData = null);

public sealed record BotKeyboard(
    IReadOnlyList<IReadOnlyList<BotButton>> Rows,
    bool Inline = false,
    string? Placeholder = null);

public sealed record BotReply(string Text, BotKeyboard? Keyboard = null);

public static class BotMenu
{
    public static string Word => global::VocabularyBot.Infrastructure.Resources.Commands.Word;
    public static string Words => global::VocabularyBot.Infrastructure.Resources.Commands.Words;
    public static string Classes => global::VocabularyBot.Infrastructure.Resources.Commands.Classes;
    public static string Join => global::VocabularyBot.Infrastructure.Resources.Commands.Join;
    public static string Register => global::VocabularyBot.Infrastructure.Resources.Commands.Register;
    public static string Help => global::VocabularyBot.Infrastructure.Resources.Commands.Help;
    public static string Course => global::VocabularyBot.Infrastructure.Resources.Commands.Course;
    public static string Admin => global::VocabularyBot.Infrastructure.Resources.Commands.Admin;
    public static string Browse => global::VocabularyBot.Infrastructure.Resources.Commands.Browse;
    public static string SkipFamily => global::VocabularyBot.Infrastructure.Resources.Commands.SkipFamily;
    public static string Back => global::VocabularyBot.Infrastructure.Resources.Commands.Back;
    public static string Cancel => global::VocabularyBot.Infrastructure.Resources.Commands.Cancel;
    public static string Skip => global::VocabularyBot.Infrastructure.Resources.Commands.Skip;
    public static string Done => global::VocabularyBot.Infrastructure.Resources.Commands.Done;
    public static string Home => global::VocabularyBot.Infrastructure.Resources.Commands.Home;

    public static string Command(string input)
    {
        var commands = new (string Label, string Value)[]
        {
            (Word, "/word"), (Words, "/words"), (Classes, "/classes"), (Join, "/join"),
            (Register, "/register"), (Help, "/help"), (Course, "/course"), (Admin, "/admin"), (Browse, "/browse"),
            (Back, "/back"), (Cancel, "/cancel"), (Skip, "/skip"), (SkipFamily, "/skip"), (Done, "/done"), (Home, "/menu")
        };
        return commands.FirstOrDefault(command => command.Label == input).Value ?? input;
    }

    public static BotKeyboard Reply(params string[][] rows)
    {
        return new BotKeyboard(rows
            .Select(row => (IReadOnlyList<BotButton>)row.Select(text => new BotButton(text)).ToArray())
            .ToArray(), Placeholder: global::VocabularyBot.Infrastructure.Resources.Messages.KeyboardPlaceholder);
    }
}