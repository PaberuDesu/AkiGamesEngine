using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using AkiGames.Core;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI.DropDown;

namespace AkiGames.Scripts.Menu
{
    public class BuildButton : DropDownItem
    {
        private static bool _isBuilding;

        public override void OnMouseUp()
        {
            StartBuild();
            base.OnMouseUp();
        }

        private static void StartBuild()
        {
            if (_isBuilding)
            {
                ConsoleWindowController.Log("Build is already running.");
                return;
            }

            string projectRoot = GetOpenedProjectRoot();
            if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
            {
                ConsoleWindowController.Log("Build failed: no project is opened.");
                return;
            }

            string buildTarget = FindBuildTarget(projectRoot);
            if (string.IsNullOrWhiteSpace(buildTarget))
            {
                ConsoleWindowController.Log($"Build failed: no .sln or .csproj found in {projectRoot}.");
                return;
            }

            HierarchyWindowController.SaveHierarchy();
            _isBuilding = true;
            ConsoleWindowController.Log($"Release build started: {Path.GetFileName(buildTarget)}");

            Task.Run(() => RunBuild(projectRoot, buildTarget));
        }

        private static string GetOpenedProjectRoot()
        {
            string contentRoot = Game1.GameContentRoot;
            if (string.IsNullOrWhiteSpace(contentRoot)) return null;

            DirectoryInfo directory = new(contentRoot);
            return string.Equals(directory.Name, "Content", StringComparison.OrdinalIgnoreCase) ?
                directory.Parent?.FullName :
                directory.FullName;
        }

        private static string FindBuildTarget(string projectRoot)
        {
            string rootName = Path.GetFileName(projectRoot.TrimEnd(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar
            ));

            string[] solutionFiles = Directory.GetFiles(projectRoot, "*.sln", SearchOption.TopDirectoryOnly);
            string matchingSolution = solutionFiles.FirstOrDefault(path =>
                string.Equals(Path.GetFileNameWithoutExtension(path), rootName, StringComparison.OrdinalIgnoreCase)
            );
            if (matchingSolution != null) return matchingSolution;
            if (solutionFiles.Length == 1) return solutionFiles[0];

            string[] projectFiles = Directory.GetFiles(projectRoot, "*.csproj", SearchOption.TopDirectoryOnly);
            string matchingProject = projectFiles.FirstOrDefault(path =>
                string.Equals(Path.GetFileNameWithoutExtension(path), rootName, StringComparison.OrdinalIgnoreCase)
            );
            if (matchingProject != null) return matchingProject;
            return projectFiles.Length == 1 ? projectFiles[0] : null;
        }

        private static void RunBuild(string projectRoot, string buildTarget)
        {
            Stopwatch stopwatch = Stopwatch.StartNew();
            try
            {
                using Process process = new()
                {
                    StartInfo = new ProcessStartInfo
                    {
                        FileName = "dotnet",
                        Arguments = $"build \"{buildTarget}\" --configuration Release",
                        WorkingDirectory = projectRoot,
                        UseShellExecute = false,
                        CreateNoWindow = true
                    }
                };

                process.Start();
                process.WaitForExit();
                stopwatch.Stop();

                if (process.ExitCode == 0)
                {
                    ConsoleWindowController.Log(
                        $"Release build succeeded: {Path.GetFileName(buildTarget)}. Build took {stopwatch.Elapsed.TotalSeconds:F1} seconds."
                    );
                }
                else
                {
                    ConsoleWindowController.Log(
                        $"Release build failed with exit code {process.ExitCode}: {Path.GetFileName(buildTarget)}. Build took {stopwatch.Elapsed.TotalSeconds:F1} seconds."
                    );
                }
            }
            catch (Exception ex)
            {
                stopwatch.Stop();
                ConsoleWindowController.Log(
                    $"Release build failed: {ex.Message}. Build took {stopwatch.Elapsed.TotalSeconds:F1} seconds."
                );
            }
            finally
            {
                _isBuilding = false;
            }
        }

    }
}
