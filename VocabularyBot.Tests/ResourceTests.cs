using System.Collections;
using System.Globalization;
using System.Reflection;
using System.Resources;
using VocabularyBot.Infrastructure.Resources;

namespace VocabularyBot.Tests;

public sealed class ResourceTests
{
    [Theory]
    [InlineData(typeof(Error), "Errors")]
    [InlineData(typeof(Messages), "Messages")]
    [InlineData(typeof(Commands), "Commands")]
    public void Resource_keys_match_typed_properties(Type accessor, string resourceName)
    {
        var manager = new ResourceManager($"VocabularyBot.Infrastructure.Resources.{resourceName}", accessor.Assembly);
        using var resources = manager.GetResourceSet(CultureInfo.InvariantCulture, true, true)!;
        var keys = resources.Cast<DictionaryEntry>().Select(entry => (string)entry.Key).Order().ToArray();
        var properties = accessor.GetProperties(BindingFlags.Public | BindingFlags.Static)
            .OrderBy(property => property.Name).ToArray();

        Assert.Equal(keys, properties.Select(property => property.Name));
        foreach (var property in properties)
            Assert.False(string.IsNullOrEmpty((string?)property.GetValue(null)));
    }
}
