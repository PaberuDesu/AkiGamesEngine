using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Text;
using System.Text.Json;
using System.IO;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AkiGames.Core;
using AkiGames.Core.Editor;
using AkiGames.Core.Serialization;
using AkiGames.Events;
using AkiGames.Scripts.Window;
using AkiGames.Scripts.Explorer;
using AkiGames.UI;
using AkiGames.UI.ScrollableList;

namespace AkiGames.Scripts.WindowContentTypes
{
    public class ExplorerWindowController : WindowController
    {
        private string _rootPath; // Корневая папка проекта
        private string _currentPath;
        private string _displayPath = "";

        private static readonly Dictionary<string, Texture2D> _icons = [];
        
        private readonly Stack<string> _pathHistory = new();

        private GameObject _backButton;
        private UITransform _titleTransform;
        private Text _titleText;
        private ScrollableListController _contentList;
        private int _objWidth = 0;
        private string _pathToRenameAfterRefresh = "";
        private bool _registerAkiAfterRename = false;
        private bool _writeScriptTemplateAfterRename = false;

        private GameObject _contentObject;
        private UITransform _contentTransform;

        private HierarchyWindowController _hierarchyWindow;
        private GameWindowController _gameWindow;
        private SceneWindowController _sceneWindow;

        public override void Awake()
        {
            _contentObject = gameObject.Children[3];
            _contentTransform = _contentObject.uiTransform;

            GameObject header = _contentObject.Children[0];
            _titleTransform = header.Children[0].uiTransform;
            _titleText = header.Children[0].GetComponent<Text>();
            _backButton = header.Children[1];
            _contentList = ResolveScrollableContent();

            _hierarchyWindow = gameObject.Parent.Children[1].GetComponent<HierarchyWindowController>();
            _gameWindow = gameObject.Parent.Children[0].GetComponent<GameWindowController>();
            _sceneWindow = gameObject.Parent.Children[2].GetComponent<SceneWindowController>();

            base.Awake();
        }
        
        public void SetProjectPath(string path)
        {
            if (!Directory.Exists(path)) { return; }

            // Проверяем на соответствие требованиям к проекту
            try
            {
                // Проверяем существование папки Content в корне целевой директории
                string contentDir = Path.Combine(path, "Content");
                if (!Directory.Exists(contentDir))
                {
                    MessageBox.Show(
                        "Structure error",
                        "This isn't an AkiGames project. Missing 'Content' folder.",
                        ["OK"]
                    );
                    return;
                }

                // Рекурсивно ищем файл main.aki внутри папки Content
                string[] mainAkiFiles = Directory.GetFiles(contentDir, "main.aki", SearchOption.AllDirectories);
                bool isRootHere = mainAkiFiles.Length > 0;

                if (isRootHere)
                {
                    RefreshEditor(mainAkiFiles[0]); // Загружаем первый найденный файл
                }
                else
                {
                    MessageBox.Show(
                        "Structure error",
                        "Required 'main.aki' not found in 'Content' folder or its subdirectories.",
                        ["OK"]
                    );
                    return;
                }
            }
            catch (Exception ex)
            {
                MessageBox.Show(
                    "Error scanning directory",
                    $"{ex.Message}",
                    ["OK"]
                );
                return;
            }

            _rootPath = path;
            _currentPath = path;
            _pathHistory.Clear();
            RefreshContent();
        }

