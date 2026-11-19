namespace DocumentSubmission.Domain.Submissions;

/// <summary>
/// An uploaded file as the domain sees it. The API validates the upload and detects the
/// content type, so the domain never depends on ASP.NET Core types such as IFormFile.
/// </summary>
public sealed record DocumentFile(string FileName, string ContentType, byte[] Content);
