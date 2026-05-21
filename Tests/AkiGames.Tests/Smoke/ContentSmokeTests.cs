using System.Text.Json;
using AkiGames.Core;
using AkiGames.Core.Serialization;
using AkiGames.Tests.Support;

namespace AkiGames.Tests.Smoke;

internal static class ContentSmokeTests
{
    public static void ProjectContentRootsHaveStartupScenes()
    {
        Assert.True(RepoPaths.ContentRoots.Count > 0, "Expected at least one Content.mgcb-backed content root.");

        List<string> failures = [];
        foreach (string contentRoot in RepoPaths.ContentRoots)
        {
            string mainScene = Path.Combine(contentRoot, "main.aki");
            if (!File.Exists(mainScene))
            {
                failures.Add($"{RepoPaths.RelativeToRoot(contentRoot)} is missing main.aki");
                continue;
            }

            try
            {
                ConfigureContentRoot(contentRoot);
                GameObject mainObject = LoadAki(mainScene);
                if (string.IsNullOrWhiteSpace(mainObject.ObjectName))
                    failures.Add($"{RepoPaths.RelativeToRoot(mainScene)} deserialized with an empty root name");
            }
            catch (Exception ex)
            {
                failures.Add($"{RepoPaths.RelativeToRoot(mainScene)} failed to deserialize: {ex.Message}");
            }
        }

        AssertNoFailures(failures, "Startup scene smoke failures");
    }

    public static void AllAkiFilesDeserialize()
    {
        List<string> failures = [];

        foreach (string contentRoot in RepoPaths.ContentRoots)
        {
            ConfigureContentRoot(contentRoot);
            foreach (string akiFile in EnumerateAkiFiles(contentRoot))
            {
                try
                {
                    GameObject gameObject = LoadAki(akiFile);
                    if (gameObject == null)
                        failures.Add($"{RepoPaths.RelativeToRoot(akiFile)} deserialized to null");
                }
                catch (Exception ex)
                {
                    failures.Add($"{RepoPaths.RelativeToRoot(akiFile)} failed to deserialize: {ex.Message}");
                }
            }
        }

        AssertNoFailures(failures, ".aki deserialization smoke failures");
    }

    public static void PrefabLinksResolveToExistingFiles()
    {
        List<string> failures = [];

        foreach (string contentRoot in RepoPaths.ContentRoots)
        {
            foreach (string akiFile in EnumerateAkiFiles(contentRoot))
            {
                JsonElement root = ReadJson(akiFile);
                foreach (string link in EnumerateLinks(root))
                {
                    string resolvedPath = ResolvePrefabLink(contentRoot, link);
                    if (string.IsNullOrWhiteSpace(resolvedPath) || !File.Exists(resolvedPath))
                    {
                        failures.Add(
                            $"{RepoPaths.RelativeToRoot(akiFile)} links to missing prefab '{link}'"
                        );
                    }
                }
            }
        }

        AssertNoFailures(failures, "Prefab link smoke failures");
    }

    public static void AkiFilesAreRegisteredInMgcb()
    {
        List<string> failures = [];

        foreach (string contentRoot in RepoPaths.ContentRoots)
        {
            string mgcbPath = Path.Combine(contentRoot, "Content.mgcb");
            HashSet<string> registeredPaths = File
                .ReadLines(mgcbPath)
                .Select(line => line.Trim())
                .Where(line => line.StartsWith("#begin ", StringComparison.OrdinalIgnoreCase))
                .Select(line => line["#begin ".Length..].Replace('\\', '/'))
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            foreach (string akiFile in EnumerateAkiFiles(contentRoot))
            {
                string relativePath = RepoPaths.RelativeTo(contentRoot, akiFile);
                if (!registeredPaths.Contains(relativePath))
                {
                    failures.Add(
                        $"{RepoPaths.RelativeToRoot(mgcbPath)} is missing #begin {relativePath}"
                    );
                }
            }
        }

        AssertNoFailures(failures, "MGCB smoke failures");
    }

    private static void ConfigureContentRoot(string contentRoot)
    {
        JsonProjectSerializer.ClearTypeCache();
        Game1.Prefabs.Clear();
        Game1.GameContentRoot = contentRoot;
        Game1.EditorContentRoot = null;
        Game1.GameContent = null;
        Game1.EditorContent = null;
    }

    private static GameObject LoadAki(string path)
    {
        JsonElement root = ReadJson(path);
        return JsonProjectSerializer.LoadFromJson(root);
    }

    private static JsonElement ReadJson(string path) =>
        JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(path));

    private static IEnumerable<string> EnumerateAkiFiles(string contentRoot) =>
        Directory
            .EnumerateFiles(contentRoot, "*.aki", SearchOption.AllDirectories)
            .Where(path => !path.Replace('\\', '/').Contains("/Content/bin/", StringComparison.OrdinalIgnoreCase))
            .Where(path => !path.Replace('\\', '/').Contains("/Content/obj/", StringComparison.OrdinalIgnoreCase))
            .OrderBy(path => path, StringComparer.OrdinalIgnoreCase);

    private static IEnumerable<string> EnumerateLinks(JsonElement element)
    {
        switch (element.ValueKind)
        {
            case JsonValueKind.Object:
                foreach (JsonProperty property in element.EnumerateObject())
                {
                    if (
                        property.NameEquals("Link") &&
                        property.Value.ValueKind == JsonValueKind.String
                    )
                    {
                        string? link = property.Value.GetString();
                        if (!string.IsNullOrWhiteSpace(link))
                            yield return link;
                    }

                    foreach (string nestedLink in EnumerateLinks(property.Value))
                        yield return nestedLink;
                }

                break;

            case JsonValueKind.Array:
                foreach (JsonElement item in element.EnumerateArray())
                {
                    foreach (string nestedLink in EnumerateLinks(item))
                        yield return nestedLink;
                }

                break;
        }
    }

    private static string ResolvePrefabLink(string contentRoot, string link)
    {
        if (string.IsNullOrWhiteSpace(link)) return "";

        string normalizedPath = link.Trim().Replace('\\', '/');
        if (Path.IsPathRooted(normalizedPath))
        {
            int contentIndex = normalizedPath.LastIndexOf("/Content/", StringComparison.OrdinalIgnoreCase);
            if (contentIndex < 0) return "";

            normalizedPath = normalizedPath[(contentIndex + "/Content/".Length)..];
        }

        if (!normalizedPath.Contains('/'))
            normalizedPath = $"Prefabs/{normalizedPath}";

        if (normalizedPath.StartsWith("Content/", StringComparison.OrdinalIgnoreCase))
            normalizedPath = normalizedPath["Content/".Length..];

        if (string.IsNullOrWhiteSpace(Path.GetExtension(normalizedPath)))
            normalizedPath += ".aki";

        return Path.Combine(
            contentRoot,
            normalizedPath.Replace('/', Path.DirectorySeparatorChar)
        );
    }

    private static void AssertNoFailures(List<string> failures, string title)
    {
        if (failures.Count == 0) return;

        throw new InvalidOperationException(
            $"{title}:{Environment.NewLine}{string.Join(Environment.NewLine, failures)}"
        );
    }
}
