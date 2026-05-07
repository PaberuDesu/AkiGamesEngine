using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace AkiGames.Scripts.Explorer
{
    public static class ContentMgcbRegistry
    {
        private const string MgcbFileName = "Content.mgcb";

        public static void RegisterFile(string contentRoot, string filePath)
        {
            string relativePath = ToMgcbPath(contentRoot, filePath);
            string mgcbPath = GetMgcbPath(contentRoot);
            List<string> lines = ReadLines(mgcbPath);

            if (ContainsBegin(lines, relativePath))
                return;

            List<string> block = GetBuildBlock(relativePath, filePath);
            if (block.Count == 0)
                return;

            if (lines.Count > 0 && !string.IsNullOrWhiteSpace(lines[^1]))
                lines.Add("");

            lines.AddRange(block);

            File.WriteAllLines(mgcbPath, lines);
        }

        public static void RegisterFolder(string contentRoot, string folderPath)
        {
            if (!Directory.Exists(folderPath)) return;

            foreach (string filePath in Directory.EnumerateFiles(folderPath, "*", SearchOption.AllDirectories))
            {
                RegisterFile(contentRoot, filePath);
            }
        }

        public static void RemoveFile(string contentRoot, string filePath)
        {
            string relativePath = ToMgcbPath(contentRoot, filePath);
            string mgcbPath = GetMgcbPath(contentRoot);
            if (!File.Exists(mgcbPath)) return;

            List<string> lines = ReadLines(mgcbPath);
            int beginIndex = FindBeginIndex(lines, relativePath);
            if (beginIndex < 0) return;

            int endIndex = beginIndex + 1;
            while (endIndex < lines.Count && !lines[endIndex].TrimStart().StartsWith("#begin ", StringComparison.OrdinalIgnoreCase))
            {
                endIndex++;
            }

            lines.RemoveRange(beginIndex, endIndex - beginIndex);
            File.WriteAllLines(mgcbPath, lines);
        }

        public static void RemoveFolder(string contentRoot, string folderPath)
        {
            string relativeFolder = ToMgcbPath(contentRoot, folderPath).TrimEnd('/') + "/";
            string mgcbPath = GetMgcbPath(contentRoot);
            if (!File.Exists(mgcbPath)) return;

            List<string> lines = ReadLines(mgcbPath);
            List<string> filteredLines = [];

            for (int i = 0; i < lines.Count;)
            {
                if (IsBeginUnderFolder(lines[i], relativeFolder))
                {
                    i++;
                    while (i < lines.Count && !IsBeginLine(lines[i]))
                    {
                        i++;
                    }
                    continue;
                }

                filteredLines.Add(lines[i]);
                i++;
            }

            File.WriteAllLines(mgcbPath, filteredLines);
        }

        public static void RenameFile(string contentRoot, string oldFilePath, string newFilePath)
        {
            string oldRelativePath = ToMgcbPath(contentRoot, oldFilePath);
            string newRelativePath = ToMgcbPath(contentRoot, newFilePath);
            ReplaceExactReference(contentRoot, oldRelativePath, newRelativePath);
        }

        public static void RenameFolder(string contentRoot, string oldFolderPath, string newFolderPath)
        {
            string oldRelativePath = ToMgcbPath(contentRoot, oldFolderPath).TrimEnd('/') + "/";
            string newRelativePath = ToMgcbPath(contentRoot, newFolderPath).TrimEnd('/') + "/";
            string mgcbPath = GetMgcbPath(contentRoot);
            if (!File.Exists(mgcbPath)) return;

            List<string> lines = ReadLines(mgcbPath);
            for (int i = 0; i < lines.Count; i++)
            {
                lines[i] = ReplaceReferencePrefix(lines[i], "#begin ", oldRelativePath, newRelativePath);
                lines[i] = ReplaceReferencePrefix(lines[i], "/build:", oldRelativePath, newRelativePath);
            }

            File.WriteAllLines(mgcbPath, lines);
        }

        private static void ReplaceExactReference(string contentRoot, string oldRelativePath, string newRelativePath)
        {
            string mgcbPath = GetMgcbPath(contentRoot);
            if (!File.Exists(mgcbPath)) return;

            List<string> lines = ReadLines(mgcbPath);
            for (int i = 0; i < lines.Count; i++)
            {
                string trimmed = lines[i].Trim();
                if (string.Equals(trimmed, $"#begin {oldRelativePath}", StringComparison.OrdinalIgnoreCase))
                    lines[i] = $"#begin {newRelativePath}";
                else if (string.Equals(trimmed, $"/build:{oldRelativePath}", StringComparison.OrdinalIgnoreCase))
                    lines[i] = $"/build:{newRelativePath}";
            }

            File.WriteAllLines(mgcbPath, lines);
        }

        private static string ReplaceReferencePrefix(
            string line,
            string directive,
            string oldRelativeFolder,
            string newRelativeFolder
        )
        {
            string trimmed = line.Trim();
            if (!trimmed.StartsWith(directive, StringComparison.OrdinalIgnoreCase))
                return line;

            string path = trimmed[directive.Length..];
            if (!path.StartsWith(oldRelativeFolder, StringComparison.OrdinalIgnoreCase))
                return line;

            return directive + newRelativeFolder + path[oldRelativeFolder.Length..];
        }

        private static bool ContainsBegin(List<string> lines, string relativePath) =>
            FindBeginIndex(lines, relativePath) >= 0;

        private static bool IsBeginLine(string line) =>
            line.TrimStart().StartsWith("#begin ", StringComparison.OrdinalIgnoreCase);

        private static bool IsBeginUnderFolder(string line, string relativeFolder)
        {
            string trimmed = line.Trim();
            if (!trimmed.StartsWith("#begin ", StringComparison.OrdinalIgnoreCase))
                return false;

            string path = trimmed["#begin ".Length..];
            return path.StartsWith(relativeFolder, StringComparison.OrdinalIgnoreCase);
        }

        private static int FindBeginIndex(List<string> lines, string relativePath) =>
            lines.FindIndex(line =>
                string.Equals(line.Trim(), $"#begin {relativePath}", StringComparison.OrdinalIgnoreCase)
            );

        private static List<string> ReadLines(string mgcbPath) =>
            File.Exists(mgcbPath) ? File.ReadAllLines(mgcbPath).ToList() : [];

        private static List<string> GetBuildBlock(string relativePath, string filePath)
        {
            if (string.Equals(Path.GetExtension(filePath), ".aki", StringComparison.OrdinalIgnoreCase))
            {
                return
                [
                    $"#begin {relativePath}",
                    "/importer:AkiImporter",
                    "/processor:AkiProcessor",
                    $"/build:{relativePath}"
                ];
            }

            if (ContentFileUtility.IsImageFile(filePath))
            {
                return
                [
                    $"#begin {relativePath}",
                    "/importer:TextureImporter",
                    "/processor:TextureProcessor",
                    "/processorParam:ColorKeyColor=255,0,255,255",
                    "/processorParam:ColorKeyEnabled=True",
                    "/processorParam:GenerateMipmaps=False",
                    "/processorParam:PremultiplyAlpha=True",
                    "/processorParam:ResizeToPowerOfTwo=False",
                    "/processorParam:MakeSquare=False",
                    "/processorParam:TextureFormat=Color",
                    $"/build:{relativePath}"
                ];
            }

            return [];
        }

        private static string GetMgcbPath(string contentRoot) =>
            Path.Combine(contentRoot, MgcbFileName);

        private static string ToMgcbPath(string contentRoot, string filePath) =>
            Path.GetRelativePath(contentRoot, filePath).Replace('\\', '/');
    }
}
