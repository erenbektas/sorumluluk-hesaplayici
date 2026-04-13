using System.Text.RegularExpressions;
using System.Windows;

namespace SorumlulukHesaplama.Services;

public static class ClipboardService
{
    /// <summary>
    /// Copy result to clipboard with RTF, HTML, and plain text formats.
    /// </summary>
    public static void CopyToClipboard(string html, string plainText)
    {
        var wordHtml = WrapHtmlForWord(html);
        var rtf = HtmlToRtf(html);

        var dataObject = new DataObject();
        dataObject.SetData(DataFormats.Rtf, rtf);
        dataObject.SetData(DataFormats.Html, CreateCfHtml(wordHtml));
        dataObject.SetData(DataFormats.UnicodeText, plainText);
        Clipboard.SetDataObject(dataObject, true);
    }

    private static string WrapHtmlForWord(string html)
    {
        return "<!DOCTYPE html>\n<html>\n<head>\n<meta charset=\"UTF-8\">\n<style>\n" +
            "body { font-family: Calibri, sans-serif; font-size: 11pt; }\n" +
            "p { margin: 0 0 12pt 0; }\n</style>\n</head>\n<body>\n" +
            html + "\n</body>\n</html>";
    }

    /// <summary>
    /// Create CF_HTML clipboard format with proper headers.
    /// </summary>
    private static string CreateCfHtml(string html)
    {
        const string header = "Version:0.9\r\nStartHTML:{0:D10}\r\nEndHTML:{1:D10}\r\nStartFragment:{2:D10}\r\nEndFragment:{3:D10}\r\n";
        var startHtml = string.Format(header, 0, 0, 0, 0).Length;
        var startFragment = startHtml + html.IndexOf("<body>", StringComparison.Ordinal) + 6;
        var endFragment = startHtml + html.IndexOf("</body>", StringComparison.Ordinal);
        var endHtml = startHtml + html.Length;
        return string.Format(header, startHtml, endHtml, startFragment, endFragment) + html;
    }

    private static string HtmlToRtf(string html)
    {
        var rtf = "{\\rtf1\\ansi\\ansicpg1254\\deff0";
        rtf += "{\\fonttbl{\\f0\\fswiss\\fcharset162 Calibri;}}";
        rtf += "{\\colortbl;\\red0\\green0\\blue0;\\red255\\green0\\blue0;}";
        rtf += "\\viewkind4\\uc1\\pard\\f0\\fs22 ";

        var content = html;

        // Handle red color spans first
        content = Regex.Replace(content, @"<span style=""color:red;"">([^<]*)</span>", "{\\cf2 $1}", RegexOptions.IgnoreCase);

        // Nested bold+italic
        content = Regex.Replace(content, @"<b><i>([^<]*)</i></b>", "{\\b\\i $1}", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, @"<i><b>([^<]*)</b></i>", "{\\b\\i $1}", RegexOptions.IgnoreCase);

        // Bold+italic with red color
        content = Regex.Replace(content, @"<b><i>\{\\cf2 ([^}]*)\}</i></b>", "{\\b\\i\\cf2 $1}", RegexOptions.IgnoreCase);

        // Bold
        content = Regex.Replace(content, @"<b>", "{\\b ", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, @"</b>", "}", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, @"<strong>", "{\\b ", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, @"</strong>", "}", RegexOptions.IgnoreCase);

        // Italic
        content = Regex.Replace(content, @"<i>", "{\\i ", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, @"</i>", "}", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, @"<em>", "{\\i ", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, @"</em>", "}", RegexOptions.IgnoreCase);

        // Tabs
        content = content.Replace("&#9;", "\\tab ");
        content = content.Replace("\t", "\\tab ");

        // Non-breaking spaces
        content = content.Replace("&#160;", "\\~");
        content = content.Replace("&nbsp;", "\\~");

        // Line breaks
        content = Regex.Replace(content, @"<br\s*/?>", "\\line ", RegexOptions.IgnoreCase);

        // Paragraphs
        content = Regex.Replace(content, @"<p[^>]*>", "", RegexOptions.IgnoreCase);
        content = Regex.Replace(content, @"</p>", "\\par ", RegexOptions.IgnoreCase);

        // Remaining spans
        content = Regex.Replace(content, @"<span[^>]*>([^<]*)</span>", "$1", RegexOptions.IgnoreCase);

        // Remove remaining HTML tags
        content = Regex.Replace(content, @"<[^>]+>", "");

        // Decode entities
        content = content.Replace("&amp;", "&");
        content = content.Replace("&lt;", "<");
        content = content.Replace("&gt;", ">");
        content = content.Replace("&quot;", "\"");

        // Encode Turkish characters
        content = EncodeTurkishForRtf(content);

        rtf += content + "}";
        return rtf;
    }

    private static string EncodeTurkishForRtf(string text)
    {
        var replacements = new Dictionary<string, string>
        {
            ["ç"] = "\\'e7", ["Ç"] = "\\'c7",
            ["ğ"] = "\\'f0", ["Ğ"] = "\\'d0",
            ["ı"] = "\\'fd", ["İ"] = "\\'dd",
            ["ö"] = "\\'f6", ["Ö"] = "\\'d6",
            ["ş"] = "\\'fe", ["Ş"] = "\\'de",
            ["ü"] = "\\'fc", ["Ü"] = "\\'dc",
            ["\u00F7"] = "\\'f7",     // ÷ Division sign
            ["\u2192"] = "\\u8594?"   // → Arrow
        };

        foreach (var (ch, code) in replacements)
            text = text.Replace(ch, code);

        return text;
    }
}