        private void RefreshContent()
        {
            if (string.IsNullOrEmpty(_currentPath)) return;

            _contentList.gameObject.Children = [];

            UpdateDisplayPath();

            GameObject prefabCopy;
            ExplorerListItem itemToRenameAfterRefresh = null;
            try
            {
                // Получаем все папки
                var folders = Directory.GetDirectories(_currentPath);
                foreach (var folder in folders)
                {
                    // Проверяем, есть ли содержимое в папке
                    bool hasContent = Directory.EnumerateFileSystemEntries(folder).Any();

                    prefabCopy = Game1.Prefabs["ExplorerContentItem"].Copy();
                    prefabCopy.Children[0].GetComponent<Image>().texture =
                        _icons[hasContent ? "full folder" : "empty folder"];
                    ExplorerListItem itemController = prefabCopy.GetComponent<ExplorerListItem>();
                    itemController.FilePath = folder;
                    itemController.Name = Path.GetFileName(folder);
                    itemController.SetActionOnDoubleClick(OpenFolder);
                    if (IsSamePath(folder, _pathToRenameAfterRefresh))
                        itemToRenameAfterRefresh = itemController;

                    _contentList.gameObject.AddChild(prefabCopy);
                }

                // Получаем все файлы
                var files = Directory.GetFiles(_currentPath);
                foreach (var file in files)
                {
                    // TODO Определяем тип файла (пока только изображение или нет)
                    string fileType = "default";

                    bool isImageFile = ContentFileUtility.IsImageFile(file);
                    if (isImageFile) fileType = "image";

                    // Получаем соответствующую текстуру
                    Texture2D icon = _icons.TryGetValue(fileType, out Texture2D value) ?
                        value :
                        _icons["default"];

                    prefabCopy = Game1.Prefabs["ExplorerContentItem"].Copy();
                    prefabCopy.Children[0].GetComponent<Image>().texture = icon;
                    ExplorerListItem itemController = prefabCopy.GetComponent<ExplorerListItem>();
                    itemController.Name = Path.GetFileName(file);
                    itemController.SetActionOnDoubleClick(OpenFile);
                    itemController.isFile = true;
                    itemController.FilePath = file;
                    itemController.IsImageFile = isImageFile;
                    if (IsSamePath(file, _pathToRenameAfterRefresh))
                        itemToRenameAfterRefresh = itemController;

                    _contentList.gameObject.AddChild(prefabCopy);
                }
            }
            catch
            {
                prefabCopy = Game1.Prefabs["ExplorerContentItem"].Copy();
                prefabCopy.GetComponent<ExplorerListItem>().Name = "Access error";
                prefabCopy.Children[0].GetComponent<Image>().fillColor = Color.Transparent;

                _contentList.gameObject.AddChild(prefabCopy);
            }

            // Обновляем состояние кнопки "Назад"
            _backButton.IsActive = _pathHistory.Count != 0;
            // Обновляем содержимое
            _contentList.Refresh();
            _contentList.gameObject.RefreshBounds();
            itemToRenameAfterRefresh?.StartRenaming(
                _registerAkiAfterRename,
                _writeScriptTemplateAfterRename
            );
            _pathToRenameAfterRefresh = "";
            _registerAkiAfterRename = false;
            _writeScriptTemplateAfterRename = false;
        }

        public static void LoadContent(ContentManager content)
        {
            _icons.Add("empty folder", content.Load<Texture2D>("ExplorerIcons/folder_icon"));
            _icons.Add("full folder", content.Load<Texture2D>("ExplorerIcons/folder_full_icon"));

            // TODO Загружаем иконки для разных типов файлов (пока только для изображений)
            _icons.Add("default", content.Load<Texture2D>("ExplorerIcons/text_file_icon"));
            _icons.Add("image", content.Load<Texture2D>("ExplorerIcons/image_icon"));
        }

        internal void UpdateDisplayPath()
        {// Вычисляем относительный путь внутри проекта
            if (_currentPath.StartsWith(_rootPath))
            {
                _displayPath = _currentPath[_rootPath.Length..];
                if (_displayPath.StartsWith(Path.DirectorySeparatorChar))
                    _displayPath = _displayPath[1..];

                if (string.IsNullOrEmpty(_displayPath))
                    _displayPath = Path.GetFileName(_rootPath);
            }
            else _displayPath = _currentPath;
            FitDisplayPath(_displayPath);
        }

        public override void Update()
        {
            if (_objWidth != _contentTransform.Bounds.Width)
            {
                FitDisplayPath(_displayPath);
                _objWidth = _contentTransform.Bounds.Width;
            }
        }

        private void FitDisplayPath(string displayPath)
        {
            Vector2 pathSize = Fonts.main.MeasureString(displayPath);
            int maxWidth = _titleTransform.Bounds.Width;
            if (pathSize.X > maxWidth)
            {
                float ratio = maxWidth / pathSize.X;
                int chars = (int)(displayPath.Length * ratio) - 3;
                displayPath = string.Concat("...", displayPath.AsSpan(displayPath.Length - chars));
            }

            _titleText.text = displayPath;
        }

