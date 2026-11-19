using System.Text;

namespace DocumentSubmission.Api.IntegrationTests.Infrastructure;

/// <summary>Minimal files that pass the signature check: PDF starts with "%PDF-", DOCX (ZIP) with "PK\x03\x04".</summary>
public static class TestFiles
{
    public const string PdfContentType = "application/pdf";
    public const string DocxContentType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";

    public static byte[] Pdf(string text = "test document") => Encoding.ASCII.GetBytes($"%PDF-1.7\n% {text}\n%%EOF\n");

    public static byte[] Docx() => [0x50, 0x4B, 0x03, 0x04, 0x14, 0x00, 0x06, 0x00, 0x08, 0x00];

    public static byte[] PdfOfSize(int size)
    {
        var bytes = new byte[size];
        "%PDF-"u8.CopyTo(bytes);
        return bytes;
    }
}
