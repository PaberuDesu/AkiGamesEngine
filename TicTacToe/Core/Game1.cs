using System;
using System.Collections.Generic;
using System.Text.Json;
using System.IO;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AkiGames.Events;
using AkiGames.UI;
using AkiGames.Scripts;

namespace AkiGames.Core
{
    public class Game1 : Game
    {
        private bool _isWindowActive = true;

        private GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        public static GameObject MainObject;

        public static event Action<GameTime> UpdateAction;
        public static IntPtr WindowHandle { get; private set; }

        // Словарь для хранения префабов
        public static Dictionary<string, GameObject> Prefabs { get; } = [];
        public static Dictionary<string, Texture2D> UIImages { get; } = [];

        public static GraphicsDevice AppGraphicsDevice { get; private set; }

        // Для рендеринга игры
        public static GameObject gameMainObject;
        public static RenderTarget2D GameRenderTarget { get; private set; }

        public Game1()
        {
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8
            };
            WindowHandle = Window.Handle;
            Content.RootDirectory = "Content";
            IsMouseVisible = true;
            Window.AllowUserResizing = true;

            // Устанавливаем обработчик изменения размера окна
            Window.ClientSizeChanged += OnWindowSizeChanged;
        }

        protected override void Initialize()
        {
            AppGraphicsDevice = GraphicsDevice;
            // Устанавливаем начальный размер окна
            SetWindowToMaximized();

            base.Initialize();

            // Создаем рендер-таргет для игры
            GameRenderTarget = new RenderTarget2D(
                GraphicsDevice,
                GraphicsDevice.Viewport.Width,
                GraphicsDevice.Viewport.Height,
                false,
                GraphicsDevice.PresentationParameters.BackBufferFormat,
                DepthFormat.Depth24
            );
    
            string jsonString = Content.Load<string>("main");
            JsonElement akiContent = JsonSerializer.Deserialize<JsonElement>(jsonString);

            MainObject = JsonProjectSerializer.LoadFromJson(akiContent);
            MainObject.AkiGamesAwakeTree();
            EventSystem.MainObject = MainObject;
            SetMainObjectBounds();
        }

        private void SetWindowToMaximized()
        {
            // Получаем размеры рабочей области экрана
            var screenWidth = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Width;
            var screenHeight = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode.Height;

            // Устанавливаем размеры окна
            _graphics.PreferredBackBufferWidth = screenWidth;
            _graphics.PreferredBackBufferHeight = screenHeight - 30;
            _graphics.ApplyChanges();

            // Центрируем окно на экране
            Window.Position = new Point(
                (screenWidth - Window.ClientBounds.Width) / 2,
                (screenHeight - Window.ClientBounds.Height) / 2 + 15
            );
        }

        // Обработчик изменения размера окна
        private void OnWindowSizeChanged(object sender, EventArgs e)
        {
            if (Window.ClientBounds.Width < 500 || Window.ClientBounds.Height < 400)
            {
                _graphics.PreferredBackBufferWidth = Math.Max(Window.ClientBounds.Width, 500);
                _graphics.PreferredBackBufferHeight = Math.Max(Window.ClientBounds.Height, 400);
                _graphics.ApplyChanges();
            }

            // Обновляем размер контейнера при изменении окна
            SetMainObjectBounds();
        }

        private void SetMainObjectBounds()
        {
            MainObject.RefreshBounds(
                UITransform.TransformOfBounds(new Rectangle(
                    0,
                    0,
                    GraphicsDevice.Viewport.Width,
                    GraphicsDevice.Viewport.Height
                ))
            );
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);

            FieldController.LoadContent(Content);
            CellController.LoadContent(Content);
            Retry.LoadContent(Content);
            EndGame.LoadContent(Content);

            // Путь к папке Prefabs
            string prefabsPath = Path.Combine(Content.RootDirectory, "Prefabs");

            // Проверяем существование папки
            if (Directory.Exists(prefabsPath))
            {
                string[] files = Directory.GetFiles(prefabsPath, "*.aki", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    // Получаем относительный путь без расширения
                    string assetName = file[(Content.RootDirectory.Length + 1)..].
                                    Replace(".aki", "");

                    string jsonString = Content.Load<string>(assetName);
                    JsonElement akiContent = JsonSerializer.Deserialize<JsonElement>(jsonString);
                    GameObject gameObject = JsonProjectSerializer.LoadFromJson(akiContent);

                    // Добавляем в словарь
                    string key = Path.GetFileName(assetName);
                    Prefabs.Add(key, gameObject);
                }
            }

            // Путь к папке UI
            string UIPath = Path.Combine(Content.RootDirectory, "UI");

            // Проверяем существование папки
            if (Directory.Exists(UIPath))
            {
                string[] files = Directory.GetFiles(UIPath, "*.png", SearchOption.AllDirectories);

                foreach (var file in files)
                {
                    // Получаем относительный путь без расширения
                    string assetName = file[(Content.RootDirectory.Length + 1)..].
                                    Replace(".png", "");

                    Texture2D image = Content.Load<Texture2D>(assetName);

                    // Добавляем в словарь
                    string key = Path.GetFileName(assetName);
                    UIImages.Add(key, image);
                }
            }
        }

        protected override void Update(GameTime gameTime)
        {
           try{ if (!_isWindowActive) return;

            EventSystem.Update();
            UpdateAction.Invoke(gameTime);

            base.Update(gameTime);}
            catch(Exception){}
        }

        protected override void OnActivated(object sender, EventArgs args)
        {
            base.OnActivated(sender, args);
            _isWindowActive = true;
        }
        
        protected override void OnDeactivated(object sender, EventArgs args)
        {
            base.OnDeactivated(sender, args);
            _isWindowActive = false;
        }

        protected override void Draw(GameTime gameTime)
        {
            base.Draw(gameTime);

            // Рендерим UI редактора
            GraphicsDevice.Clear(new Color(30, 30, 30));

            _spriteBatch.Begin();
            MainObject.SortByLayers();
            DrawableComponent.DrawLayers(_spriteBatch);
            _spriteBatch.End();
        }
    }
}