        private void OpenFolder(string folderName)
        {
            string newPath = Path.Combine(_currentPath, folderName);
            if (Directory.Exists(newPath))
            {
                _pathHistory.Push(_currentPath);
                _currentPath = newPath;
                RefreshContent();
            }
        }

        private void OpenFile(string fileName)
        {
            string fullPath = Path.Combine(_currentPath, fileName);
            if (File.Exists(fullPath))
            {
                if (fileName[^4..] == ".aki")
                {
                    RefreshEditor(fullPath);
                }
                else
                {
                    try
                    {
                        Process.Start(new ProcessStartInfo
                        {
                            FileName = fullPath,
                            UseShellExecute = true
                        });
                    }
                    catch { }
                }
            }
        }

        public override void OnRMBUp()
        {
            if (string.IsNullOrWhiteSpace(_currentPath))
                return;

            if (FindExplorerItem(Input.MouseHoverTarget) != null)
                return;

            _contentList?.ChooseItem(null);

            if (IsInContent(_currentPath))
            {
                ShowExplorerContext(
                    ("Create scene", CreateScene),
                    ("Create folder", CreateFolder)
                );
                return;
            }

            ShowExplorerContext(
                ("Create folder", CreateFolder),
                ("Create C# script", CreateCSharpScript)
            );
        }

        public void ShowItemContext(ExplorerListItem item)
        {
            if (item == null) return;

            if (item.isFile)
            {
                ShowExplorerContext(
                    ("Rename", () => item.StartRenaming()),
                    (GetDeleteMenuText(item.FilePath), () => DeleteFile(item.FilePath))
                );
                return;
            }

            ShowExplorerContext(
                ("Rename", () => item.StartRenaming()),
                ("Delete folder", () => DeleteFolder(item.FilePath))
            );
        }

        public bool IsCursorInsideExplorerWindow() =>
            gameObject.uiTransform.Contains(Input.mousePosition);

        public ExplorerListItem FindExplorerItemAt(GameObject target) =>
            FindExplorerItem(target);

        public void MoveItemIntoFolder(string sourcePath, string targetFolderPath)
        {
            try
            {
                if (string.IsNullOrWhiteSpace(sourcePath) ||
                    string.IsNullOrWhiteSpace(targetFolderPath) ||
                    !Directory.Exists(targetFolderPath))
                {
                    return;
                }

                bool sourceIsFile = File.Exists(sourcePath);
                bool sourceIsFolder = Directory.Exists(sourcePath);
                if (!sourceIsFile && !sourceIsFolder) return;

                if (sourceIsFolder &&
                    (IsSamePath(sourcePath, targetFolderPath) || IsPathInside(targetFolderPath, sourcePath)))
                {
                    return;
                }

                string destinationPath = Path.Combine(targetFolderPath, Path.GetFileName(sourcePath));
                if (File.Exists(destinationPath) || Directory.Exists(destinationPath))
                {
                    ConsoleWindowController.Log(
                        $"Explorer move failed: '{Path.GetFileName(sourcePath)}' already exists in target folder."
                    );
                    return;
                }

                TryGetContentRoot(sourcePath, out string oldContentRoot);

                if (sourceIsFile)
                    File.Move(sourcePath, destinationPath);
                else
                    Directory.Move(sourcePath, destinationPath);

                TryGetContentRoot(destinationPath, out string newContentRoot);
                UpdateMgcbForMove(sourcePath, destinationPath, sourceIsFile, oldContentRoot, newContentRoot);
                RefreshContent();
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log($"Explorer move failed: {ex.Message}");
                RefreshContent();
            }
        }

        public bool TryAddPrefabToHierarchy(string filePath)
        {
            if (!TryGetPrefabLink(filePath, out string prefabLink))
                return false;

            return _hierarchyWindow?.TryAddPrefabLink(prefabLink, Input.MouseHoverTarget) == true;
        }

