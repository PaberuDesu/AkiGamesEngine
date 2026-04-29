using System;
using Microsoft.Xna.Framework;
using AkiGames.Events;
using AkiGames.Scripts.Window;
using AkiGames.UI;

namespace AkiGames.Scripts.WindowContentTypes
{
    public class SceneWindowController : WindowController
    {
        private const int SceneContentWidth = 1920;
        private const int SceneContentHeight = 1080;
        private const float MinZoom = 0.5f;
        private const float MaxZoom = 4f;
        private const float ZoomStep = 1.1f;
        private static readonly Color SceneBackgroundColor = Color.White;
        private static readonly Color PrefabBackgroundColor = new(215, 230, 255);

        private UITransform _container;
        private GameObject _containerObject;
        private GameObject _content;
        private SceneGridImage _backgroundImage;
        private Rectangle _prevBounds;
        private float _scaleFactor = 1;
        private float _zoom = 1;
        private float _prevZoom = 1;
        private Vector2 _panOffset = Vector2.Zero;
        private Point? _previousPanMousePosition;
        private bool _isPanning;

        public override void Awake()
        {
            _containerObject  = gameObject.Children[3];
            _container = _containerObject.uiTransform;
            _backgroundImage = _containerObject.Children[0].GetComponent<SceneGridImage>();
            _content  = _containerObject.Children[1];
            base.Awake();
        }
        
        public override void Update()
        {
            bool layoutChanged = _container.Bounds != _prevBounds || _zoom != _prevZoom;
            bool panChanged = UpdatePan();

            if (layoutChanged || panChanged)
            {
                UpdateSceneViewTransform(layoutChanged);
            }

            base.Update();
        }

        public override void OnScroll(int scrollValue)
        {
            int zoomSteps = Math.Max(1, Math.Abs(scrollValue) / 120);
            float zoomFactor = (float)Math.Pow(ZoomStep, zoomSteps);
            if (scrollValue > 0) zoomFactor = 1 / zoomFactor;

            _zoom = Math.Clamp(_zoom * zoomFactor, MinZoom, MaxZoom);
        }

        private bool UpdatePan()
        {
            if (Input.LMB.IsDown)
            {
                _isPanning = PressStartedInSceneContent();
                _previousPanMousePosition = Input.mousePosition;
                return false;
            }

            if (!Input.LMB.IsPressed || !_isPanning)
            {
                _isPanning = false;
                _previousPanMousePosition = null;
                return false;
            }

            Point previousMousePosition = _previousPanMousePosition ?? Input.mousePosition;
            Point currentMousePosition = Input.mousePosition;
            _previousPanMousePosition = currentMousePosition;

            Vector2 delta = new(
                currentMousePosition.X - previousMousePosition.X,
                currentMousePosition.Y - previousMousePosition.Y
            );
            if (delta == Vector2.Zero) return false;

            _panOffset += delta;
            return true;
        }

        private bool PressStartedInSceneContent() =>
            Input.MousePressTarget != null &&
            _container.Contains(Input.mousePosition) &&
            _containerObject.IsParentFor(Input.MousePressTarget);

        private void UpdateSceneViewTransform(bool layoutChanged)
        {
            if (layoutChanged)
            {
                float baseScale = Math.Min(
                    _container.Bounds.Width / (float)SceneContentWidth,
                    _container.Bounds.Height / (float)SceneContentHeight
                );
                _scaleFactor = baseScale * _zoom;
                _content.uiTransform.Width = (int)(SceneContentWidth * _scaleFactor);
                _content.uiTransform.Height = (int)(SceneContentHeight * _scaleFactor);
                _prevBounds = _container.Bounds;
                _prevZoom = _zoom;

                if (_content.Children.Count > 0) RescaleObjectsRecursive(_content.Children[0]);
            }

            _content.uiTransform.OffsetMin = _panOffset;
            if (_backgroundImage != null)
            {
                _backgroundImage.TileScale = _scaleFactor;
                _backgroundImage.TileOffset = _panOffset;
            }

            _content.RefreshBounds();
        }
        
        public void RefreshContent(GameObject gameObjectTree, bool isPrefab = false)
        {
            _backgroundImage?.fillColor = isPrefab ?
                    PrefabBackgroundColor :
                    SceneBackgroundColor;

            foreach (GameObject child in _content.Children)
            {
                child.Dispose();
            }

            _content.Children = [ProcessChildrenRecursive(gameObjectTree)];
            _content.Children[0].AkiGamesAwakeTree();
            _prevBounds = Rectangle.Empty; // чтобы перезагрузить Update
        }

        private GameObject ProcessChildrenRecursive(GameObject objectRealization)
        {
            GameObject objectConcept = new(objectRealization.ObjectName + " (concept)")
            {
                IsActive = objectRealization.IsActive
            };
            UITransform tr = objectRealization.uiTransform.Copy();

            objectConcept.uiTransform = tr;
            objectConcept.AddComponent(tr);
            
            Image image = (Image) objectRealization.GetComponent<Image>()?.Copy();
            if (image != null) objectConcept.AddComponent(image);

            SceneText text = SceneText.From(objectRealization.GetComponent<Text>(), _scaleFactor);
            if (text != null)
            {
                objectConcept.AddComponent(text);
            }

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
            UITransform trRealization = objectConcept.GetComponent<SceneInteractableObject>().source;
            trConcept.OffsetMin = trRealization.OffsetMin * _scaleFactor;
            trConcept.OffsetMax = trRealization.OffsetMax * _scaleFactor;
            trConcept.Width = (int)(trRealization.Width * _scaleFactor);
            trConcept.Height = (int)(trRealization.Height * _scaleFactor);

            SceneText text = objectConcept.GetComponent<SceneText>();
            if (text != null) text.RenderScale = _scaleFactor;

            foreach (GameObject child in objectConcept.Children)
            {
                RescaleObjectsRecursive(child);
            }
        }
    }
}
