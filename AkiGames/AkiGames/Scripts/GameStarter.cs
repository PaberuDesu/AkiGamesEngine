using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AkiGames.Core;
using AkiGames.Events;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI;

namespace AkiGames.Scripts
{
    public class GameStarter : InteractableComponent
    {
        private Image _buttonImage;
        private Texture2D _playTexture;
        private Texture2D _stopTexture;
        private GameWindowController _gameWindow;
        private GameObject _dimmerObject;
        private bool _isRunning;

        public override void Awake()
        {
            image = gameObject.GetComponent<Image>();
            _buttonImage = image;
            _playTexture = _buttonImage?.texture;
            _gameWindow = gameObject.GetAncestry()
                .Select(ancestor => ancestor.GetComponent<GameWindowController>())
                .FirstOrDefault(controller => controller != null);

            RefreshVisibility();
        }

        public override void Update()
        {
            RefreshVisibility();
        }

        public override void OnMouseUp()
        {
            ToggleGame();
            StopInteracting();
        }

        private void ToggleGame()
        {
            if (_isRunning)
            {
                StopGame();
            }
            else
            {
                StartGame();
            }
        }

        private void StartGame()
        {
            if (_gameWindow == null || !HasOpenedProject())
                return;

            _gameWindow.StartGame();
            if (!_gameWindow.IsGameRunning || Game1.gameMainObject == null)
                return;

            _isRunning = true;
            EventSystem.StartGameInput(Game1.gameMainObject, _gameWindow.ViewTransform, gameObject);
            ShowDimmer();
            SetButtonTexture(GetStopTexture());
        }

        private void RefreshVisibility()
        {
            bool hasOpenedProject = HasOpenedProject();
            if (!hasOpenedProject && _isRunning)
                StopGame();

            gameObject.IsActive = hasOpenedProject;
        }

        private static bool HasOpenedProject() =>
            Game1.editableGameMainObject != null &&
            !string.IsNullOrWhiteSpace(Game1.GameContentRoot);

        private void StopGame()
        {
            EventSystem.StopGameInput();
            _gameWindow?.StopGame();
            HideDimmer();
            SetButtonTexture(_playTexture);
            _isRunning = false;
        }

        private void SetButtonTexture(Texture2D texture)
        {
            if (_buttonImage != null && texture != null)
                _buttonImage.texture = texture;
        }

        private Texture2D GetStopTexture()
        {
            if (Game1.UIImages.TryGetValue("Stop", out Texture2D stopTexture))
                return stopTexture;

            if (_stopTexture != null)
                return _stopTexture;

            _stopTexture = new Texture2D(Game1.AppGraphicsDevice, 32, 32);
            Color[] pixels = new Color[32 * 32];
            for (int y = 0; y < 32; y++)
            {
                for (int x = 0; x < 32; x++)
                {
                    bool insideStopSquare = x >= 8 && x < 24 && y >= 8 && y < 24;
                    pixels[y * 32 + x] = insideStopSquare ? new Color(220, 80, 80) : Color.Transparent;
                }
            }

            _stopTexture.SetData(pixels);
            return _stopTexture;
        }

        private void ShowDimmer()
        {
            if (_dimmerObject != null) return;

            _dimmerObject = new GameObject("GameModeDimmer")
            {
                IsMouseTargetable = false
            };
            _dimmerObject.AddComponent(new PlayModeDimmer
            {
                ExcludedObject = _gameWindow.gameObject
            });
            Game1.MainObject.AddChild(_dimmerObject);
            _dimmerObject.RefreshBounds(Game1.MainObject.uiTransform);
        }

        private void HideDimmer()
        {
            _dimmerObject?.Dispose();
            _dimmerObject = null;
        }

        public override void Dispose()
        {
            if (_isRunning)
                StopGame();

            base.Dispose();
        }
    }

    internal class PlayModeDimmer : DrawableComponent
    {
        private static Texture2D _pixel;
        private static readonly Color OverlayColor = new(0, 0, 0, 145);

        public GameObject ExcludedObject;

        public PlayModeDimmer()
        {
            zIndex = int.MaxValue - 100;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            EnsureTexture(spriteBatch.GraphicsDevice);

            Rectangle screen = uiTransform.Bounds;
            if (screen.Width <= 0 || screen.Height <= 0)
                screen = spriteBatch.GraphicsDevice.Viewport.Bounds;

            Rectangle excluded = ExcludedObject?.uiTransform.Bounds ?? Rectangle.Empty;
            excluded = Rectangle.Intersect(screen, excluded);

            if (excluded == Rectangle.Empty)
            {
                spriteBatch.Draw(_pixel, screen, OverlayColor);
                return;
            }

            DrawRect(spriteBatch, new Rectangle(screen.Left, screen.Top, screen.Width, excluded.Top - screen.Top));
            DrawRect(spriteBatch, new Rectangle(screen.Left, excluded.Bottom, screen.Width, screen.Bottom - excluded.Bottom));
            DrawRect(spriteBatch, new Rectangle(screen.Left, excluded.Top, excluded.Left - screen.Left, excluded.Height));
            DrawRect(spriteBatch, new Rectangle(excluded.Right, excluded.Top, screen.Right - excluded.Right, excluded.Height));
        }

        private static void EnsureTexture(GraphicsDevice graphicsDevice)
        {
            if (_pixel != null) return;

            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData([Color.White]);
        }

        private static void DrawRect(SpriteBatch spriteBatch, Rectangle rectangle)
        {
            if (rectangle.Width <= 0 || rectangle.Height <= 0) return;
            spriteBatch.Draw(_pixel, rectangle, OverlayColor);
        }
    }
}
