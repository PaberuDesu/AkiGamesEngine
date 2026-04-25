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
        private static GameObject _gameMainObject;
        private Rectangle prevBounds = Rectangle.Empty;

        public override void Awake()
        {
            // Компонент для отображения текстуры игры
            GameObject _viewObject = gameObject.Children[3].Children[0];
            _gameViewImage = _viewObject.GetComponent<Image>();
            _gameViewImage.texture = Game1.GameRenderTarget;

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
            Game1.gameMainObject?.Dispose();

            _gameMainObject = new("main")
            {
                ObjectIDSpace = ObjectIdSpace.Game,
                uiTransform = UITransform.TransformOfBounds(new Rectangle(0, 0, 1920, 1080))
            };

            GameObject runtimeTree = gameObjectTree.Copy(preserveObjectId: true);
            runtimeTree.SetObjectIdSpaceRecursive(ObjectIdSpace.Game);
            _gameMainObject.Children = [runtimeTree];
            runtimeTree.AkiGamesEditorAwakeTree(ObjectIdSpace.Game);
            runtimeTree.RefreshBounds(_gameMainObject.uiTransform);
            Game1.gameMainObject = _gameMainObject;
        
            _viewTransform.RefreshBounds();
            // Обновляем текстуру (на случай если рендер-таргет был пересоздан)
            _gameViewImage.texture = Game1.GameRenderTarget;
        }
    }
}
