using System.Text.Json;
using Veldrid;
using Veldrid.Sdl2;
using Veldrid.StartupUtilities;
using SixLabors.ImageSharp.PixelFormats;
using AkiGames.UI;
using AkiGames.Events;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.Scripts;

namespace AkiGames.Core
{
    public class VeldridGame : IDisposable
    {
        private int _frameCount = 0;
        private double _lastTime = 0;
        private double _fps = 0;
        
        private readonly Sdl2Window _window;
        private static GraphicsDevice _graphicsDevice = null!;
        private readonly CommandList _commandList;
        private readonly SpriteBatch _spriteBatch;
        private Framebuffer _gameFramebuffer = null!;
        private Texture _gameDepthTexture = null!;
        public static Texture WhiteTexture { get; private set; } = null!;

        private bool _isWindowActive = true;
        private bool _closed = false;

        public static IntPtr WindowHandle { get; private set; }
        public static GraphicsDevice AppGraphicsDevice => _graphicsDevice;
        public static Dictionary<string, GameObject> Prefabs { get; } = [];
        public static Dictionary<string, Texture> UIImages { get; } = [];
        public static GameObject MainObject { get; set; } = null!;
        public static GameObject GameMainObject { get; set; } = null!;

        private static Texture _gameRenderTarget = null!;
        public static Texture GameRenderTarget => _gameRenderTarget;
        private Color _backgroundColor = new(45, 45, 45, 255);

        public VeldridGame()
        {
            // Создаём окно 1920x1050, позиция (0,30), с возможностью изменения размера
            _window = new Sdl2Window(
                "AkiGames Editor", 
                0, 30, 1920, 1050, 
                0, 
                false);
            WindowHandle = _window.Handle;
            _window.Resized += OnWindowResized;
            _window.Closed += () => _closed = true;

            var options = new GraphicsDeviceOptions(
                debug: true,
                swapchainDepthFormat: PixelFormat.R16_UNorm,
                syncToVerticalBlank: true,
                resourceBindingModel: ResourceBindingModel.Default);
            _graphicsDevice = VeldridStartup.CreateGraphicsDevice(_window, options, GraphicsBackend.Vulkan);
            
            var texDesc = TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled);
            WhiteTexture = _graphicsDevice.ResourceFactory.CreateTexture(texDesc);
            _graphicsDevice.UpdateTexture(WhiteTexture, new byte[] { 255, 255, 255, 255 }, 0, 0, 0, 1, 1, 1, 0, 0);
            
            EventSystem.Initialize(_window, _graphicsDevice);
            _commandList = _graphicsDevice.ResourceFactory.CreateCommandList();
            _spriteBatch = new SpriteBatch(_graphicsDevice, _commandList);
            RecreateGameRenderTarget();

            // При желании можно переключиться в полноэкранный режим (раскомментировать):
            // _window.WindowState = WindowState.Fullscreen;
        }

