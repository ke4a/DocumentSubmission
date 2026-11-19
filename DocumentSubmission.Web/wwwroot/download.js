// Saves bytes downloaded by ApiClient as a file. The API needs a bearer token,
// so the browser cannot simply follow a link to it.
window.downloadFile = (fileName, bytes) => {
    const url = URL.createObjectURL(new Blob([bytes]));
    const link = document.createElement("a");
    link.href = url;
    link.download = fileName;
    link.click();
    URL.revokeObjectURL(url);
};
