namespace TxtToEpubMaker.Structs;

public struct TranslationTask()
{
    public required BookContent Content { get; set; }
    public required string OutFilePath { get; set; }

    public bool ForceRemove { get; set; } = false;

    public struct BookContent
    {
        public required string Title { get; set; }
        public required string Author { get; set; }
        public required string BookId { get; set; }
        public required string CoverPath { get; set; }
        public required List<Volume> Volumes { get; set; }

        public struct ChapterLinker
        {
            public required string Title { get; set; }
            public required string FilePath { get; set; }
        }

        public struct Volume
        {
            public required string Title { get; set; }
            public required List<ChapterLinker> Chapters { get; set; }
        }
    }
}