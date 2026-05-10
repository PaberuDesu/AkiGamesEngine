using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows.Forms;
using AkiGames.Core;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI.DropDown;

namespace AkiGames.Scripts.Menu
{
    public class ProjectCreator : DropDownItem
    {
        private static readonly HashSet<string> SkippedDirectoryNames = new(StringComparer.OrdinalIgnoreCase)
        {
            "bin",
            "obj"
        };

        private ExplorerWindowController _explorerWindowController;

        public override void Awake()
        {
            _explorerWindowController = GameObject.FindById(45)
                .GetComponent<ExplorerWindowController>();
        }

        public override void OnMouseUp()
        {
            OpenCreateProjectDialog();
            base.OnMouseUp();
        }

        private void OpenCreateProjectDialog()
        {
            try
            {
                using SaveFileDialog dialog = new()
                {
                    Title = "Choose new project folder and name",
                    FileName = "NewProject",
                    Filter = "Project name (*.*)|*.*",
                    AddExtension = false,
                    CheckFileExists = false,
                    CheckPathExists = true,
                    OverwritePrompt = false,
                    ValidateNames = true
                };

                Control form = Control.FromHandle(Game1.WindowHandle);
                if (dialog.ShowDialog(form) != DialogResult.OK) return;

                string parentPath = Path.GetDirectoryName(dialog.FileName);
                string projectName = Path.GetFileName(dialog.FileName);

                if (string.IsNullOrWhiteSpace(parentPath) || string.IsNullOrWhiteSpace(projectName))
                {
                    ConsoleWindowController.Log("Project creation failed: project path is empty.");
                    return;
                }

                string projectPath = Path.Combine(parentPath, projectName);
                CreateProject(projectPath, projectName);
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log($"Project creation failed: {ex.Message}");
            }
        }

        private void CreateProject(string projectPath, string projectName)
        {
            string templateRoot = FindTemplateRoot();
            if (string.IsNullOrWhiteSpace(templateRoot))
            {
                ConsoleWindowController.Log("Project creation failed: template folder wasn't found.");
                return;
            }

            if (Directory.Exists(projectPath) && Directory.EnumerateFileSystemEntries(projectPath).Any())
            {
                ConsoleWindowController.Log($"Project creation failed: folder is not empty: {projectPath}");
                return;
            }

            Directory.CreateDirectory(projectPath);
            CopyTemplateDirectory(templateRoot, projectPath);
            RenameProjectFiles(projectPath, projectName);

            _explorerWindowController?.SetProjectPath(projectPath);
            ConsoleWindowController.Log($"Project created: {projectPath}");
        }

        private static string FindTemplateRoot()
        {
            foreach (string startPath in GetTemplateSearchStartPaths())
            {
                if (string.IsNullOrWhiteSpace(startPath)) continue;

                DirectoryInfo directory = new(startPath);
                while (directory != null)
                {
                    string templatePath = Path.Combine(directory.FullName, "Template");
                    if (IsTemplateRoot(templatePath))
                        return templatePath;

                    templatePath = Path.Combine(directory.FullName, "AkiGames", "Template");
                    if (IsTemplateRoot(templatePath))
                        return templatePath;

                    directory = directory.Parent;
                }
            }

            return null;
        }

        private static IEnumerable<string> GetTemplateSearchStartPaths()
        {
            yield return AppContext.BaseDirectory;
            yield return Directory.GetCurrentDirectory();
            yield return Game1.EditorContentRoot;
        }

        private static bool IsTemplateRoot(string path)
        {
            return Directory.Exists(path) &&
                File.Exists(Path.Combine(path, "AkiGames.csproj")) &&
                File.Exists(Path.Combine(path, "Content", "main.aki"));
        }

        private static void CopyTemplateDirectory(string sourceDirectory, string destinationDirectory)
        {
            Directory.CreateDirectory(destinationDirectory);

            foreach (string filePath in Directory.EnumerateFiles(sourceDirectory))
            {
                string destinationFile = Path.Combine(destinationDirectory, Path.GetFileName(filePath));
                File.Copy(filePath, destinationFile, overwrite: false);
            }

            foreach (string childDirectory in Directory.EnumerateDirectories(sourceDirectory))
            {
                string directoryName = Path.GetFileName(childDirectory);
                if (SkippedDirectoryNames.Contains(directoryName)) continue;

                CopyTemplateDirectory(childDirectory, Path.Combine(destinationDirectory, directoryName));
            }
        }

        private static void RenameProjectFiles(string projectPath, string projectName)
        {
            string projectFileName = $"{projectName}.csproj";
            string solutionFileName = $"{projectName}.sln";

            RenameFile(projectPath, "AkiGames.csproj", projectFileName);
            RenameFile(projectPath, "AkiGames.sln", solutionFileName);

            ReplaceInFile(Path.Combine(projectPath, solutionFileName), "AkiGames.csproj", projectFileName);
            ReplaceInFile(Path.Combine(projectPath, solutionFileName), "\"AkiGames\"", $"\"{projectName}\"");
            ReplaceInFile(Path.Combine(projectPath, ".vscode", "launch.json"), "AkiGames.csproj", projectFileName);
            ReplaceInFile(Path.Combine(projectPath, ".vscode", "launch.json"), "C#: AkiGames Debug", $"C#: {projectName} Debug");
            ReplaceInFile(
                Path.Combine(projectPath, "Core", "Game1.cs"),
                "Window.Title = \"Template\";",
                $"Window.Title = \"{EscapeCSharpString(projectName)}\";"
            );
            ReplaceInFile(
                Path.Combine(projectPath, "app.manifest"),
                "name=\"AkiGames\"",
                $"name=\"{EscapeXmlAttribute(projectName)}\""
            );
        }

        private static void RenameFile(string directory, string oldName, string newName)
        {
            if (string.Equals(oldName, newName, StringComparison.OrdinalIgnoreCase)) return;

            string oldPath = Path.Combine(directory, oldName);
            if (!File.Exists(oldPath)) return;

            File.Move(oldPath, Path.Combine(directory, newName));
        }

        private static void ReplaceInFile(string path, string oldValue, string newValue)
        {
            if (!File.Exists(path)) return;

            string text = File.ReadAllText(path);
            File.WriteAllText(path, text.Replace(oldValue, newValue));
        }

        private static string EscapeXmlAttribute(string value)
        {
            return value
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;");
        }

        private static string EscapeCSharpString(string value)
        {
            return value
                .Replace("\\", "\\\\")
                .Replace("\"", "\\\"");
        }
    }
}
