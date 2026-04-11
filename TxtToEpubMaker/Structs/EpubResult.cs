using System.Diagnostics.CodeAnalysis;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace TxtToEpubMaker.Structs;

public struct EpubResult
{
    public string? EpubPath { get; init; }
    public required bool Success { get; init; }
    public string? ErrorMessage { get; init; }

    [JsonIgnore] public Exception? InnerException { get; init; }

    [SetsRequiredMembers]
    public EpubResult(string epubPath)
    {
        EpubPath = epubPath;
        Success = true;
        ErrorMessage = null;
        InnerException = null;
    }

    [SetsRequiredMembers]
    public EpubResult(Exception exception)
    {
        EpubPath = null;
        Success = false;
        InnerException = exception;

        ErrorMessage = InnerException switch
        {
            JsonException jsonException 
                => $"{jsonException.GetType().FullName}: {jsonException.Message} | Line: {jsonException.LineNumber}",
            _ => $"{InnerException.GetType().FullName}: {InnerException.Message}"
        };
    }
}