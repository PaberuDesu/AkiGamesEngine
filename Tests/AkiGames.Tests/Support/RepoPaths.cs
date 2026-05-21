namespace AkiGames.Tests.Support;

internal static class RepoPaths
{
    public static string Root { get; } = FindRepoRoot();

    public static IReadOnlyList<string> ContentRoots { get; } = Directory
        .EnumerateFiles(Root, "Content.mgcb", SearchOption.AllDirectories)
        .Where(path => !IsGeneratedPath(path))
        .Select(path => Path.GetDirectoryName(path)!)
        .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
        .ToArray();

    public static string RelativeToRoot(string path) =>
        Path.GetRelativePath(Root, path).Replace('\\', '/');

    public static string RelativeTo(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');

    private static string FindRepoRoot()
    {
        DirectoryInfo? directory = new(AppContext.BaseDirectory);
        while (directory != null)
        {
            if (
                File.Exists(Path.Combine(directory.FullName, "AGENTS.md")) &&
                Directory.Exists(Path.Combine(directory.FullName, "AkiGames"))
            )
            {
                return directory.FullName;
            }

            directory = directory.Parent;
        }

        throw new DirectoryNotFoundException("Could not locate the AkiGamesEngine repository root.");
    }

    private static bool IsGeneratedPath(string path)
    {
        string normalized = path.Replace('\\', '/');
        return normalized.Contains("/bin/", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("/obj/", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("/Content/bin/", StringComparison.OrdinalIgnoreCase) ||
               normalized.Contains("/Content/obj/", StringComparison.OrdinalIgnoreCase);
    }
}
