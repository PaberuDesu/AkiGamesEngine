using System;
using System.Collections.Generic;
using System.Diagnostics;
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
                    itemController.Name = Path.GetFileName(folder);
                    itemController.SetActionOnDoubleClick(OpenFolder);

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

        private static bool IsPrefabFile(string fullPath)
        {
            DirectoryInfo directory = new(Path.GetDirectoryName(fullPath));
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
