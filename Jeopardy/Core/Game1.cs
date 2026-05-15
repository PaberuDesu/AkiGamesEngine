using System;
using System.Collections.Generic;
using System.IO;
using System.Text.Json;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using AkiGames.Core.Serialization;
using AkiGames.Events;
using AkiGames.UI;

namespace AkiGames.Core
{
    public class Game1 : Game
    {
        private readonly GraphicsDeviceManager _graphics;
        private SpriteBatch _spriteBatch;
        private bool _isWindowActive = true;
        private static Game1 _instance;

        public static GameObject MainObject;
        public static event Action<GameTime> UpdateAction;
        public static IntPtr WindowHandle { get; private set; }
        public static GraphicsDevice AppGraphicsDevice { get; private set; }
        public static IServiceProvider AppServices { get; private set; }
        public static ContentManager ProjectContent { get; private set; }
        public static string ContentRoot { get; private set; }
        public static Dictionary<string, GameObject> Prefabs { get; } = [];
        public static Dictionary<string, Texture2D> UIImages { get; } = [];

        private static readonly Dictionary<Texture2D, string> _textureLinks =
            new(ReferenceEqualityComparer.Instance);
        private static readonly Color BackgroundColor = new(30, 30, 30);

        public Game1()
        {
            _instance = this;
            _graphics = new GraphicsDeviceManager(this)
            {
                PreferredDepthStencilFormat = DepthFormat.Depth24Stencil8
            };

            AppServices = Services;
            WindowHandle = Window.Handle;
            Content.RootDirectory = "Content";
            ProjectContent = Content;
            ContentRoot = Content.RootDirectory;
            IsMouseVisible = true;
            Window.Title = "Jeopardy";
            Window.AllowUserResizing = true;
            Window.ClientSizeChanged += OnWindowSizeChanged;
        }

        public static void ExitGame() => _instance?.Exit();

        public static Texture2D LoadGameTexture(string assetPath)
        {
            string contentLink = NormalizeContentLink(assetPath);
            if (string.IsNullOrWhiteSpace(contentLink)) return null;

            string normalizedPath = contentLink["Content/".Length..];
            string assetName = Path.ChangeExtension(normalizedPath, null);

            if (ProjectContent != null)
            {
                try
                {
                    Texture2D texture = ProjectContent.Load<Texture2D>(assetName);
                    RegisterTexture(texture, contentLink);
                    return texture;
                }
                catch { }
            }

            string rawPath = Path.IsPathRooted(assetPath) ?
                assetPath :
                Path.Combine(ContentRoot ?? "", normalizedPath);

            if (!File.Exists(rawPath) || AppGraphicsDevice == null) return null;

            using FileStream stream = File.OpenRead(rawPath);
            Texture2D loadedTexture = Texture2D.FromStream(AppGraphicsDevice, stream);
            RegisterTexture(loadedTexture, contentLink);
            return loadedTexture;
        }

        public static string GetGameTextureLink(Texture2D texture)
        {
            if (texture == null) return "";
            if (_textureLinks.TryGetValue(texture, out string link)) return link;
            return NormalizeContentLink(texture.Name);
        }

        protected override void Initialize()
        {
            AppGraphicsDevice = GraphicsDevice;
            SetWindowToMaximized();

            base.Initialize();

            string jsonString = Content.Load<string>("main");
            JsonElement akiContent = JsonSerializer.Deserialize<JsonElement>(jsonString);

            MainObject = JsonProjectSerializer.LoadFromJson(akiContent);
            MainObject.AkiGamesAwakeTree();
            EventSystem.MainObject = MainObject;
            SetMainObjectBounds();
        }

        protected override void LoadContent()
        {
            _spriteBatch = new SpriteBatch(GraphicsDevice);
            LoadFont();
            LoadPrefabs();
            LoadUIImages();
        }

        protected override void Update(GameTime gameTime)
        {
            if (!_isWindowActive) return;

            try
            {
                EventSystem.Update();
                UpdateAction?.Invoke(gameTime);
                base.Update(gameTime);
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
            }
        }

        protected override void Draw(GameTime gameTime)
        {
            GraphicsDevice.Clear(BackgroundColor);

            _spriteBatch.Begin();
            MainObject?.SortByLayers();
            DrawableComponent.DrawLayers(_spriteBatch);
            _spriteBatch.End();

            base.Draw(gameTime);
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

        private static void RegisterTexture(Texture2D texture, string assetPath)
        {
            string contentLink = NormalizeContentLink(assetPath);
            if (texture == null || string.IsNullOrWhiteSpace(contentLink)) return;

            _textureLinks[texture] = contentLink;
            texture.Name = contentLink;
        }

        private static string NormalizeContentLink(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath)) return "";

