using System.Diagnostics;
using System.Text.Json;
using AkiGames.Core;
using AkiGames.Scripts.Window;
using AkiGames.Events;
using AkiGames.UI;
using AkiGames.UI.ScrollableList;
using Image = AkiGames.UI.Image;

namespace AkiGames.Scripts.WindowContentTypes
{
    public class ExplorerWindowController : WindowController
    {
        private string _rootPath = ""; // Корневая папка проекта
        private string _currentPath = "";
        private string _displayPath = "";

        private static readonly Dictionary<string, Veldrid.Texture> _icons = [];
        private static readonly string[] extImage = [".png", ".jpg", ".jpeg", ".bmp", ".gif"];
        
        private readonly Stack<string> _pathHistory = new();

        private GameObject _backButton = null!;
        private UITransform _titleTransform = null!;
        private Text _titleText = null!;
        private ScrollableListController _contentList = null!;
        private int _objWidth = 0;

        private GameObject _contentObject = null!;
        private UITransform _contentTransform = null!;

        private HierarchyWindowController _hierarchyWindow = null!;
        private GameWindowController _gameWindow = null!;
        private SceneWindowController _sceneWindow = null!;

        public override void Awake()
        {
            _contentObject = gameObject.Children[3];
            _contentTransform = _contentObject.uiTransform;

            GameObject header = _contentObject.Children[0];
            _titleTransform = header.Children[0].uiTransform;
            _titleText = header.Children[0].GetComponent<Text>()!;
            _backButton = header.Children[1];
            scrollableContent = _contentObject.Children[1].Children[0];
            _contentList = scrollableContent.GetComponent<ScrollableListController>()!;

            _hierarchyWindow = gameObject.Parent.Children[1].GetComponent<HierarchyWindowController>()!;
            _gameWindow = gameObject.Parent.Children[0].GetComponent<GameWindowController>()!;
            _sceneWindow = gameObject.Parent.Children[2].GetComponent<SceneWindowController>()!;

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
                    ConsoleWindowController.Log("Structure error: This isn't an AkiGames project. Missing 'Content' folder.");
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
                    ConsoleWindowController.Log("Structure error: Required 'main.aki' file not found in 'Content' folder or its subdirectories.");
                    return;
                }
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log($"Error scanning directory: {ex.Message}");
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

                    prefabCopy = VeldridGame.Prefabs["ExplorerContentItem"].Copy();
                    prefabCopy.Children[0].GetComponent<Image>()!.texture =
                        _icons[hasContent ? "full folder" : "empty folder"];
                    ContentItemController itemController = prefabCopy.GetComponent<ContentItemController>()!;
                    itemController.Name = Path.GetFileName(folder);
                    itemController.SetActionOnDoubleClick(OpenFolder);

                    _contentList.gameObject.AddChild(prefabCopy);
                }

                // Получаем все файлы
                var files = Directory.GetFiles(_currentPath);
                foreach (var file in files)
                {
                    // TODO Определяем тип файла
                    string ext = Path.GetExtension(file).ToLower();
                    string fileType = "default";

                    if (extImage.Contains(ext)) fileType = "image";
                    //else if (new[] { ".cs", ".txt", ".json", ".xml" }.Contains(ext))
                    //    fileType = "script";
                    //else if (new[] { ".fbx", ".obj", ".gltf" }.Contains(ext))
                    //    fileType = "model";

                    // Получаем соответствующую текстуру
                    Veldrid.Texture icon = _icons.TryGetValue(fileType, out Veldrid.Texture? value) ?
                        value :
                        _icons["default"];

                    prefabCopy = VeldridGame.Prefabs["ExplorerContentItem"].Copy();
                    prefabCopy.Children[0].GetComponent<Image>()!.texture = icon;
                    ContentItemController itemController = prefabCopy.GetComponent<ContentItemController>()!;
                    itemController.Name = Path.GetFileName(file);
                    itemController.SetActionOnDoubleClick(OpenFile);

                    _contentList.gameObject.AddChild(prefabCopy);
                }
            }
            catch
            {
                prefabCopy = VeldridGame.Prefabs["ExplorerContentItem"].Copy();
                prefabCopy.GetComponent<ContentItemController>()!.Name = "Access error";
                prefabCopy.Children[0].GetComponent<Image>()!.fillColor = Core.Color.Transparent;

                _contentList.gameObject.AddChild(prefabCopy);
            }

            // Обновляем состояние кнопки "Назад"
            _backButton.IsActive = _pathHistory.Count != 0;
            // Обновляем содержимое
            _contentList.Refresh();
        }

        public static void LoadContent()
        {
            _icons.Add("empty folder", VeldridGame.UIImages.GetValueOrDefault("ExplorerIcons/folder_icon")!);
            _icons.Add("full folder", VeldridGame.UIImages.GetValueOrDefault("ExplorerIcons/folder_full_icon")!);

            // TODO Загружаем иконки для разных типов файлов
            _icons.Add("default", VeldridGame.UIImages.GetValueOrDefault("ExplorerIcons/text_file_icon")!);
            _icons.Add("image", VeldridGame.UIImages.GetValueOrDefault("ExplorerIcons/image_icon")!);
            //_icons.Add("script", content.Load<Texture2D>("script_icon"));
            //_icons.Add("model", content.Load<Texture2D>("model_icon"));
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
            Vector2 pathSize = Core.TextRenderer.MeasureString(displayPath);
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

            JsonElement akiContent = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(fullPath));
            GameObject gameMainObject = JsonProjectSerializer.LoadFromJson(akiContent);
            
            _gameWindow.RefreshContent(gameMainObject);
            _hierarchyWindow.RefreshContent(fullPath, gameMainObject);
            _sceneWindow.RefreshContent(gameMainObject);
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