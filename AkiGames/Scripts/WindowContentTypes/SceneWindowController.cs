using AkiGames.Core;
using Rectangle = AkiGames.Core.Rectangle;
using AkiGames.Scripts.Window;
using AkiGames.UI;
using Image = AkiGames.UI.Image;

namespace AkiGames.Scripts.WindowContentTypes
{
    public class SceneWindowController : WindowController
    {
        private static Veldrid.Texture _tex = null!;
        private UITransform _container = null!;
        private GameObject _content = null!;
        private Rectangle _prevBounds;
        private float _scaleFactor = 1;

        public override void Awake()
        {
            GameObject parent  = gameObject.Children[3];
            _container = parent.uiTransform;
            _content  = parent.Children[1];
            parent.Children[0].GetComponent<Image>()!.texture = _tex;
            base.Awake();
        }

        public static void LoadContent() =>
            _tex = VeldridGame.UIImages.GetValueOrDefault("scene_grid")!;
        
        public override void Update()
        {
            if (_container.Bounds != _prevBounds)
            {
                float aspectRatio = _container.Bounds.Width / _container.Bounds.Height;
                if (aspectRatio > 1920 / 1080.0f)
                {
                    _content.uiTransform.Width = (int)(_container.Bounds.Height * 1920.0f / 1080);
                    _content.uiTransform.Height = _container.Bounds.Height;
                }
                else
                {
                    _content.uiTransform.Width = _container.Bounds.Width;
                    _content.uiTransform.Height = (int)(_container.Bounds.Width * 1080.0f / 1920);
                }
                _prevBounds = _container.Bounds;
                _scaleFactor = _content.uiTransform.Width / 1920f;

                if (_content.Children.Count > 0) RescaleObjectsRecursive(_content.Children[0]);

                _content.RefreshBounds();
            }

            base.Update();
        }
        
        public void RefreshContent(GameObject gameObjectTree)
        {
            _content.Children = [ProcessChildrenRecursive(gameObjectTree)];
            _content.Children[0].AkiGamesAwakeTree();
            _prevBounds = Rectangle.Empty; // чтобы перезагрузить Update
        }
        

        private GameObject ProcessChildrenRecursive(GameObject objectRealization)
        {
            GameObject objectConcept = new(objectRealization.ObjectName + " (concept)");
            UITransform tr = objectRealization.uiTransform.Copy();

            objectConcept.uiTransform = tr;
            objectConcept.AddComponent(tr);
            
            Image image = (Image) objectRealization.GetComponent<Image>()?.Copy()!;
            if (image != null) objectConcept.AddComponent(image);

            SceneInteractableObject interactable = new()
            {
                source = objectRealization.uiTransform
            };
            interactable.SetActionOnDoubleClick(() => { InspectorWindowController.LoadFor(objectRealization); });
            objectConcept.AddComponent(interactable);

            foreach (GameObject childRealization in objectRealization.Children)
            {
                objectConcept.AddChild(ProcessChildrenRecursive(childRealization));
            }
            return objectConcept;
        }

        private void RescaleObjectsRecursive(GameObject objectConcept)
        {
            UITransform trConcept = objectConcept.uiTransform;
            UITransform trRealization = objectConcept.GetComponent<SceneInteractableObject>()!.source;
            trConcept.OffsetMin = trRealization.OffsetMin * _scaleFactor;
            trConcept.OffsetMax = trRealization.OffsetMax * _scaleFactor;
            trConcept.Width = (int)(trRealization.Width * _scaleFactor);
            trConcept.Height = (int)(trRealization.Height * _scaleFactor);

            foreach (GameObject child in objectConcept.Children)
            {
                RescaleObjectsRecursive(child);
            }
        }
    }
}