            string normalizedPath = assetPath.Trim();
            if (Path.IsPathRooted(normalizedPath) && !string.IsNullOrWhiteSpace(ContentRoot))
            {
                string fullPath = Path.GetFullPath(normalizedPath);
                string contentRoot = Path.GetFullPath(ContentRoot)
                    .TrimEnd(Path.DirectorySeparatorChar, Path.AltDirectorySeparatorChar);

                if (
                    fullPath.StartsWith(contentRoot + Path.DirectorySeparatorChar, StringComparison.OrdinalIgnoreCase) ||
                    fullPath.StartsWith(contentRoot + Path.AltDirectorySeparatorChar, StringComparison.OrdinalIgnoreCase)
                )
                {
                    normalizedPath = Path.GetRelativePath(contentRoot, fullPath);
                }
            }

            if (Path.IsPathRooted(normalizedPath))
            {
                normalizedPath = normalizedPath.Replace('\\', '/');
                int contentIndex = normalizedPath.LastIndexOf("/Content/", StringComparison.OrdinalIgnoreCase);
                if (contentIndex < 0) return "";

                normalizedPath = normalizedPath[(contentIndex + 1)..];
            }

            normalizedPath = normalizedPath.Replace('\\', '/').TrimStart('/');
            if (normalizedPath.StartsWith("Content/", StringComparison.OrdinalIgnoreCase))
                normalizedPath = normalizedPath["Content/".Length..];

            return string.IsNullOrWhiteSpace(normalizedPath) ? "" : $"Content/{normalizedPath}";
        }

        private void SetWindowToMaximized()
        {
            DisplayMode displayMode = GraphicsAdapter.DefaultAdapter.CurrentDisplayMode;
            _graphics.PreferredBackBufferWidth = displayMode.Width;
            _graphics.PreferredBackBufferHeight = displayMode.Height - 30;
            _graphics.ApplyChanges();

            Window.Position = new Point(
                (displayMode.Width - Window.ClientBounds.Width) / 2,
                (displayMode.Height - Window.ClientBounds.Height) / 2 + 15
            );
        }

        private void OnWindowSizeChanged(object sender, EventArgs e)
        {
            if (Window.ClientBounds.Width < 500 || Window.ClientBounds.Height < 400)
            {
                _graphics.PreferredBackBufferWidth = Math.Max(Window.ClientBounds.Width, 500);
                _graphics.PreferredBackBufferHeight = Math.Max(Window.ClientBounds.Height, 400);
                _graphics.ApplyChanges();
            }

            SetMainObjectBounds();
        }

        private void SetMainObjectBounds()
        {
            if (MainObject == null) return;

            MainObject.RefreshBounds(
                UITransform.TransformOfBounds(new Rectangle(
                    0,
                    0,
                    GraphicsDevice.Viewport.Width,
                    GraphicsDevice.Viewport.Height
                ))
            );
        }

        private void LoadFont()
        {
            try
            {
                Fonts.main = Content.Load<SpriteFont>("EditorFont");
            }
            catch
            {
                Fonts.main = CreateTempFont(GraphicsDevice);
            }
        }

        private void LoadPrefabs()
        {
            Prefabs.Clear();
            string prefabsPath = Path.Combine(Content.RootDirectory, "Prefabs");
            if (!Directory.Exists(prefabsPath)) return;

            foreach (string file in Directory.GetFiles(prefabsPath, "*.xnb", SearchOption.AllDirectories))
            {
                string assetName = file[(Content.RootDirectory.Length + 1)..]
                    .Replace(".xnb", "")
                    .Replace('\\', '/');

                string jsonString = Content.Load<string>(assetName);
                JsonElement akiContent = JsonSerializer.Deserialize<JsonElement>(jsonString);
                GameObject gameObject = JsonProjectSerializer.LoadFromJson(akiContent);

                string key = Path.GetFileName(assetName);
                Prefabs[key] = gameObject;
            }

            if (MainObject != null)
                Prefabs["empty"] = MainObject;
        }

        private void LoadUIImages()
        {
            UIImages.Clear();
            string uiPath = Path.Combine(Content.RootDirectory, "UI");
            if (!Directory.Exists(uiPath)) return;

            foreach (string file in Directory.GetFiles(uiPath, "*.xnb", SearchOption.AllDirectories))
            {
                string assetName = file[(Content.RootDirectory.Length + 1)..]
                    .Replace(".xnb", "")
                    .Replace('\\', '/');

                Texture2D image = Content.Load<Texture2D>(assetName);
                string key = Path.GetFileName(assetName);
                UIImages[key] = image;
            }
        }

        private static SpriteFont CreateTempFont(GraphicsDevice device)
        {
            Texture2D texture = new(device, 1, 1);
            texture.SetData([Color.White]);

            List<Rectangle> glyphs = [];
            List<Rectangle> cropping = [];
            List<char> chars = [];
            List<Vector3> kerning = [];

            for (char c = ' '; c <= '~'; c++)
            {
                glyphs.Add(new Rectangle(0, 0, 1, 1));
                cropping.Add(new Rectangle(0, 0, 8, 12));
                chars.Add(c);
                kerning.Add(new Vector3(0, 8, 0));
            }

            return new SpriteFont(texture, glyphs, cropping, chars, 12, 0, kerning, '?');
        }
    }
}
