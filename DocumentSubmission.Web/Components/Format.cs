namespace DocumentSubmission.Web.Components;

public static class Format
{
    // The API returns UTC times.
    public static string Time(DateTime utc) => $"{utc:yyyy-MM-dd HH:mm} UTC";

    public static string Size(long bytes) => bytes switch
    {
        < 1024 => $"{bytes} B",
        < 1024 * 1024 => $"{bytes / 1024.0:0.#} KB",
        _ => $"{bytes / (1024.0 * 1024):0.#} MB",
    };
}
