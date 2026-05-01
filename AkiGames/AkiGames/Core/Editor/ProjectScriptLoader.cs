using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Threading.Tasks;
using Microsoft.Xna.Framework.Content;
using AkiGames.Core.Serialization;
using AkiGames.Scripts.WindowContentTypes;

namespace AkiGames.Core.Editor
{
    public static class ProjectScriptLoader
    {
        public const string AssemblyNamePrefix = "AkiGames.ProjectScripts";
        private const string TargetFramework = "net10.0-windows";

        private static readonly Dictionary<string, Type> _activeComponentTypes = new(StringComparer.Ordinal);
        private static string _activeProjectRoot;
        private static string _activeSignature;
        private static bool _activeProjectIsRunningEditor;
        public static bool ActiveProjectIsRunningEditor => _activeProjectIsRunningEditor;

        public static bool LoadProjectScripts(string projectRoot, ContentManager content)
        {
            projectRoot = NormalizePath(projectRoot);
            if (string.IsNullOrWhiteSpace(projectRoot) || !Directory.Exists(projectRoot))
            {
                ClearActiveProject();
                return false;
            }

            if (IsRunningEditorProject(projectRoot))
            {
                ClearActiveProject();
                _activeProjectIsRunningEditor = true;
                return false;
            }

            List<string> scriptFiles = FindScriptFiles(projectRoot);
            string signature = GetSourceSignature(scriptFiles);

            if (_activeProjectRoot == projectRoot && _activeSignature == signature)
            {
                InvokeLoadContent(content);
                return _activeComponentTypes.Count > 0;
            }

            ClearActiveProject();
            _activeProjectRoot = projectRoot;
            _activeSignature = signature;

            if (scriptFiles.Count == 0) return false;

            try
            {
                string assemblyPath = BuildScriptAssembly(projectRoot, scriptFiles);
                Assembly assembly = Assembly.LoadFrom(assemblyPath);
                RegisterComponentTypes(assembly);
                JsonProjectSerializer.ClearTypeCache();
                InvokeLoadContent(content);
                return _activeComponentTypes.Count > 0;
            }
            catch (Exception ex)
            {
                ClearActiveProject();
                ConsoleWindowController.Log($"Project script load failed: {ex.Message}");
                return false;
            }
        }

        public static Type ResolveComponentType(string typeName)
        {
            if (string.IsNullOrWhiteSpace(typeName)) return null;

            return _activeComponentTypes.TryGetValue(typeName, out Type componentType) ?
                componentType :
                null;
        }

        public static IEnumerable<Type> GetActiveComponentTypes() =>
            _activeComponentTypes.Values.Distinct();

        public static bool IsProjectScriptAssembly(Assembly assembly) =>
            assembly?.GetName().Name?.StartsWith(AssemblyNamePrefix, StringComparison.Ordinal) == true;

        private static void ClearActiveProject()
        {
            _activeComponentTypes.Clear();
            _activeProjectRoot = null;
            _activeSignature = null;
            _activeProjectIsRunningEditor = false;
            JsonProjectSerializer.ClearTypeCache();
        }

        private static List<string> FindScriptFiles(string projectRoot)
        {
            return Directory
                .EnumerateDirectories(projectRoot, "*", SearchOption.AllDirectories)
                .Where(path => string.Equals(Path.GetFileName(path), "Scripts", StringComparison.OrdinalIgnoreCase))
                .SelectMany(path => Directory.EnumerateFiles(path, "*.cs", SearchOption.AllDirectories))
                .Distinct(StringComparer.OrdinalIgnoreCase)
                .OrderBy(path => path, StringComparer.OrdinalIgnoreCase)
                .ToList();
        }

        private static string BuildScriptAssembly(string projectRoot, List<string> scriptFiles)
        {
            string projectHash = HashText(projectRoot);
            string assemblyName = $"{AssemblyNamePrefix}.{projectHash}.{DateTime.UtcNow.Ticks}";
            string buildRoot = Path.Combine(Path.GetTempPath(), "AkiGamesEngine", "ProjectScripts", assemblyName);
            Directory.CreateDirectory(buildRoot);

            string projectFile = Path.Combine(buildRoot, $"{assemblyName}.csproj");
            File.WriteAllText(projectFile, CreateProjectFile(projectRoot, scriptFiles, assemblyName), Encoding.UTF8);

            string output = RunDotnetBuild(projectFile);
            string assemblyPath = Path.Combine(buildRoot, "bin", "Debug", TargetFramework, $"{assemblyName}.dll");
            if (!File.Exists(assemblyPath))
            {
                throw new InvalidOperationException($"Script assembly was not produced.{Environment.NewLine}{output}");
            }

            return assemblyPath;
        }

        private static string CreateProjectFile(string projectRoot, List<string> scriptFiles, string assemblyName)
        {
            StringBuilder builder = new();
            builder.AppendLine("<Project Sdk=\"Microsoft.NET.Sdk\">");
            builder.AppendLine("  <PropertyGroup>");
            builder.AppendLine($"    <TargetFramework>{TargetFramework}</TargetFramework>");
            builder.AppendLine("    <OutputType>Library</OutputType>");
            builder.AppendLine($"    <AssemblyName>{EscapeXml(assemblyName)}</AssemblyName>");
            builder.AppendLine("    <EnableDefaultCompileItems>false</EnableDefaultCompileItems>");
            builder.AppendLine("    <ImplicitUsings>disable</ImplicitUsings>");
            builder.AppendLine("    <Nullable>disable</Nullable>");
            builder.AppendLine("    <UseWindowsForms>true</UseWindowsForms>");
            builder.AppendLine("  </PropertyGroup>");
            builder.AppendLine("  <ItemGroup>");

            foreach (string scriptFile in scriptFiles)
            {
                string link = Path.GetRelativePath(projectRoot, scriptFile);
                builder.AppendLine($"    <Compile Include=\"{EscapeXml(scriptFile)}\" Link=\"{EscapeXml(link)}\" />");
            }

            builder.AppendLine("  </ItemGroup>");
            builder.AppendLine("  <ItemGroup>");

            foreach (Assembly assembly in GetReferenceAssemblies())
            {
                string name = assembly.GetName().Name;
                builder.AppendLine($"    <Reference Include=\"{EscapeXml(name)}\">");
                builder.AppendLine($"      <HintPath>{EscapeXml(assembly.Location)}</HintPath>");
                builder.AppendLine("    </Reference>");
            }

            builder.AppendLine("  </ItemGroup>");
            builder.AppendLine("</Project>");
            return builder.ToString();
        }

