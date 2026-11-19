using DocumentSubmission.Domain.Exceptions;
using DocumentSubmission.Domain.Submissions;

namespace DocumentSubmission.Api.Services;

/// <summary>
/// Validates an uploaded file (R16) and turns it into a <see cref="DocumentFile"/>. The content type is
/// decided here from the extension and the file signature; the client's Content-Type is ignored.
/// </summary>
public static class DocumentUploadValidator
{
    public const long MaxSizeBytes = 10 * 1024 * 1024;
    public const int MaxFileNameLength = 255;

    private sealed record AllowedType(string Extension, string ContentType, byte[] Signature);

    private static readonly AllowedType[] AllowedTypes =
    [
        new(".pdf", "application/pdf", "%PDF-"u8.ToArray()),
        // A DOCX file is a ZIP container; we only check the ZIP signature ("PK\x03\x04").
        new(".docx", "application/vnd.openxmlformats-officedocument.wordprocessingml.document", [0x50, 0x4B, 0x03, 0x04]),
    ];

    public static async Task<DocumentFile> ReadAsync(IFormFile file, CancellationToken cancellationToken)
    {
        var fileName = Path.GetFileName(file.FileName ?? string.Empty).Trim();
        if (fileName.Length == 0 || fileName.Length > MaxFileNameLength)
            throw new InvalidInputException($"The file name is required and must be at most {MaxFileNameLength} characters.");
        if (file.Length == 0)
            throw new InvalidInputException("The file is empty.");
        if (file.Length > MaxSizeBytes)
            throw new InvalidInputException("The file must be at most 10 MB.");

        var type = AllowedTypes.SingleOrDefault(t => string.Equals(Path.GetExtension(fileName), t.Extension, StringComparison.OrdinalIgnoreCase))
            ?? throw new InvalidInputException("Only PDF (.pdf) and Word (.docx) files are allowed.");

        using var buffer = new MemoryStream((int)file.Length);
        await file.CopyToAsync(buffer, cancellationToken);
        var content = buffer.ToArray();

        if (!content.AsSpan().StartsWith(type.Signature))
            throw new InvalidInputException($"The file content does not match the {type.Extension} extension.");

        return new DocumentFile(fileName, type.ContentType, content);
    }
}
