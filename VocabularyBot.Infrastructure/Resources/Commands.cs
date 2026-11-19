// Generated from Commands.resx. Keep properties in sync with resource keys.
using System.Resources;

namespace VocabularyBot.Infrastructure.Resources;

public static class Commands
{
    private static readonly ResourceManager Manager = new("VocabularyBot.Infrastructure.Resources.Commands", typeof(Commands).Assembly);

    private static string Get(string key) => Manager.GetString(key) ?? throw new MissingManifestResourceException(key);

    public static string Word => Get(nameof(Word));
    public static string Words => Get(nameof(Words));
    public static string Classes => Get(nameof(Classes));
    public static string Join => Get(nameof(Join));
    public static string Register => Get(nameof(Register));
    public static string Help => Get(nameof(Help));
    public static string Course => Get(nameof(Course));
    public static string Admin => Get(nameof(Admin));
    public static string Browse => Get(nameof(Browse));
    public static string SkipFamily => Get(nameof(SkipFamily));
    public static string Back => Get(nameof(Back));
    public static string Cancel => Get(nameof(Cancel));
    public static string Skip => Get(nameof(Skip));
    public static string Done => Get(nameof(Done));
    public static string Home => Get(nameof(Home));
}