        public void CompleteItemRename(
            string oldPath,
            string newBaseName,
            bool isFile,
            string extension,
            bool registerAkiAfterRename,
            bool writeScriptTemplateAfterRename
        )
        {
            try
            {
                if (string.IsNullOrWhiteSpace(newBaseName) || HasInvalidFileNameChars(newBaseName))
                {
                    FinishPendingCreatedFile(oldPath, registerAkiAfterRename, writeScriptTemplateAfterRename);
                    RefreshContent();
                    return;
                }

                string directoryPath = Path.GetDirectoryName(oldPath);
                string finalName = isFile && !newBaseName.EndsWith(extension, StringComparison.OrdinalIgnoreCase) ?
                    newBaseName + extension :
                    newBaseName;
                string newPath = Path.Combine(directoryPath, finalName);

                if (!IsSamePath(oldPath, newPath))
                {
                    if (File.Exists(newPath) || Directory.Exists(newPath))
                    {
                        ConsoleWindowController.Log($"Explorer rename failed: '{finalName}' already exists.");
                        FinishPendingCreatedFile(oldPath, registerAkiAfterRename, writeScriptTemplateAfterRename);
                        RefreshContent();
                        return;
                    }

                    if (isFile)
                        File.Move(oldPath, newPath);
                    else
                        Directory.Move(oldPath, newPath);

                    if (TryGetContentRoot(oldPath, out string contentRoot))
                    {
                        if (isFile && !registerAkiAfterRename)
                        {
                            ContentMgcbRegistry.RenameFile(contentRoot, oldPath, newPath);
                            ContentMgcbRegistry.RegisterFile(contentRoot, newPath);
                        }
                        if (!isFile)
                        {
                            ContentMgcbRegistry.RenameFolder(contentRoot, oldPath, newPath);
                            ContentMgcbRegistry.RegisterFolder(contentRoot, newPath);
                        }
                    }
                }

                FinishPendingCreatedFile(newPath, registerAkiAfterRename, writeScriptTemplateAfterRename);
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log($"Explorer rename failed: {ex.Message}");
                FinishPendingCreatedFile(oldPath, registerAkiAfterRename, writeScriptTemplateAfterRename);
            }

            RefreshContent();
        }

        public static void RegisterCreatedScene(string filePath)
        {
            if (!string.Equals(Path.GetExtension(filePath), ".aki", StringComparison.OrdinalIgnoreCase))
                return;

            if (TryGetContentRoot(filePath, out string contentRoot))
                ContentMgcbRegistry.RegisterFile(contentRoot, filePath);
        }

        public void WriteCreatedScriptTemplate(string filePath)
        {
            if (!string.Equals(Path.GetExtension(filePath), ".cs", StringComparison.OrdinalIgnoreCase))
                return;

            string className = ToCSharpClassName(Path.GetFileNameWithoutExtension(filePath));
            string scriptNamespace = GetScriptNamespace(filePath);
            string template =
                $"namespace {scriptNamespace}\r\n" +
                "{\r\n" +
                $"    public class {className} : GameComponent\r\n" +
                "    {\r\n" +
                "    }\r\n" +
                "}\r\n";

            File.WriteAllText(filePath, template, Encoding.UTF8);
        }

        private void ShowExplorerContext(params (string Text, Action Action)[] items)
        {
            GameObject contextMenu = Game1.Prefabs["ContextMenu"].Copy();
            contextMenu
                .GetComponent<ContextMenuController>()
                ?.Show(Input.mousePosition.ToVector2(), items);
        }

        private void CreateScene()
        {
            try
            {
                if (!IsInContent(_currentPath)) return;

                string filePath = GetUniqueFilePath(_currentPath, "New Scene", ".aki");
                GameObject rootObject = new("RootObject");
                rootObject.AddComponent(rootObject.uiTransform);
                File.WriteAllText(filePath, JsonProjectSerializer.SerializeToJson(rootObject), Encoding.UTF8);

                RefreshContentAndRename(filePath, registerAkiAfterRename: true);
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log($"Explorer create scene failed: {ex.Message}");
            }
        }

