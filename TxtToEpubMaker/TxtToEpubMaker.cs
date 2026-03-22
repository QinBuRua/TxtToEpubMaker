using System.Diagnostics;
using System.IO.Compression;
using System.Xml.Linq;
using TxtToEpubMaker.Helpers;
using TxtToEpubMaker.Structs;
using Zio;
using Zio.FileSystems;

namespace TxtToEpubMaker;

public class TxtToEpubMaker(TranslationTask translationTask)
{
    public static EpubResult MakeEpubFromTask(TranslationTask translationTask)
    {
        var statue = new EpubResult
        {
            Success = true
        };

        try
        {
            var txtToEpub = new TxtToEpubMaker(translationTask);

            if (File.Exists(txtToEpub._outputFilePath) && !txtToEpub._forceRemove)
            {
                throw new IOException($"File \"{txtToEpub._outputFilePath}\" has existed, and not allow ForceRemove");
            }

            txtToEpub.GenerateTemp();
            txtToEpub.PackUp();

            statue.Success = true;
            statue.EpubPath = txtToEpub._outputFilePath;
        }
        catch (Exception exception)
        {
            statue.Success = false;
            statue.ErrorMessage = $"{exception.GetType().Name}: {exception}";
        }

        return statue;
    }


    private void GenerateTemp()
    {
        GenerateTempMetaInf();
        GenerateTempAllXhtmlContentWithRegister();
        GenerateTempCoverXhtmlWithRegister();
        GenerateTempTocXhtmlWithRegister();
        GenerateTempTocNcxWithRegister();
        GenerateTempContentOpf();
    }

