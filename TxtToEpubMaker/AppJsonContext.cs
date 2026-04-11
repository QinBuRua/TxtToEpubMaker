using System.Text.Json;
using System.Text.Json.Serialization;
using TxtToEpubMaker.Structs;

namespace TxtToEpubMaker;

[JsonSerializable(typeof(EpubResult))]
[JsonSerializable(typeof(TranslationTask))]
[JsonSerializable(typeof(TranslationTask.BookContentSet))]
[JsonSerializable(typeof(TranslationTask.BookContentSet.Volume))]
[JsonSerializable(typeof(TranslationTask.BookContentSet.ChapterLinker))]
public partial class AppJsonContext : JsonSerializerContext
{
    public static Lazy<JsonSerializerOptions> DefaultOptions { get; } = new(() => new JsonSerializerOptions
    {
        TypeInfoResolver = Default,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        MaxDepth = 16,
    });

    public static T Deserialize<T>(string jsonString)
    {
        return JsonSerializer.Deserialize<T>(jsonString, DefaultOptions.Value)
               ?? throw new InvalidOperationException(
                   $"Failed to deserialize {typeof(T).FullName}: result is null");
    }

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, DefaultOptions.Value);
    }
}