using System.Text.Json.Serialization;

namespace TxtToEpubMaker.Structs;

public struct EpubResult
{
    public string? EpubPath { get; set; }
    public required bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}