        private void CreateFolder()
        {
            try
            {
                string folderPath = GetUniqueDirectoryPath(_currentPath, "New Folder");
                Directory.CreateDirectory(folderPath);
                RefreshContentAndRename(folderPath);
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log($"Explorer create folder failed: {ex.Message}");
            }
        }

        private void CreateCSharpScript()
        {
            try
            {
                if (IsInContent(_currentPath)) return;

                string filePath = GetUniqueFilePath(_currentPath, "NewScript", ".cs");
                File.WriteAllText(filePath, "", Encoding.UTF8);
                RefreshContentAndRename(filePath, writeScriptTemplateAfterRename: true);
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log($"Explorer create C# script failed: {ex.Message}");
            }
        }

        private void DeleteFile(string filePath)
        {
            try
            {
                if (!File.Exists(filePath)) return;
                TryGetContentRoot(filePath, out string contentRoot);

                Microsoft.VisualBasic.FileIO.FileSystem.DeleteFile(
                    filePath,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin
                );

                if (!string.IsNullOrWhiteSpace(contentRoot))
                    ContentMgcbRegistry.RemoveFile(contentRoot, filePath);

                RefreshContent();
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log($"Explorer delete failed: {ex.Message}");
            }
        }

        private void DeleteFolder(string folderPath)
        {
            try
            {
                if (!Directory.Exists(folderPath)) return;
                TryGetContentRoot(folderPath, out string contentRoot);

                Microsoft.VisualBasic.FileIO.FileSystem.DeleteDirectory(
                    folderPath,
                    Microsoft.VisualBasic.FileIO.UIOption.OnlyErrorDialogs,
                    Microsoft.VisualBasic.FileIO.RecycleOption.SendToRecycleBin
                );

                if (!string.IsNullOrWhiteSpace(contentRoot))
                    ContentMgcbRegistry.RemoveFolder(contentRoot, folderPath);

                RefreshContent();
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log($"Explorer delete folder failed: {ex.Message}");
            }
        }

        private static void UpdateMgcbForMove(
            string oldPath,
            string newPath,
            bool isFile,
            string oldContentRoot,
            string newContentRoot
        )
        {
            bool oldIsInContent = !string.IsNullOrWhiteSpace(oldContentRoot);
            bool newIsInContent = !string.IsNullOrWhiteSpace(newContentRoot);

            if (!oldIsInContent && !newIsInContent)
                return;

            if (oldIsInContent && newIsInContent && IsSamePath(oldContentRoot, newContentRoot))
            {
                if (isFile)
                {
                    ContentMgcbRegistry.RenameFile(oldContentRoot, oldPath, newPath);
                    ContentMgcbRegistry.RegisterFile(oldContentRoot, newPath);
                }
                else
                {
                    ContentMgcbRegistry.RenameFolder(oldContentRoot, oldPath, newPath);
                    ContentMgcbRegistry.RegisterFolder(oldContentRoot, newPath);
                }
                return;
            }

            if (oldIsInContent)
            {
                if (isFile)
                    ContentMgcbRegistry.RemoveFile(oldContentRoot, oldPath);
                else
                    ContentMgcbRegistry.RemoveFolder(oldContentRoot, oldPath);
            }

            if (newIsInContent)
            {
                if (isFile)
                    ContentMgcbRegistry.RegisterFile(newContentRoot, newPath);
                else
                    ContentMgcbRegistry.RegisterFolder(newContentRoot, newPath);
            }
        }

        private void RefreshContentAndRename(
            string path,
            bool registerAkiAfterRename = false,
            bool writeScriptTemplateAfterRename = false
        )
        {
            _pathToRenameAfterRefresh = path;
            _registerAkiAfterRename = registerAkiAfterRename;
            _writeScriptTemplateAfterRename = writeScriptTemplateAfterRename;
            RefreshContent();
        }

        private void FinishPendingCreatedFile(
            string filePath,
            bool registerAkiAfterRename,
            bool writeScriptTemplateAfterRename
        )
        {
            if (registerAkiAfterRename)
                RegisterCreatedScene(filePath);
            if (writeScriptTemplateAfterRename)
                WriteCreatedScriptTemplate(filePath);
        }

