using System.Diagnostics;
using System.IO.Compression;
using System.Text.Json;
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
        catch (JsonException jsonException)
        {
            statue.Success = false;
            statue.ErrorMessage = $"{nameof(JsonException)}: {jsonException}";
        }
        catch (IOException ioException)
        {
            statue.Success = false;
            statue.ErrorMessage = $"{nameof(IOException)}: {ioException}";
        }

        return statue;
    }


    private void GenerateTemp()
    {
        GenerateTempMetaInf();
        GenerateTempAllXhtmlContentWithRegister();
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

    private void GenerateTempTocXhtmlWithRegister()
    {
        XNamespace xmlns = "http://www.w3.org/1999/xhtml";
        var tocXhtml = EpubMakeHelper.MakeTocXhtmlTemplate();

        Debug.Assert(tocXhtml.Root != null);
        var navElement = tocXhtml.Root.Element(xmlns + "body")?.Element(xmlns + "nav");
        var volumesOl = navElement?.Element(xmlns + "ol");

        foreach (var volume in _registeredOebpsList.Volumes)
        {
            var volumeLi = new XElement(xmlns + "li");
            volumeLi.Add(new XElement(xmlns + "span", volume.Title));

            var chaptersOl = new XElement(xmlns + "ol");
            foreach (var chapterLinker in volume.Chapters)
            {
                var relativePath = chapterLinker.Path;
                var fixedPath = relativePath.Replace('\\', '/');
                var chapterLi = new XElement(xmlns + "li");

                chapterLi.Add(new XElement(xmlns + "a", new XAttribute("href", fixedPath), chapterLinker.Title));
                chaptersOl.Add(chapterLi);
            }

            volumeLi.Add(chaptersOl);
            volumesOl?.Add(volumeLi);
        }

        WriteAllTextInTemp("/OEBPS/toc.xhtml", tocXhtml.ToString());
        _registeredOebpsList.TocXhtml = "toc.xhtml";
    }

    private void GenerateTempTocNcxWithRegister()
    {
        XNamespace xmlns = "http://www.daisy.org/z3986/2005/ncx/";
        var tocNcx = EpubMakeHelper.MakeTocNcxTemplate(_bookContent);

        Debug.Assert(tocNcx.Root != null);
        var navMap = tocNcx.Root.Element(xmlns + "navMap")
                     ?? throw new InvalidOperationException("navMap element not found");

        var playOrder = 1;
        foreach (var (volume, volIdx) in _registeredOebpsList.Volumes.Select((v, i) => (v, i + 1)))
        {
            // 卷的 navPoint（一级目录）
            var volumeNavPoint = new XElement(xmlns + "navPoint",
                new XAttribute("id", $"volume{volIdx}"),
                new XAttribute("playOrder", playOrder++),
                new XElement(xmlns + "navLabel",
                    new XElement(xmlns + "text", volume.Title)),
                // 指向卷的第一个章节（如果存在）
                new XElement(xmlns + "content",
                    new XAttribute("src", volume.Chapters.FirstOrDefault().Path ?? ""))
            );

            // 章节子目录（二级）
            foreach (var (chapter, chapIdx) in volume.Chapters.Select((c, i) => (c, i + 1)))
            {
                var chapterNavPoint = new XElement(xmlns + "navPoint",
                    new XAttribute("id", $"volume{volIdx}_chapter{chapIdx}"),
                    new XAttribute("playOrder", playOrder++),
                    new XElement(xmlns + "navLabel",
                        new XElement(xmlns + "text", chapter.Title)),
                    new XElement(xmlns + "content",
                        new XAttribute("src", chapter.Path))
                );
                volumeNavPoint.Add(chapterNavPoint);
            }

            navMap.Add(volumeNavPoint);
        }

        // 写入临时文件系统
        WriteAllTextInTemp("/OEBPS/toc.ncx", tocNcx.ToString());
        _registeredOebpsList.TocNcx = "toc.ncx";
    }

    private void GenerateTempContentOpf()
    {
        const string contentOpfPath = "/OEBPS/content.opf";
        var contentOpf = EpubMakeHelper.MakeContentOpfTemplate();

        Debug.Assert(contentOpf.Root != null);
        XNamespace xmlnsDc = "http://purl.org/dc/elements/1.1/";
        XNamespace xmlnsOpf = "http://www.idpf.org/2007/opf";

        var metadata = contentOpf.Root.Element(xmlnsOpf + "metadata")
                       ?? throw new ArgumentNullException();
        metadata.Add(new XElement(xmlnsDc + "identifier", new XAttribute("id", "book-id"), _bookContent.BookId));
        metadata.Add(new XElement(xmlnsDc + "title", _bookContent.Title));
        metadata.Add(new XElement(xmlnsDc + "creator", new XAttribute("id", "author"), _bookContent.Author));
        metadata.Add(new XElement(xmlnsDc + "lang", "zh-CN"));
        metadata.Add(new XElement("meta", new XAttribute("property", "dcterms:modified"),
            TimeHelper.GetNowUtcTimeString()));

        var manifest = contentOpf.Root.Element(xmlnsOpf + "manifest") ?? throw new ArgumentNullException();
        var spine = contentOpf.Root.Element(xmlnsOpf + "spine") ?? throw new ArgumentNullException();

        manifest.Add(new XElement(xmlnsOpf + "item",
            new XAttribute("id", "toc"),
            new XAttribute("href", "toc.xhtml"),
            new XAttribute("media-type", "application/xhtml+xml"),
            new XAttribute("properties", "nav")));

        manifest.Add(new XElement(xmlnsOpf + "item",
            new XAttribute("id", "ncx"),
            new XAttribute("href", "toc.ncx"),
            new XAttribute("media-type", "application/x-dtbncx+xml")));

        spine.Add(new XElement(xmlnsOpf + "itemref", new XAttribute("idref", "ncx")));

        if (spine.Attributes().All(a => a.Name != "toc"))
            spine.Add(new XAttribute("toc", "ncx"));

        foreach (var (volume, indexV) in _registeredOebpsList.Volumes.Select((volume, index) =>
                     (volume, index + 1)))
        {
            foreach (var (chapterLinker, indexC) in volume.Chapters.Select((chapter, index) =>
                         (chapter, index + 1)))
            {
                var relativePath = chapterLinker.Path;
                var id = $"volume{indexV}chapter{indexC}";
                manifest.Add(new XElement(xmlnsOpf + "item",
                    new XAttribute("id", id),
                    new XAttribute("href", relativePath),
                    new XAttribute("media-type", "application/xhtml+xml")
                ));
                spine.Add(new XElement(xmlnsOpf + "itemref", new XAttribute("idref", id)));
            }
        }

        WriteAllTextInTemp(contentOpfPath, contentOpf.ToString());
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
        var memoryStream = new MemoryStream();
        var zipArchive = new ZipArchive(memoryStream, ZipArchiveMode.Create, leaveOpen: true);

        using (var stream = zipArchive.CreateEntry("mimetype", CompressionLevel.NoCompression).Open())
        {
            stream.Write("application/epub+zip"u8);
        }

        foreach (var file in _tempFileSystem.EnumerateFiles("/", "*", SearchOption.AllDirectories))
        {
            var path = file.FullName.TrimStart('/');
            var tmpZipStream = zipArchive.CreateEntry(path, CompressionLevel.Optimal)
                .Open();
            var tmpFileStream = _tempFileSystem.OpenFile(file, FileMode.Open, FileAccess.Read);

            tmpFileStream.CopyTo(tmpZipStream);

            tmpFileStream.Dispose();
            tmpZipStream.Dispose();
        }

        zipArchive.Dispose();

        memoryStream.Position = 0;
        Directory.CreateDirectory(Path.GetDirectoryName(_outputFilePath) ??
                                  throw new IOException($"File \"{_outputFilePath}\" does not has parent directory"));
        var outputFileStream = new FileStream(_outputFilePath, FileMode.Create, FileAccess.Write);

        memoryStream.CopyTo(outputFileStream);

        memoryStream.Dispose();
        outputFileStream.Dispose();
    }

    private readonly TranslationTask.BookContent _bookContent = translationTask.Content;
    private readonly string _outputFilePath = translationTask.OutFilePath;
    private readonly bool _forceRemove = translationTask.ForceRemove;
    private RegisteredOebpsList _registeredOebpsList = new();

    private readonly MemoryFileSystem _tempFileSystem = new();

    private struct RegisteredOebpsList()
    {
        public List<Volume> Volumes { get; set; } = [];
        public string TocXhtml { get; set; }

        public string TocNcx { get; set; }

        public struct Volume()
        {
            public required string Title { get; set; }
            public List<ChapterLinker> Chapters { get; set; } = [];
        }

        public struct ChapterLinker
        {
            public required string Title { get; set; }
            public required string Path { get; set; }
        }
    }

    private void WriteAllTextInTemp(in UPath uPath, in string text)
    {
        var parent = uPath.GetDirectory();
        _tempFileSystem.CreateDirectory(parent);
        _tempFileSystem.WriteAllText(uPath, text);
    }
}