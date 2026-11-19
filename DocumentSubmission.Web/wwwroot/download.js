// Saves a file that .NET sends as a DotNetStreamReference
// (https://learn.microsoft.com/aspnet/core/blazor/file-downloads).
// Needed because the API requires a bearer token, so the browser cannot link to it directly.
window.downloadFile = async (fileName, streamReference) => {
    const blob = new Blob([await streamReference.arrayBuffer()]);
    const url = URL.createObjectURL(blob);
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
};