    private void GenerateTempMetaInf()
    {
        XNamespace xmlns = "urn:oasis:names:tc:opendocument:xmlns:container";
        var container = new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(xmlns + "container",
                new XAttribute("version", "1.0"),
                new XElement(xmlns + "rootfiles",
                    new XElement(xmlns + "rootfile",
                        new XAttribute("full-path", "OEBPS/content.opf"),
                        new XAttribute("media-type", "application/oebps-package+xml")
                    )
                )
            )
        );
        WriteAllTextInTemp("/META-INF/container.xml", container.ToString());
    }

    private void GenerateTempAllXhtmlContentWithRegister()
    {
        var volumes = _bookContent.Volumes;
        const string xhtmlRootPath = $"/OEBPS/xhtml";
        foreach (var (volume, index) in volumes.Select((value, index) => (value, index + 1)))
        {
            GenerateTempVolumeWithRegister(Path.Combine(xhtmlRootPath, $"volume{index}"), volume);
        }
    }

    private void GenerateTempCoverXhtmlWithRegister()
    {
        RegisterCoverPath();
        var coverXhtml =
            EpubMakeHelper.MakeCoverXhtmlTemplate(Path.Combine("..", _registeredOebpsList.Cover.VirtualPath)
                .Replace('\\', '/'));

        WriteAllTextInTemp("/OEBPS/xhtml/cover.xhtml", coverXhtml.ToString());
        _registeredOebpsList.CoverXhtml = "xhtml/cover.xhtml";
    }

    private void GenerateTempTocXhtmlWithRegister()
    {
        XNamespace xmlns = "http://www.w3.org/1999/xhtml";
        var tocXhtml = EpubMakeHelper.MakeTocXhtmlTemplate();

        Debug.Assert(tocXhtml.Root != null);
        var navElement = tocXhtml.Root.Element(xmlns + "body")?.Element(xmlns + "nav");
        var volumesOl = navElement?.Element(xmlns + "ol") ?? throw new ArgumentNullException();

        volumesOl.AddFirst(new XElement(xmlns + "li",
            new XElement(xmlns + "a",
                new XAttribute("href", "cover.xhtml"),
                "封面")));

        foreach (var volume in _registeredOebpsList.Volumes)
        {
            var volumeLi = new XElement(xmlns + "li");
            volumeLi.Add(new XElement(xmlns + "span", volume.Title));

            var chaptersOl = new XElement(xmlns + "ol");
            foreach (var chapterLinker in volume.Chapters)
            {
                var relativePath = chapterLinker.Path.Replace('\\', '/').TrimStart("xhtml/").ToString();
                var fixedPath = relativePath.Replace('\\', '/');
                var chapterLi = new XElement(xmlns + "li");

                chapterLi.Add(new XElement(xmlns + "a", new XAttribute("href", fixedPath), chapterLinker.Title));
                chaptersOl.Add(chapterLi);
            }

            volumeLi.Add(chaptersOl);
            volumesOl.Add(volumeLi);
        }

        WriteAllTextInTemp("/OEBPS/xhtml/toc.xhtml", tocXhtml.ToString());
        _registeredOebpsList.TocXhtml = "xhtml/toc.xhtml";
    }

    private void GenerateTempTocNcxWithRegister()
    {
        XNamespace xmlns = "http://www.daisy.org/z3986/2005/ncx/";
        var tocNcx = EpubMakeHelper.MakeTocNcxTemplate(_bookContent);

        Debug.Assert(tocNcx.Root != null);
        var navMap = tocNcx.Root.Element(xmlns + "navMap")
                     ?? throw new InvalidOperationException("navMap element not found");

        var playOrder = 1;

        navMap.Add(new XElement(xmlns + "navPoint",
            new XAttribute("id", "cover"),
            new XAttribute("playOrder", playOrder++),
            new XElement(xmlns + "navLabel",
                new XElement(xmlns + "text", "封面")),
            new XElement(xmlns + "content",
                new XAttribute("src", Path.Combine("..", _registeredOebpsList.CoverXhtml).Replace('\\', '/')))
        ));

        foreach (var (volume, volIdx) in _registeredOebpsList.Volumes.Select((v, i) => (v, i + 1)))
        {
            var volumeNavPoint = new XElement(xmlns + "navPoint",
                new XAttribute("id", $"volume{volIdx}"),
                new XAttribute("playOrder", playOrder++),
                new XElement(xmlns + "navLabel",
                    new XElement(xmlns + "text", volume.Title)),
                new XElement(xmlns + "content",
                    new XAttribute("src",
                        Path.Combine("..", volume.Chapters.FirstOrDefault().Path ?? "").Replace('\\', '/')))
            );

            foreach (var (chapter, chapIdx) in volume.Chapters.Select((c, i) => (c, i + 1)))
            {
                var chapterNavPoint = new XElement(xmlns + "navPoint",
                    new XAttribute("id", $"volume{volIdx}_chapter{chapIdx}"),
                    new XAttribute("playOrder", playOrder++),
                    new XElement(xmlns + "navLabel",
                        new XElement(xmlns + "text", chapter.Title)),
                    new XElement(xmlns + "content",
                        new XAttribute("src", Path.Combine("..", chapter.Path).Replace('\\', '/')))
                );
                volumeNavPoint.Add(chapterNavPoint);
            }

            navMap.Add(volumeNavPoint);
        }

        WriteAllTextInTemp("/OEBPS/backup/toc.ncx", tocNcx.ToString());
        _registeredOebpsList.TocNcx = "backup/toc.ncx";
    }

    private void GenerateTempContentOpf()
    {
        var contentOpf = ContentOpfHelper.MakeContentOpfFrom(_registeredOebpsList, _bookContent);
        WriteAllTextInTemp("/OEBPS/content.opf", contentOpf.ToString());
    }

    private void RegisterCoverPath()
    {
        var fullPath = _bookContent.CoverPath;
        if (!File.Exists(fullPath))
        {
            throw new FileNotFoundException($"Missing file \"{fullPath}\"");
        }

        var fileExtension = Path.GetExtension(fullPath);
        _registeredOebpsList.Cover = new RegisteredOebpsList.CoverLinker
        {
            PhysicalPath = fullPath,
            VirtualPath = $"images/cover{fileExtension}"
        };
    }

    private void GenerateTempVolumeWithRegister(in string prefix, in TranslationTask.BookContent.Volume volume)
    {
        _registeredOebpsList.Volumes.Add(new RegisteredOebpsList.Volume
        {
            Title = volume.Title
        });

        foreach (var (chapterLinker, index) in volume.Chapters.Select((value, index) => (value, index + 1)))
        {
            GenerateTempChapterWithRegister(Path.Combine(prefix, $"chapter{index}.xhtml"), chapterLinker);
        }
    }

    private void GenerateTempChapterWithRegister(in string filename,
        in TranslationTask.BookContent.ChapterLinker chapterLinker)
    {
        WriteAllTextInTemp(filename, EpubMakeHelper.TxtToXml(chapterLinker).ToString());
        _registeredOebpsList.Volumes.Last().Chapters.Add(new RegisteredOebpsList.ChapterLinker
        {
            Title = chapterLinker.Title,
            Path = filename.TrimStart("/OEBPS/").ToString().Replace('\\', '/')
        });
    }

    private void PackUp()
    {
        using var memoryStream = new MemoryStream();

        using (var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true))
        {
            using (var stream = zipArchive.CreateEntry("/mimetype", CompressionLevel.NoCompression).Open())
            {
                stream.Write("application/epub+zip"u8);
            }

            foreach (var file in _tempFileSystem.EnumerateFiles("/", "*", SearchOption.AllDirectories))
            {
                var path = file.FullName.TrimStart('/');
                using var tmpZipStream = zipArchive.CreateEntry(path, CompressionLevel.Optimal)
                    .Open();
                using var tmpFileStream = _tempFileSystem.OpenFile(file, FileMode.Open, FileAccess.Read);

                tmpFileStream.CopyTo(tmpZipStream);
            }

            {
                var cover = _registeredOebpsList.Cover;
                using var coverStream = new FileStream(cover.PhysicalPath, FileMode.Open,
                    FileAccess.Read);

                var zipFilePath = Path.Combine("/OEBPS", cover.VirtualPath).Replace('\\', '/');
                using var tmpZipStream = zipArchive.CreateEntry(zipFilePath, CompressionLevel.Fastest).Open();

                coverStream.CopyTo(tmpZipStream);
            }
        }

        memoryStream.Position = 0;
        Directory.CreateDirectory(Path.GetDirectoryName(_outputFilePath) ??
                                  throw new IOException(
                                      $"File \"{_outputFilePath}\" does not has parent directory"));
        using var outputFileStream = new FileStream(_outputFilePath, FileMode.Create, FileAccess.Write);

        memoryStream.CopyTo(outputFileStream);
    }

    private void WriteAllTextInTemp(in UPath uPath, in string text)
    {
        var parent = uPath.GetDirectory();
        _tempFileSystem.CreateDirectory(parent);
        _tempFileSystem.WriteAllText(uPath, text);
    }

    private readonly TranslationTask.BookContent _bookContent = translationTask.SkipIfTxtNotExists
        ? EpubMakeHelper.FilterTxtIfExists(translationTask.Content)
        : translationTask.Content;

    private readonly string _outputFilePath = translationTask.OutFilePath;
    private readonly bool _forceRemove = translationTask.ForceRemove;
    private RegisteredOebpsList _registeredOebpsList = new();

    private readonly MemoryFileSystem _tempFileSystem = new();

    private struct RegisteredOebpsList()
    {
        public List<Volume> Volumes { get; set; } = [];
        public string TocXhtml { get; set; }

        public string TocNcx { get; set; }

        public string CoverXhtml { get; set; }
        public CoverLinker Cover { get; set; }

        public record struct CoverLinker
        {
            public string PhysicalPath { get; init; }
            public string VirtualPath { get; init; }
        }

        public struct Volume()
        {
            public required string Title { get; init; }
            public List<ChapterLinker> Chapters { get; set; } = [];
        }

        public struct ChapterLinker
        {
            public required string Title { get; init; }
            public required string Path { get; init; }
        }
    }

    private class ContentOpfHelper(
        in RegisteredOebpsList registeredOebpsList,
        in TranslationTask.BookContent bookContent)
    {
        public static XDocument MakeContentOpfFrom(in RegisteredOebpsList registeredOebpsList,
            in TranslationTask.BookContent bookContent)
        {
            var helper = new ContentOpfHelper(registeredOebpsList, bookContent);

            helper.GenerateMetadata();
            helper.GenerateManifestAndSpine();

            return helper._contentOpf;
        }

        private void GenerateMetadata()
        {
            Debug.Assert(_contentOpf.Root != null);
            var metadata = _contentOpf.Root.Element(_xmlnsOpf + "metadata")
                           ?? throw new ArgumentNullException();

            metadata.Add(new XElement(_xmlnsDc + "identifier", new XAttribute("id", "book-id"), _bookContent.BookId));
            metadata.Add(new XElement(_xmlnsDc + "title", _bookContent.Title));
            metadata.Add(new XElement(_xmlnsDc + "creator", new XAttribute("id", "author"), _bookContent.Author));
            metadata.Add(new XElement(_xmlnsDc + "lang", "zh-CN"));
            metadata.Add(new XElement(_xmlnsOpf + "meta", new XAttribute("property", "dcterms:modified"),
                TimeHelper.GetNowUtcTimeString()));
            metadata.Add(new XElement(_xmlnsOpf + "meta",
                new XAttribute("name", "cover"),
                new XAttribute("content", "cover-image")));
        }

        private void GenerateManifestAndSpine()
        {
            Debug.Assert(_contentOpf.Root != null);
            var manifest = _contentOpf.Root.Element(_xmlnsOpf + "manifest") ?? throw new ArgumentNullException();
            var spine = _contentOpf.Root.Element(_xmlnsOpf + "spine") ?? throw new ArgumentNullException();

            AddCoverTo(ref manifest);
            AddTocTo(ref manifest, ref spine);
            AddVolumesTo(ref manifest, ref spine);
        }

        private void AddCoverTo(ref XElement manifest)
        {
            manifest.Add(new XElement(_xmlnsOpf + "item",
                new XAttribute("id", "cover"),
                new XAttribute("href", _registeredOebpsList.CoverXhtml),
                new XAttribute("media-type", "application/xhtml+xml")));

            var coverImageHref = _registeredOebpsList.Cover.VirtualPath.Replace('\\', '/');
            var ext = Path.GetExtension(_registeredOebpsList.Cover.PhysicalPath).ToLowerInvariant();
            var mediaType = ext switch
            {
                ".jpg" or ".jpeg" => "image/jpeg",
                ".png" => "image/png",
                ".gif" => "image/gif",
                ".svg" => "image/svg+xml",
                _ => "application/octet-stream"
            };
            manifest.Add(new XElement(_xmlnsOpf + "item",
                new XAttribute("id", "cover-image"),
                new XAttribute("href", coverImageHref),
                new XAttribute("media-type", mediaType),
                new XAttribute("properties", "cover-image")));
        }

        private void AddTocTo(ref XElement manifest, ref XElement spine)
        {
            manifest.Add(new XElement(_xmlnsOpf + "item",
                new XAttribute("id", "toc"),
                new XAttribute("href", _registeredOebpsList.TocXhtml),
                new XAttribute("media-type", "application/xhtml+xml"),
                new XAttribute("properties", "nav")));

            manifest.Add(new XElement(_xmlnsOpf + "item",
                new XAttribute("id", "ncx"),
                new XAttribute("href", _registeredOebpsList.TocNcx),
                new XAttribute("media-type", "application/x-dtbncx+xml")));

            spine.Add(new XElement(_xmlnsOpf + "itemref", new XAttribute("idref", "ncx")));

            if (spine.Attributes().All(a => a.Name != "toc"))
                spine.Add(new XAttribute("toc", "ncx"));
        }

        private void AddVolumesTo(ref XElement manifest, ref XElement spine)
        {
            foreach (var (volume, indexV) in _registeredOebpsList.Volumes.Select((volume, index) =>
                         (volume, index + 1)))
            {
                foreach (var (chapterLinker, indexC) in volume.Chapters.Select((chapter, index) =>
                             (chapter, index + 1)))
                {
                    var relativePath = chapterLinker.Path;
                    var id = $"volume{indexV}chapter{indexC}";
                    manifest.Add(new XElement(_xmlnsOpf + "item",
                        new XAttribute("id", id),
                        new XAttribute("href", relativePath),
                        new XAttribute("media-type", "application/xhtml+xml")
                    ));
                    spine.Add(new XElement(_xmlnsOpf + "itemref", new XAttribute("idref", id)));
                }
            }
        }

        private readonly XNamespace _xmlnsDc = "http://purl.org/dc/elements/1.1/";
        private readonly XNamespace _xmlnsOpf = "http://www.idpf.org/2007/opf";
        private readonly XDocument _contentOpf = EpubMakeHelper.MakeContentOpfTemplate();
        private readonly RegisteredOebpsList _registeredOebpsList = registeredOebpsList;
        private readonly TranslationTask.BookContent _bookContent = bookContent;
    }
}