        private ExplorerListItem FindExplorerItem(GameObject target)
        {
            GameObject current = target;
            while (current != null)
            {
                ExplorerListItem item = current.GetComponent<ExplorerListItem>();
                if (item != null)
                    return item;

                current = current.Parent;
            }

            return null;
        }

        private static string GetDeleteMenuText(string filePath)
        {
            if (string.Equals(Path.GetExtension(filePath), ".aki", StringComparison.OrdinalIgnoreCase))
                return "Delete scene";

            return "Delete file";
        }

        private static bool HasInvalidFileNameChars(string fileName) =>
            fileName.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0;

        private static string GetUniqueFilePath(string directoryPath, string baseName, string extension)
        {
            string filePath = Path.Combine(directoryPath, baseName + extension);
            int suffix = 1;
            while (File.Exists(filePath) || Directory.Exists(filePath))
            {
                filePath = Path.Combine(directoryPath, $"{baseName} {suffix}{extension}");
                suffix++;
            }

            return filePath;
        }

        private static string GetUniqueDirectoryPath(string directoryPath, string baseName)
        {
            string folderPath = Path.Combine(directoryPath, baseName);
            int suffix = 1;
            while (File.Exists(folderPath) || Directory.Exists(folderPath))
            {
                folderPath = Path.Combine(directoryPath, $"{baseName} {suffix}");
                suffix++;
            }

            return folderPath;
        }

        private static bool IsSamePath(string firstPath, string secondPath)
        {
            if (string.IsNullOrWhiteSpace(firstPath) || string.IsNullOrWhiteSpace(secondPath))
                return false;

            string firstFullPath = Path.GetFullPath(firstPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string secondFullPath = Path.GetFullPath(secondPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return string.Equals(firstFullPath, secondFullPath, StringComparison.OrdinalIgnoreCase);
        }

        private static bool IsInContent(string path) =>
            TryGetContentRoot(path, out _);

        private static bool TryGetContentRoot(string path, out string contentRoot)
        {
            contentRoot = null;
            if (string.IsNullOrWhiteSpace(path))
                return false;

            string directoryPath = Directory.Exists(path) ? path : Path.GetDirectoryName(path);
            if (string.IsNullOrWhiteSpace(directoryPath))
                return false;

            DirectoryInfo directory = new(directoryPath);
            while (directory != null)
            {
                if (string.Equals(directory.Name, "Content", StringComparison.OrdinalIgnoreCase))
                {
                    contentRoot = directory.FullName;
                    return true;
                }

                directory = directory.Parent;
            }

            return false;
        }

        private string GetScriptNamespace(string filePath)
        {
            string directoryPath = Path.GetDirectoryName(filePath);
            if (string.IsNullOrWhiteSpace(directoryPath))
                return "AkiGames";

            string[] pathSegments = SplitPath(directoryPath);
            int scriptsIndex = Array.FindLastIndex(
                pathSegments,
                segment => string.Equals(segment, "Scripts", StringComparison.OrdinalIgnoreCase)
            );

            IEnumerable<string> namespaceSegments;
            if (scriptsIndex >= 0)
            {
                namespaceSegments = pathSegments.Skip(scriptsIndex);
            }
            else if (IsSamePath(directoryPath, _rootPath) || IsPathInside(directoryPath, _rootPath))
            {
                string relativeDirectory = Path.GetRelativePath(_rootPath, directoryPath);
                namespaceSegments = string.Equals(relativeDirectory, ".", StringComparison.Ordinal) ?
                    [] :
                    SplitPath(relativeDirectory);
            }
            else
            {
                namespaceSegments = [];
            }

            List<string> namespaceParts = ["AkiGames"];
            namespaceParts.AddRange(namespaceSegments
                .Select(segment => ToCSharpIdentifier(segment, ""))
                .Where(segment => !string.IsNullOrWhiteSpace(segment)));

            return string.Join(".", namespaceParts);
        }

        private static string[] SplitPath(string path) =>
            path
                .Split([Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar], StringSplitOptions.RemoveEmptyEntries);

        private static bool IsPathInside(string path, string rootPath)
        {
            if (string.IsNullOrWhiteSpace(path) || string.IsNullOrWhiteSpace(rootPath))
                return false;

            string fullPath = Path.GetFullPath(path).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            string fullRootPath = Path.GetFullPath(rootPath).TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);
            return fullPath.StartsWith(
                fullRootPath + Path.DirectorySeparatorChar,
                StringComparison.OrdinalIgnoreCase
            );
        }

        private static string ToCSharpClassName(string value) =>
            ToCSharpIdentifier(value, "NewScript");

        private static string ToCSharpIdentifier(string value, string fallback)
        {
            StringBuilder builder = new();
            foreach (char character in value ?? "")
            {
                if (char.IsLetterOrDigit(character) || character == '_')
                    builder.Append(character);
            }

            if (builder.Length == 0)
                builder.Append(fallback);

            if (builder.Length > 0 && char.IsDigit(builder[0]))
                builder.Insert(0, '_');

            return builder.ToString();
        }

        private void RefreshEditor(string fullPath)
        {
            InspectorWindowController.LoadFor(null);// чистим инспектор

            string contentRoot = FindContentRoot(fullPath);
            Game1.SetGameContentRoot(contentRoot);
            ProjectScriptLoader.LoadProjectScripts(FindProjectRoot(contentRoot), Game1.GameContent);

            JsonElement akiContent = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(fullPath));
            GameObject gameMainObject = JsonProjectSerializer.LoadFromJson(akiContent);
            gameMainObject.EnsureUniqueObjectIdsInTree();
            Game1.editableGameMainObject = gameMainObject;

            bool isPrefab = IsPrefabFile(fullPath);
            
            _gameWindow.RefreshContent(gameMainObject);
            _hierarchyWindow.RefreshContent(gameMainObject, fullPath, isPrefab);
            _sceneWindow.RefreshContent(gameMainObject, isPrefab);
        }

