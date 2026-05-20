using System;
using System.IO;
using System.Text.Json;
using AkiGames.Core;
using AkiGames.Core.Serialization;
using AkiGames.Events;
using AkiGames.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;

namespace AkiGames.Scripts
{
    public class FactoryApp : GameComponent
    {
        private enum PendingAction
        {
            None,
            NewGame,
            SaveGame,
            LoadGame,
            MainMenu
        }

        public string GameSceneAsset = "Content/Scenes/GameScene";
        public string SaveFileName = "savegame.json";

        [DontSerialize, HideInInspector] public static FactoryApp Instance { get; private set; }

        [DontSerialize, HideInInspector] private GameObject _sceneHost;
        [DontSerialize, HideInInspector] private GameObject _mainMenuPanel;
        [DontSerialize, HideInInspector] private GameObject _newGameButton;
        [DontSerialize, HideInInspector] private GameObject _loadGameButton;
        [DontSerialize, HideInInspector] private GameObject _exitButton;
        [DontSerialize, HideInInspector] private Text _menuStatusText;
        [DontSerialize, HideInInspector] private FactorySeedInput _seedInput;
        [DontSerialize, HideInInspector] private GameObject _loadedSceneRoot;
        [DontSerialize, HideInInspector] private FactoryGame _loadedGame;
        [DontSerialize, HideInInspector] private PendingAction _pendingAction;
        [DontSerialize, HideInInspector] private bool _wasLeftDown;

        public override void Awake()
        {
            Instance = this;
            BindUi();
            SetMainMenuVisible(true);
            SetMenuStatus(File.Exists(GetSaveFilePath())
                ? "Load your last shift or start fresh."
                : "Start a new shift.");
        }

        public override void Update()
        {
            if (_sceneHost == null || _mainMenuPanel == null)
                BindUi();

            HandleMainMenuClick();
            HandlePendingAction();
        }

        public override void Dispose()
        {
            if (ReferenceEquals(Instance, this))
                Instance = null;

            UnloadGameScene();
            base.Dispose();
        }

        public void RequestNewGame() => _pendingAction = PendingAction.NewGame;
        public void RequestSaveGame() => _pendingAction = PendingAction.SaveGame;
        public void RequestLoadGame() => _pendingAction = PendingAction.LoadGame;
        public void RequestMainMenu() => _pendingAction = PendingAction.MainMenu;

        private void HandleMainMenuClick()
        {
            MouseState mouse = Mouse.GetState();
            Point mousePosition = Input.mousePosition;
            bool pressedNow = mouse.LeftButton == ButtonState.Pressed;

            if (_mainMenuPanel?.IsActive == true && pressedNow && !_wasLeftDown)
            {
                if (_newGameButton?.uiTransform.Contains(mousePosition) == true)
                    RequestNewGame();
                else if (_loadGameButton?.uiTransform.Contains(mousePosition) == true)
                    RequestLoadGame();
                else if (_exitButton?.uiTransform.Contains(mousePosition) == true)
                    Game1.ExitGame();
            }

            _wasLeftDown = pressedNow;
        }

        private void HandlePendingAction()
        {
            PendingAction action = _pendingAction;
            if (action == PendingAction.None)
                return;

            _pendingAction = PendingAction.None;

            switch (action)
            {
                case PendingAction.NewGame:
                    StartNewGame();
                    break;
                case PendingAction.SaveGame:
                    SaveGame();
                    break;
                case PendingAction.LoadGame:
                    LoadGame();
                    break;
                case PendingAction.MainMenu:
                    ReturnToMainMenu();
                    break;
            }
        }

        private void StartNewGame()
        {
            _seedInput?.CommitPending();
            int seed = FactoryRules.SeedFromText(_seedInput?.CurrentValue);

            LoadGameScene();
            if (_loadedGame == null)
            {
                SetMainMenuVisible(true);
                SetMenuStatus("Game scene failed to load.");
                return;
            }

            _loadedGame.StartNewWorld(seed);
            SetMainMenuVisible(false);
        }

        private void SaveGame()
        {
            if (_loadedGame == null)
            {
                SetMenuStatus("There is no running game to save.");
                return;
            }

            try
            {
                string savePath = GetSaveFilePath();
                Directory.CreateDirectory(Path.GetDirectoryName(savePath));
                string json = JsonSerializer.Serialize(_loadedGame.ExportSave(), new JsonSerializerOptions
                {
                    WriteIndented = true
                });
                File.WriteAllText(savePath, json);
            }
            catch
            {
                if (_mainMenuPanel?.IsActive == true)
                    SetMenuStatus("Save failed.");
            }
        }