        private void RecreateGameRenderTarget()
        {
            _graphicsDevice.WaitForIdle();

            _gameRenderTarget?.Dispose();
            _gameDepthTexture?.Dispose();
            _gameFramebuffer?.Dispose();

            uint width = _graphicsDevice.SwapchainFramebuffer.Width;
            uint height = _graphicsDevice.SwapchainFramebuffer.Height;
            var colorFormat = _graphicsDevice.SwapchainFramebuffer.OutputDescription.ColorAttachments[0].Format;

            _gameRenderTarget = _graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                width, height, 1, 1, colorFormat, TextureUsage.RenderTarget | TextureUsage.Sampled));

            _gameDepthTexture = _graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                width, height, 1, 1, PixelFormat.R16_UNorm, TextureUsage.DepthStencil));

            _gameFramebuffer = _graphicsDevice.ResourceFactory.CreateFramebuffer(new FramebufferDescription(
                _gameDepthTexture,
                _gameRenderTarget
            ));
        }

        private void OnWindowResized()
        {
            _graphicsDevice.ResizeMainWindow((uint)_window.Width, (uint)_window.Height);
            RecreateGameRenderTarget();
            SetMainObjectBounds();
        }

        private static void SetMainObjectBounds()
        {
            var bounds = new Rectangle(0, 0,
                (int)_graphicsDevice.SwapchainFramebuffer.Width,
                (int)_graphicsDevice.SwapchainFramebuffer.Height);
            MainObject.uiTransform.ForceSetBounds(bounds);
            MainObject.RefreshBounds();
        }

        public static void LoadContent()
        {
            string contentPath = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, "Content");
            if (Directory.Exists(contentPath))
            {
                var allPngFiles = Directory.GetFiles(contentPath, "*.png", SearchOption.AllDirectories);
                foreach (var file in allPngFiles)
                {
                    // Получаем относительный путь от папки Content
                    string relativePath = Path.GetRelativePath(contentPath, file);
                    // Убираем расширение и заменяем обратные слеши на прямые
                    string key = Path.ChangeExtension(relativePath, null).Replace('\\', '/');

                    using var stream = File.OpenRead(file);
                    using var image = SixLabors.ImageSharp.Image.Load<Rgba32>(stream);

                    var texture = _graphicsDevice.ResourceFactory.CreateTexture(TextureDescription.Texture2D(
                        (uint)image.Width, (uint)image.Height, 1, 1,
                        PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));

                    var pixels = new byte[image.Width * image.Height * 4];
                    image.CopyPixelDataTo(pixels);
                    _graphicsDevice.UpdateTexture(texture, pixels, 0, 0, 0,
                        (uint)image.Width, (uint)image.Height, 1, 0, 0);

                    UIImages[key] = texture;
                }
            }

            string prefabsPath = Path.Combine("Content", "Prefabs");
            if (Directory.Exists(prefabsPath))
            {
                foreach (var file in Directory.GetFiles(prefabsPath, "*.aki"))
                {
                    string json = File.ReadAllText(file);
                    JsonElement root = JsonSerializer.Deserialize<JsonElement>(json);
                    GameObject go = JsonProjectSerializer.LoadFromJson(root);
                    string key = Path.GetFileNameWithoutExtension(file);
                    Prefabs[key] = go;
                }
                Prefabs["empty"] = MainObject;
            }

            string fontPath = Path.Combine("Content", "Fonts", "arial.ttf");
            if (File.Exists(fontPath))
            {
                TextRenderer.LoadFont(fontPath, 14);
            }
            else
            {
                Console.WriteLine("Font file not found. Text will not be rendered.");
            }

            ExplorerWindowController.LoadContent();
            SceneWindowController.LoadContent();
            InspectorItemController.LoadContent();
            HierarchyExpander.LoadContent();

            string mainJson = File.ReadAllText(Path.Combine("Content", "main.aki"));
            JsonElement akiContent = JsonSerializer.Deserialize<JsonElement>(mainJson);
            MainObject = JsonProjectSerializer.LoadFromJson(akiContent);
            MainObject.AkiGamesAwakeTree();
            EventSystem.MainObject = MainObject;
            SetMainObjectBounds();

        }

        public void Run()
        {
            LoadContent();
            _lastTime = DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond;
            while (!_closed && _window.Exists)
            {
                InputSnapshot snapshot = _window.PumpEvents();
                if (!_isWindowActive) continue;
                
                var gameTime = new GameTime();
                GlobalEvents.InvokeUpdate(gameTime);

                EventSystem.Update(snapshot);

                _commandList.Begin();

                // 1. Рендерим игру в текстуру
                if (GameMainObject != null)
                {
                    _commandList.SetFramebuffer(_gameFramebuffer);
                    _commandList.ClearColorTarget(0, new RgbaFloat(_backgroundColor.R/255f, _backgroundColor.G/255f, _backgroundColor.B/255f, 1));
                    _commandList.ClearDepthStencil(1f);
                    _spriteBatch.Begin();
                    GameMainObject.SortByLayers();
                    DrawableComponent.DrawLayers(_spriteBatch);
                    _spriteBatch.End();
                }

                // 2. Рендерим UI в основной буфер (экран)
                _commandList.SetFramebuffer(_graphicsDevice.SwapchainFramebuffer);
                _commandList.ClearColorTarget(0, new RgbaFloat(0.117f, 0.117f, 0.117f, 1)); // #1E1E1E
                _commandList.ClearDepthStencil(1f);
                _spriteBatch.Begin();
                MainObject.SortByLayers();
                DrawableComponent.DrawLayers(_spriteBatch);
                _spriteBatch.End();

                _commandList.End();
                _graphicsDevice.SubmitCommands(_commandList);
                _graphicsDevice.SwapBuffers();
                // Подсчёт FPS
                _frameCount++;
                double currentTime = DateTime.UtcNow.Ticks / (double)TimeSpan.TicksPerSecond;
                if (currentTime - _lastTime >= 1.0)
                {
                    _fps = _frameCount / (currentTime - _lastTime);
                    Console.WriteLine($"FPS: {_fps:F2}");
                    _frameCount = 0;
                    _lastTime = currentTime;
                }
            }
        }

        public void Dispose()
        {
            _graphicsDevice.WaitForIdle();
            _gameFramebuffer?.Dispose();
            _gameDepthTexture?.Dispose();
            _gameRenderTarget?.Dispose();
            _spriteBatch.Dispose();
            _commandList.Dispose();
            _graphicsDevice.Dispose();
            _window.Close();
        }
    }
}