        private static IEnumerable<Assembly> GetReferenceAssemblies()
        {
            string engineAssemblyName = typeof(GameComponent).Assembly.GetName().Name;
            string contentAssemblyName = typeof(ContentManager).Assembly.GetName().Name;

            return AppDomain.CurrentDomain
                .GetAssemblies()
                .Where(assembly => !assembly.IsDynamic && !string.IsNullOrWhiteSpace(assembly.Location))
                .Where(assembly =>
                {
                    string name = assembly.GetName().Name;
                    return name == engineAssemblyName ||
                           name == contentAssemblyName ||
                           name.StartsWith("MonoGame", StringComparison.OrdinalIgnoreCase) ||
                           name.StartsWith("ImGui", StringComparison.OrdinalIgnoreCase);
                })
                .GroupBy(assembly => assembly.Location, StringComparer.OrdinalIgnoreCase)
                .Select(group => group.First());
        }

        private static string RunDotnetBuild(string projectFile)
        {
            ProcessStartInfo startInfo = new()
            {
                FileName = "dotnet",
                Arguments = $"build \"{projectFile}\" --configuration Debug --nologo --verbosity minimal",
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                UseShellExecute = false,
                CreateNoWindow = true
            };

            using Process process = Process.Start(startInfo);
            Task<string> outputTask = process.StandardOutput.ReadToEndAsync();
            Task<string> errorTask = process.StandardError.ReadToEndAsync();
            process.WaitForExit();

            string output = outputTask.Result + errorTask.Result;
            if (process.ExitCode != 0)
            {
                throw new InvalidOperationException(output.Trim());
            }

            return output;
        }

        private static void RegisterComponentTypes(Assembly assembly)
        {
            foreach (Type type in GetLoadableTypes(assembly))
            {
                if (!typeof(GameComponent).IsAssignableFrom(type) ||
                    type.IsAbstract ||
                    type.GetConstructor(Type.EmptyTypes) == null)
                {
                    continue;
                }

                _activeComponentTypes[type.Name] = type;
                if (!string.IsNullOrWhiteSpace(type.FullName))
                {
                    _activeComponentTypes[type.FullName] = type;
                }
            }
        }

        private static void InvokeLoadContent(ContentManager content)
        {
            if (content == null) return;

            foreach (Type type in _activeComponentTypes.Values.Distinct())
            {
                MethodInfo loadContent = type.GetMethod(
                    "LoadContent",
                    BindingFlags.Public | BindingFlags.Static,
                    null,
                    [typeof(ContentManager)],
                    null
                );

                try
                {
                    loadContent?.Invoke(null, [content]);
                }
                catch (Exception ex)
                {
                    ConsoleWindowController.Log($"LoadContent failed for {type.Name}: {ex.InnerException?.Message ?? ex.Message}");
                }
            }
        }

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null);
            }
        }

        private static string GetSourceSignature(List<string> scriptFiles)
        {
            StringBuilder builder = new();
            foreach (string scriptFile in scriptFiles)
            {
                FileInfo fileInfo = new(scriptFile);
                builder.Append(fileInfo.FullName);
                builder.Append('|');
                builder.Append(fileInfo.Length);
                builder.Append('|');
                builder.Append(fileInfo.LastWriteTimeUtc.Ticks);
                builder.AppendLine();
            }

            return HashText(builder.ToString());
        }

        private static string HashText(string text)
        {
            byte[] bytes = SHA256.HashData(Encoding.UTF8.GetBytes(text ?? ""));
            return Convert.ToHexString(bytes)[..12].ToLowerInvariant();
        }

        private static string NormalizePath(string path)
        {
            if (string.IsNullOrWhiteSpace(path)) return null;
            return Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
        }

        private static bool IsRunningEditorProject(string projectRoot)
        {
            string editorProjectRoot = FindRunningEditorProjectRoot();
            return !string.IsNullOrWhiteSpace(editorProjectRoot) &&
                   string.Equals(projectRoot, editorProjectRoot, StringComparison.OrdinalIgnoreCase);
        }

        private static string FindRunningEditorProjectRoot()
        {
            string projectFileName = $"{typeof(Game1).Assembly.GetName().Name}.csproj";
            DirectoryInfo directory = new(Path.GetDirectoryName(typeof(Game1).Assembly.Location));

            while (directory != null)
            {
                string candidate = Path.Combine(directory.FullName, projectFileName);
                if (File.Exists(candidate)) return NormalizePath(directory.FullName);
                directory = directory.Parent;
            }

            return null;
        }

        private static string EscapeXml(string value) =>
            value?
                .Replace("&", "&amp;")
                .Replace("\"", "&quot;")
                .Replace("<", "&lt;")
                .Replace(">", "&gt;") ?? "";
    }
}