        private void LoadGame()
        {
            string savePath = GetSaveFilePath();
            if (!File.Exists(savePath))
            {
                SetMainMenuVisible(true);
                SetMenuStatus("No save file found yet.");
                return;
            }

            try
            {
                string json = File.ReadAllText(savePath);
                FactorySaveData saveData = JsonSerializer.Deserialize<FactorySaveData>(json);
                if (saveData == null)
                {
                    SetMainMenuVisible(true);
                    SetMenuStatus("Save file is empty.");
                    return;
                }

                LoadGameScene();
                if (_loadedGame == null)
                {
                    SetMainMenuVisible(true);
                    SetMenuStatus("Game scene failed to load.");
                    return;
                }

                _loadedGame.LoadFromSave(saveData);
                SetMainMenuVisible(false);
            }
            catch
            {
                SetMainMenuVisible(true);
                SetMenuStatus("Save file could not be loaded.");
            }
        }

        private void ReturnToMainMenu()
        {
            UnloadGameScene();
            SetMainMenuVisible(true);
            SetMenuStatus(File.Exists(GetSaveFilePath())
                ? "Game paused. You can load or start over."
                : "Back at the main menu.");
        }

        private void LoadGameScene()
        {
            UnloadGameScene();

            try
            {
                GameObject loadedRoot = LoadLinkedScene(GameSceneAsset);
                if (loadedRoot == null)
                    return;

                _sceneHost?.AddChild(loadedRoot);
                loadedRoot.RefreshBounds(_sceneHost?.uiTransform);
                _loadedSceneRoot = loadedRoot;
                _loadedGame = FindComponent<FactoryGame>(loadedRoot);
                RefreshRootBounds();
            }
            catch
            {
                _loadedSceneRoot = null;
                _loadedGame = null;
            }
        }

        private void UnloadGameScene()
        {
            if (_loadedSceneRoot == null)
                return;

            GameObject sceneToDispose = _loadedSceneRoot;
            _loadedSceneRoot = null;
            _loadedGame = null;
            sceneToDispose.Dispose();
            RefreshRootBounds();
        }

        private static GameObject LoadLinkedScene(string sceneLink)
        {
            string wrapperJson = $$"""
            {
              "ObjectName": "SceneLinkLoader",
              "Children": [
                { "Link": {{JsonSerializer.Serialize(sceneLink)}} }
              ]
            }
            """;

            using JsonDocument document = JsonDocument.Parse(wrapperJson);
            GameObject wrapper = JsonProjectSerializer.LoadFromJson(document.RootElement);
            GameObject scene = wrapper.Children.Count > 0 ? wrapper.Children[0] : null;
            if (scene == null)
            {
                wrapper.Dispose();
                return null;
            }

            wrapper.RemoveChild(scene);
            wrapper.Dispose();
            return scene;
        }

        private void BindUi()
        {
            GameObject root = GetRoot(gameObject);
            _sceneHost = FindByName(root, "SceneHost");
            _mainMenuPanel = FindByName(root, "MainMenuPanel");
            _newGameButton = FindByName(root, "MainMenuNewGameButton");
            _loadGameButton = FindByName(root, "MainMenuLoadGameButton");
            _exitButton = FindByName(root, "MainMenuExitButton");
            _menuStatusText = FindByName(root, "MainMenuStatusText")?.GetComponent<Text>();
            _seedInput = FindByName(root, "MainMenuSeedInput")?.GetComponent<FactorySeedInput>();
        }

        private static GameObject GetRoot(GameObject obj)
        {
            GameObject current = obj;
            while (current?.Parent != null)
                current = current.Parent;

            return current;
        }

        private static GameObject FindByName(GameObject root, string objectName)
        {
            if (root == null)
                return null;

            if (root.ObjectName == objectName)
                return root;

            foreach (GameObject child in root.Children)
            {
                GameObject found = FindByName(child, objectName);
                if (found != null)
                    return found;
            }

            return null;
        }

        private static T FindComponent<T>(GameObject root) where T : GameComponent
        {
            if (root == null)
                return null;

            T component = root.GetComponent<T>();
            if (component != null)
                return component;

            foreach (GameObject child in root.Children)
            {
                T found = FindComponent<T>(child);
                if (found != null)
                    return found;
            }

            return null;
        }

        private void SetMainMenuVisible(bool visible)
        {
            if (_mainMenuPanel != null)
                _mainMenuPanel.IsActive = visible;
        }

        private void SetMenuStatus(string message)
        {
            if (_menuStatusText != null)
                _menuStatusText.text = message ?? "";
        }

        private static string GetSaveFilePath()
        {
            string rootPath = Path.Combine(
                Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData),
                "AkiGames",
                "Factory"
            );
            return Path.Combine(rootPath, "savegame.json");
        }

        private static void RefreshRootBounds()
        {
            if (Game1.MainObject == null || Game1.AppGraphicsDevice == null)
                return;

            Game1.MainObject.RefreshBounds(
                UITransform.TransformOfBounds(Game1.AppGraphicsDevice.Viewport.Bounds)
            );
        }
    }
}
