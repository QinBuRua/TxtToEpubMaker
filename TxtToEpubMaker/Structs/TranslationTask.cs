namespace TxtToEpubMaker.Structs;

public struct TranslationTask()
{
    public required BookContentSet BookContent { get; init; }
    public required string OutputFilePath { get; init; }

    public bool ForceRemove { get; init; } = false;

    public bool SkipIfTxtNotExists { get; init; } = false;

    public struct BookContentSet
    {
        public required string Title { get; set; }
        public required string Author { get; set; }
        public required string BookId { get; set; }
        public required string CoverPath { get; set; }
        public required List<Volume> Volumes { get; init; }

        public struct ChapterLinker
        {
            public required string Title { get; set; }
            public required string FilePath { get; set; }
        }

        public struct Volume
        {
            public required string Title { get; set; }
            public required List<ChapterLinker> Chapters { get; init; }
        }
    }
}