        private static string FindContentRoot(string fullPath)
        {
            DirectoryInfo directory = new(Path.GetDirectoryName(fullPath));
            while (directory != null)
            {
                if (directory.Name == "Content") return directory.FullName;
                directory = directory.Parent;
            }

            return Path.GetDirectoryName(fullPath);
        }

        private static string FindProjectRoot(string contentRoot)
        {
            DirectoryInfo directory = new(contentRoot);
            return string.Equals(directory.Name, "Content", StringComparison.OrdinalIgnoreCase) ?
                directory.Parent?.FullName ?? contentRoot :
                Directory.GetParent(contentRoot)?.FullName ?? contentRoot;
        }

        public static bool IsPrefabFile(string fullPath)
        {
            string directoryPath = Path.GetDirectoryName(fullPath);
            if (string.IsNullOrWhiteSpace(directoryPath)) return false;

            DirectoryInfo directory = new(directoryPath);
            while (directory != null)
            {
                if (string.Equals(directory.Name, "Prefabs", StringComparison.OrdinalIgnoreCase))
                    return true;
                if (string.Equals(directory.Name, "Content", StringComparison.OrdinalIgnoreCase))
                    return false;

                directory = directory.Parent;
            }
            ConsoleWindowController.Log($"Warning: Could not determine if file '{fullPath}' is a prefab. Assuming it's not.");
            return false;
        }

        public static bool TryGetPrefabLink(string filePath, out string prefabLink)
        {
            prefabLink = "";
            if (
                string.IsNullOrWhiteSpace(filePath) ||
                !File.Exists(filePath) ||
                !string.Equals(Path.GetExtension(filePath), ".aki", StringComparison.OrdinalIgnoreCase) ||
                !IsPrefabFile(filePath) ||
                !TryGetContentRoot(filePath, out string contentRoot)
            )
            {
                return false;
            }

            string relativePath = Path
                .ChangeExtension(Path.GetRelativePath(contentRoot, filePath), null)
                .Replace('\\', '/');
            prefabLink = $"Content/{relativePath}";
            return true;
        }

        internal void GoBack()
        {
            if (_pathHistory?.Count > 0)
            {
                _currentPath = _pathHistory.Pop();
                RefreshContent();
            }
        }

        public override void ProcessHotkey(Input.HotKey hotkey)
        {
            if (hotkey == Input.HotKey.CtrlZ) GoBack();
        }
    }
}
