namespace AkiGames.Tests.Support;

internal sealed class TemporaryDirectory : IDisposable
{
    public TemporaryDirectory(string name)
    {
        Path = System.IO.Path.Combine(
            AppContext.BaseDirectory,
            ".tmp",
            $"{name}-{Guid.NewGuid():N}"
        );
        Directory.CreateDirectory(Path);
    }

    public string Path { get; }

    public void Dispose()
    {
        if (!Directory.Exists(Path)) return;
        Directory.Delete(Path, recursive: true);
    }
}
