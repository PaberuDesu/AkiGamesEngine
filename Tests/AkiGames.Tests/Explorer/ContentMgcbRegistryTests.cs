using AkiGames.Scripts;
using AkiGames.Scripts.Explorer;
using AkiGames.Tests.Support;

namespace AkiGames.Tests.Explorer;

internal static class ContentMgcbRegistryTests
{
    public static void RegisterAndRemoveFiles()
    {
        using TemporaryDirectory temp = new("mgcb-register");
        string contentRoot = System.IO.Path.Combine(temp.Path, "Content");
        Directory.CreateDirectory(contentRoot);

        string scenePath = System.IO.Path.Combine(contentRoot, "Scenes", "Main.aki");
        string imagePath = System.IO.Path.Combine(contentRoot, "Textures", "Hero.png");
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(scenePath)!);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(imagePath)!);
        File.WriteAllText(scenePath, "{}");
        File.WriteAllText(imagePath, "");

        ContentMgcbRegistry.RegisterFile(contentRoot, scenePath);
        ContentMgcbRegistry.RegisterFile(contentRoot, scenePath);
        ContentMgcbRegistry.RegisterFile(contentRoot, imagePath);

        string mgcb = File.ReadAllText(System.IO.Path.Combine(contentRoot, "Content.mgcb"));

        Assert.Equal(1, CountOccurrences(mgcb, "#begin Scenes/Main.aki"));
        Assert.Contains("/importer:AkiImporter", mgcb);
        Assert.Contains("/processor:AkiProcessor", mgcb);
        Assert.Contains("#begin Textures/Hero.png", mgcb);
        Assert.Contains("/importer:TextureImporter", mgcb);

        ContentMgcbRegistry.RemoveFile(contentRoot, scenePath);
        mgcb = File.ReadAllText(System.IO.Path.Combine(contentRoot, "Content.mgcb"));

        Assert.DoesNotContain("Scenes/Main.aki", mgcb);
        Assert.Contains("Textures/Hero.png", mgcb);
    }

    public static void RenameFolderUpdatesReferences()
    {
        using TemporaryDirectory temp = new("mgcb-rename");
        string contentRoot = System.IO.Path.Combine(temp.Path, "Content");
        string oldFolder = System.IO.Path.Combine(contentRoot, "Old");
        string newFolder = System.IO.Path.Combine(contentRoot, "New");
        Directory.CreateDirectory(oldFolder);
        Directory.CreateDirectory(newFolder);

        string filePath = System.IO.Path.Combine(oldFolder, "Thing.aki");
        File.WriteAllText(filePath, "{}");
        ContentMgcbRegistry.RegisterFile(contentRoot, filePath);

        ContentMgcbRegistry.RenameFolder(contentRoot, oldFolder, newFolder);

        string mgcb = File.ReadAllText(System.IO.Path.Combine(contentRoot, "Content.mgcb"));
        Assert.Contains("#begin New/Thing.aki", mgcb);
        Assert.Contains("/build:New/Thing.aki", mgcb);
        Assert.DoesNotContain("Old/Thing.aki", mgcb);

        ContentMgcbRegistry.RemoveFolder(contentRoot, newFolder);
        mgcb = File.ReadAllText(System.IO.Path.Combine(contentRoot, "Content.mgcb"));
        Assert.DoesNotContain("New/Thing.aki", mgcb);
    }

    public static void ContentFileUtilityRecognizesImages()
    {
        Assert.True(ContentFileUtility.IsImageFile("hero.PNG"));
        Assert.True(ContentFileUtility.IsImageFile("portrait.jpeg"));
        Assert.False(ContentFileUtility.IsImageFile("scene.aki"));
        Assert.Equal("hero", ContentFileUtility.GetDisplayName("Content/Textures/hero.png"));
    }

    private static int CountOccurrences(string haystack, string needle)
    {
        int count = 0;
        int index = 0;
        while ((index = haystack.IndexOf(needle, index, StringComparison.Ordinal)) >= 0)
        {
            count++;
            index += needle.Length;
        }

        return count;
    }
}
