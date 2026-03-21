namespace TxtToEpubMaker.Structs;

public struct EpubResult
{
    public string? EpubPath { get; set; }
    public bool Success { get; set; }
    public string? ErrorMessage { get; set; }
}