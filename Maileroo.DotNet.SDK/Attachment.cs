namespace Maileroo.DotNet.SDK;

public sealed class Attachment
{
    private string FileName { get; }
    private string ContentType { get; }
    private string ContentBase64 { get; }
    private bool Inline { get; }

    private Attachment(string fileName, string base64Content, string? contentType, bool inline)
    {
        if (string.IsNullOrEmpty(fileName))
            throw new ArgumentException("file_name is required.", nameof(fileName));
        if (string.IsNullOrEmpty(base64Content))
            throw new ArgumentException("content must be a non-empty base64 string.", nameof(base64Content));

        FileName = fileName;
        ContentBase64 = base64Content;
        ContentType = string.IsNullOrWhiteSpace(contentType) ? "application/octet-stream" : contentType!;
        Inline = inline;
    }

    public static Attachment FromContent(string fileName, string content, string? contentType = null, bool inline = false, bool isBase64 = false)
    {
        if (content is null) throw new ArgumentNullException(nameof(content));

        byte[] binary;
        if (isBase64)
        {
            try { binary = Convert.FromBase64String(content); }
            catch { throw new ArgumentException("Invalid base64 content provided.", nameof(content)); }
        }
        else
        {
            binary = System.Text.Encoding.UTF8.GetBytes(content);
        }

        var detected = contentType ?? "application/octet-stream";
        var b64 = Convert.ToBase64String(binary);
        return new Attachment(fileName, b64, detected, inline);
    }

    public static Attachment FromFile(string path, string? contentType = null, bool inline = false)
    {
        if (string.IsNullOrWhiteSpace(path) || !File.Exists(path))
            throw new ArgumentException("path must be a readable file.", nameof(path));

        var fileName = Path.GetFileName(path);
        var bytes = File.ReadAllBytes(path);

        var detected = contentType ?? DetectMimeFromPath(path);
        var b64 = Convert.ToBase64String(bytes);
        return new Attachment(fileName, b64, detected, inline);
    }

    internal Dictionary<string, object?> ToApi() => new()
    {
        ["file_name"] = FileName,
        ["content_type"] = string.IsNullOrWhiteSpace(ContentType) ? "application/octet-stream" : ContentType,
        ["content"] = ContentBase64,
        ["inline"] = Inline
    };
        
    private static string DetectMimeFromPath(string path)
    {
        var ext = Path.GetExtension(path)?.TrimStart('.').ToLowerInvariant();
        return string.IsNullOrEmpty(ext) ? "application/octet-stream" : ExtensionToMime.GetValueOrDefault(ext, "application/octet-stream");
    }

    private static readonly Dictionary<string, string> ExtensionToMime = new(StringComparer.OrdinalIgnoreCase)
    {
        ["png"] = "image/png",
        ["jpg"] = "image/jpeg",
        ["jpeg"] = "image/jpeg",
        ["gif"] = "image/gif",
        ["bmp"] = "image/bmp",
        ["webp"] = "image/webp",
        ["svg"] = "image/svg+xml",
        ["tiff"] = "image/tiff",
        ["ico"] = "image/x-icon",
        ["pdf"] = "application/pdf",
        ["doc"] = "application/msword",
        ["docx"] = "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
        ["xls"] = "application/vnd.ms-excel",
        ["xlsx"] = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
        ["ppt"] = "application/vnd.ms-powerpoint",
        ["pptx"] = "application/vnd.openxmlformats-officedocument.presentationml.presentation",
        ["odt"] = "application/vnd.oasis.opendocument.text",
        ["ods"] = "application/vnd.oasis.opendocument.spreadsheet",
        ["odp"] = "application/vnd.oasis.opendocument.presentation",
        ["rtf"] = "application/rtf",
        ["txt"] = "text/plain",
        ["csv"] = "text/csv",
        ["tsv"] = "text/tab-separated-values",
        ["json"] = "application/json",
        ["xml"] = "application/xml",
        ["html"] = "text/html",
        ["htm"] = "text/html",
        ["md"] = "text/markdown",
        ["zip"] = "application/zip",
        ["tar"] = "application/x-tar",
        ["gz"] = "application/gzip",
        ["tgz"] = "application/gzip",
        ["rar"] = "application/vnd.rar",
        ["7z"] = "application/x-7z-compressed",
        ["mp3"] = "audio/mpeg",
        ["wav"] = "audio/wav",
        ["ogg"] = "audio/ogg",
        ["m4a"] = "audio/mp4",
        ["flac"] = "audio/flac",
        ["aac"] = "audio/aac",
        ["mp4"] = "video/mp4",
        ["webm"] = "video/webm",
        ["mov"] = "video/quicktime",
        ["avi"] = "video/x-msvideo",
        ["mkv"] = "video/x-matroska",
        ["flv"] = "video/x-flv",
        ["wmv"] = "video/x-ms-wmv",
        ["m4v"] = "video/x-m4v",
        ["woff"] = "font/woff",
        ["woff2"] = "font/woff2",
        ["ttf"] = "font/ttf",
        ["otf"] = "font/otf",
        ["eot"] = "application/vnd.ms-fontobject",
    };
}