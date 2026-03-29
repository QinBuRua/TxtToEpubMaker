using System.Text.Json;
using System.Text.Json.Serialization;
using TxtToEpubMaker.Structs;

namespace TxtToEpubMaker;

[JsonSerializable(typeof(EpubResult))]
[JsonSerializable(typeof(TranslationTask))]
[JsonSerializable(typeof(TranslationTask.BookContent))]
[JsonSerializable(typeof(TranslationTask.BookContent.Volume))]
[JsonSerializable(typeof(TranslationTask.BookContent.ChapterLinker))]
public partial class AppJsonContext : JsonSerializerContext
{
    public static Lazy<JsonSerializerOptions> DefaultOptions { get; } = new(() => new JsonSerializerOptions
    {
        TypeInfoResolver = Default,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
        MaxDepth = 16
    });

    public static T Deserialize<T>(in string jsonString)
    {
        if (string.IsNullOrEmpty(jsonString))
        {
            throw new ArgumentNullException(nameof(jsonString));
        }

        return JsonSerializer.Deserialize<T>(jsonString, DefaultOptions.Value)
               ?? throw new JsonException(
                   $"Fail to deserialize {typeof(T).Namespace}.{typeof(T).FullName}, Value is null");
    }

    public static string Serialize<T>(T value)
    {
        return JsonSerializer.Serialize(value, DefaultOptions.Value);
    }
}