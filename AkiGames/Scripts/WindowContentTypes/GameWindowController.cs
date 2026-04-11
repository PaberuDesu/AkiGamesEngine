using AkiGames.Core;
using Rectangle = AkiGames.Core.Rectangle;
using AkiGames.Scripts.Window;
using AkiGames.UI;
using Image = AkiGames.UI.Image;

namespace AkiGames.Scripts.WindowContentTypes
{
    public class GameWindowController : WindowController
    {
        private UITransform _viewMaskTransform = null!;
        private UITransform _viewTransform = null!;
        private Image _gameViewImage = null!;
        private static GameObject _gameMainObject = new("main");
        private Rectangle prevBounds = Rectangle.Empty;

        public override void Awake()
        {
            // Компонент для отображения текстуры игры
            GameObject _viewObject = gameObject.Children[3].Children[0];
            _gameViewImage = _viewObject.GetComponent<Image>()!;
            _gameViewImage.texture = VeldridGame.GameRenderTarget;
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
            VeldridGame.GameMainObject = _gameMainObject;
        
            _viewTransform.RefreshBounds();
            // Обновляем текстуру (на случай если рендер-таргет был пересоздан)
            _gameViewImage.texture = VeldridGame.GameRenderTarget;
        }
    }
}