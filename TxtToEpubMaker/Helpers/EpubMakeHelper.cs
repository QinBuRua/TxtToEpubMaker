using System.Diagnostics;
using System.Xml.Linq;
using TxtToEpubMaker.Structs;

namespace TxtToEpubMaker.Helpers;

public static class EpubMakeHelper
{
    public static XDocument MakeContentOpfTemplate()
    {
        XNamespace xmlnsOpf = "http://www.idpf.org/2007/opf";
        XNamespace xmlnsDc = "http://purl.org/dc/elements/1.1/";
        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XElement(xmlnsOpf + "package",
                new XAttribute("version", "3.0"),
                new XAttribute("unique-identifier", "book-id"),
                new XAttribute(XNamespace.Xml + "lang", "zh-CN"),
                new XElement(xmlnsOpf + "metadata",
                    new XAttribute(XNamespace.Xmlns + "dc", xmlnsDc)
                ),
                new XElement(xmlnsOpf + "manifest"),
                new XElement(xmlnsOpf + "spine")
            )
        );
    }

    public static XDocument MakeTocXhtmlTemplate()
    {
        XNamespace xmlns = "http://www.w3.org/1999/xhtml";
        XNamespace xmlnsEpub = "http://www.idpf.org/2007/ops";
        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XDocumentType("html", "", "", null),
            new XElement(xmlns + "html",
                new XAttribute(XNamespace.Xml + "lang", "zh-CN"),
                new XAttribute("lang", "zh-CN"),
                new XAttribute(XNamespace.Xmlns + "epub", xmlnsEpub.NamespaceName),
                new XElement(xmlns + "head",
                    new XElement(xmlns + "title", "目录")
                ),
                new XElement(xmlns + "body",
                    new XElement(xmlns + "nav",
                        new XAttribute(xmlnsEpub + "type", "toc"),
                        new XAttribute("id", "toc"),
                        new XElement(xmlns + "h1", "目录"),
                        new XElement(xmlns + "ol")
                    )
                )
            )
        );
    }

    public static XDocument MakeXhtmlTemplate(in string? title)
    {
        XNamespace xmlns = "http://www.w3.org/1999/xhtml";
        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XDocumentType("html",
                "-//W3C//DTD XHTML 1.0 Strict//EN",
                "http://www.w3.org/TR/xhtml1/DTD/xhtml1-strict.dtd",
                null),
            new XElement(xmlns + "html",
                new XAttribute(XNamespace.Xml + "lang", "zh-CN"),
                new XAttribute("lang", "zh-CN"),
                new XElement(xmlns + "head", new XElement(xmlns + "title", title)),
                new XElement(xmlns + "body", new XElement(xmlns + "h1", title))
            )
        );
    }

    public static XDocument MakeTocNcxTemplate(in TranslationTask.BookContent bookContent)
    {
        XNamespace xmlns = "http://www.daisy.org/z3986/2005/ncx/";

        return new XDocument(
            new XDeclaration("1.0", "UTF-8", null),
            new XDocumentType("ncx",
                "-//NISO//DTD ncx 2005-1//EN",
                "http://www.daisy.org/z3986/2005/ncx-2005-1.dtd",
                null),
            new XElement(xmlns + "ncx",
                new XAttribute("version", "2005-1"),
                new XAttribute(XNamespace.Xml + "lang", "zh-CN"),
                new XElement(xmlns + "head",
                    new XElement(xmlns + "meta",
                        new XAttribute("name", "dtb:uid"),
                        new XAttribute("content", bookContent.BookId)),
                    new XElement(xmlns + "meta",
                        new XAttribute("name", "dtb:depth"),
                        new XAttribute("content", "2")),
                    new XElement(xmlns + "meta",
                        new XAttribute("name", "dtb:totalPageCount"),
                        new XAttribute("content", "0")),
                    new XElement(xmlns + "meta",
                        new XAttribute("name", "dtb:maxPageNumber"),
                        new XAttribute("content", "0"))),
                new XElement(xmlns + "docTitle",
                    new XElement(xmlns + "text", bookContent.Title)),
                new XElement(xmlns + "docAuthor",
                    new XElement(xmlns + "text", bookContent.Author)),
                new XElement(xmlns + "navMap")
            )
        );
    }

    public static XDocument TxtToXml(in TranslationTask.BookContent.ChapterLinker chapterLinker)
    {
        if (!File.Exists(chapterLinker.FilePath))
        {
            throw new IOException($"Missing file {chapterLinker.FilePath}");
        }

        XNamespace xmlns = "http://www.w3.org/1999/xhtml";
        var xhtmlDoc = MakeXhtmlTemplate(chapterLinker.Title);
        var lines = File.ReadAllLines(chapterLinker.FilePath);

        Debug.Assert(xhtmlDoc.Root != null);
        var body = xhtmlDoc.Root.Element(xmlns + "body") ?? throw new ArgumentNullException();
        var emptyFlag = false;
        foreach (var line in lines)
        {
            if (!string.IsNullOrWhiteSpace(line))
            {
                body.Add(new XElement(xmlns + "p", line));
                emptyFlag = false;
            }

            if (emptyFlag)
            {
                body.Add(new XElement(xmlns + "br"));
            }

            emptyFlag = true;
        }

        return xhtmlDoc;
    }
}