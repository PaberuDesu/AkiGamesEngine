using System;
using System.Collections.Generic;
using System.IO;

namespace AkiGames.Scripts
{
    public static class ContentFileUtility
    {
        private static readonly HashSet<string> ImageExtensions = new(StringComparer.OrdinalIgnoreCase)
        {
            ".png",
            ".jpg",
            ".jpeg",
            ".bmp",
            ".gif"
        };

        public static bool IsImageFile(string filePath) =>
            !string.IsNullOrWhiteSpace(filePath) &&
            ImageExtensions.Contains(Path.GetExtension(filePath));

        public static string GetDisplayName(string filePath) =>
            string.IsNullOrWhiteSpace(filePath) ? "" : Path.GetFileNameWithoutExtension(filePath);
    }
}
