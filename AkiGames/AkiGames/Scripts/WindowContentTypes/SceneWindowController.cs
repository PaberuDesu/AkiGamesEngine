using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using AkiGames.Scripts.Window;
using AkiGames.UI;

namespace AkiGames.Scripts.WindowContentTypes
{
    public class SceneWindowController : WindowController
    {
        private static Texture2D _tex = null;
        private UITransform _container;
        private GameObject _content;
        private Rectangle _prevBounds;
        private float scaleFactor = 1;
        public override void Awake()
        {
            GameObject parent  = gameObject.Children[3];
            _container = parent.uiTransform;
            _content  = parent.Children[1];
            parent.Children[0].GetComponent<Image>().texture = _tex;
            base.Awake();
        }

        public static void LoadContent(ContentManager content) =>
            _tex = content.Load<Texture2D>("scene_grid");
        
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
                scaleFactor = _content.uiTransform.Width / 1920f;
                _content.RefreshBounds();
            }

            base.Update();
        }
        
        public void RefreshContent(GameObject gameObjectTree)
        {
            _content.Children = [ProcessChildrenRecursive(gameObjectTree)];
            _content.Children[0].AkiGamesAwakeTree();
            _content.Children[0].RefreshBounds();
        }
        

        private GameObject ProcessChildrenRecursive(GameObject objectRealization)
        {
            GameObject objectConcept = new(objectRealization.ObjectName + " (concept)");
            UITransform tr = objectRealization.uiTransform.Copy();

            tr.OffsetMin *= scaleFactor;
            tr.OffsetMax *= scaleFactor;
            tr.Width = (int)(tr.Width * scaleFactor);
            tr.Height = (int)(tr.Height * scaleFactor);

            objectConcept.uiTransform = tr;
            objectConcept.AddComponent(tr);
            
            Image image = (Image) objectRealization.GetComponent<Image>()?.Copy();
            if (image != null) objectConcept.AddComponent(image);

            SceneInteractableObject interactable = new();
            interactable.SetActionOnDoubleClick(() => { InspectorWindowController.LoadFor(objectRealization); });
            objectConcept.AddComponent(interactable);

            foreach (GameObject childRealization in objectRealization.Children)
            {
                objectConcept.AddChild(ProcessChildrenRecursive(childRealization));
            }
            return objectConcept;
        }
    }
}