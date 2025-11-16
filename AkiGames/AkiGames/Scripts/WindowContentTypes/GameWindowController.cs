using Microsoft.Xna.Framework;
using AkiGames.Core;
using AkiGames.Scripts.Window;
using AkiGames.UI;

namespace AkiGames.Scripts.WindowContentTypes
{
    public class GameWindowController : WindowController
    {
        private UITransform _viewMaskTransform;
        private UITransform _viewTransform;
        private Image _gameViewImage;
        private static GameObject _gameMainObject = new("main");
        private Rectangle prevBounds = Rectangle.Empty;

        public override void Awake()
        {
            // Компонент для отображения текстуры игры
            GameObject _viewObject = gameObject.Children[3].Children[0];
            _gameViewImage = _viewObject.GetComponent<Image>();
            _gameViewImage.texture = Game1.GameRenderTarget;
            _gameMainObject.uiTransform = new UITransform
            {
                VerticalAlignment = UITransform.AlignmentV.Middle,
                HorizontalAlignment = UITransform.AlignmentH.Center,
                Width = 1920, Height = 1080
            };

            _viewMaskTransform = _viewObject.Parent.uiTransform;
            _viewTransform = _viewObject.uiTransform;
            prevBounds = _viewTransform.Bounds;
            _viewObject.AkiGamesAwakeTree();

            base.Awake();
        }

        public override void Update()
        {
            if (_viewMaskTransform.Bounds != prevBounds)
            {
                float aspectRatio = _viewMaskTransform.Bounds.Width / _viewMaskTransform.Bounds.Height;
                if (aspectRatio > 1920 / 1080.0f)
                {
                    _viewTransform.Width = (int)(_viewMaskTransform.Bounds.Height * 1920.0f / 1080);
                    _viewTransform.Height = _viewMaskTransform.Bounds.Height;
                }
                else
                {
                    _viewTransform.Width = _viewMaskTransform.Bounds.Width;
                    _viewTransform.Height = (int)(_viewMaskTransform.Bounds.Width * 1080.0f / 1920);
                }
                prevBounds = _viewMaskTransform.Bounds;
            }

            _viewTransform.gameObject.Update();
            base.Update();
        }

        public void RefreshContent(GameObject gameObjectTree)
        {
            // Получаем иерархию объектов
            _gameMainObject.Children = [gameObjectTree];
            _gameMainObject.Children[0].RefreshBounds();
            Game1.gameMainObject = _gameMainObject;
        
            _viewTransform.RefreshBounds();
            // Обновляем текстуру (на случай если рендер-таргет был пересоздан)
            _gameViewImage.texture = Game1.GameRenderTarget;
        }
    }
}