using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AkiGames.UI;
using AkiGames.UI.ScrollableList;

namespace AkiGames.Scripts
{
    public class HierarchyListItem() : ContentItemController
    {
        private GameObject transporter = null;
        private bool _isDragging = false;

        private GameObject _hierarchyList;
        internal ScrollableListController scrollableList;

        internal List<HierarchyListItem> childItems = [];
        private HierarchyExpander _opener = null;
        internal bool IsOpened => _opener?.isOpened ?? false;

        public override void Start()
        {
            _hierarchyList = gameObject.Parent;

            _opener = gameObject.Children[0].GetComponent<HierarchyExpander>();
            _opener.gameObject.IsActive = childItems.Count > 0;
        }

        public override void OnRMBUp()
        {
            //TODO: menu (rename, delete)
        }

        public void StartDrag()
        {
            _isDragging = true;
            
            if (transporter is null)
            {
                transporter = new("Transporter");
                transporter.uiTransform.LocalBounds = uiTransform.LocalBounds;
                transporter.uiTransform.Height = 2;
                transporter.uiTransform.VerticalAlignment = UITransform.AlignmentV.Top;
                transporter.AddComponent(new Image{fillColor = Color.RoyalBlue});
            }
            _hierarchyList.AddChild(transporter);
        }

        public override void OnMouseUpOutside() => OnMouseUp();

        public override void OnMouseUp()
        {
            if (_isDragging)
            {
                _isDragging = false;

                _hierarchyList.RemoveChild(transporter);
            }
        }

        public override void Drag(Vector2 cursorPosOnObj)
        {
            if (!_isDragging) StartDrag();

            int spacing = scrollableList.Spacing;
            int mySize = uiTransform.Bounds.Height+spacing;

            Rectangle hierarchyListRect = _hierarchyList.uiTransform.Bounds;
            int parentOffsetY = hierarchyListRect.Y;

            // расчет координат полоски для вставки (располагается между элементами списка)
            int positionY = Math.Clamp(
                Events.Input.mousePosition.Y - parentOffsetY,
                0, (scrollableList.ActiveMembers-1) * mySize);
            int roundedCoordinate = (int) Math.Round((double)positionY / mySize) * mySize - spacing;
            if (roundedCoordinate < 0) roundedCoordinate = 0;

            // Автоскролл
            Rectangle visibleContentRect = _hierarchyList.Parent.uiTransform.Bounds;
            float relativeY = (Events.Input.mousePosition.Y - visibleContentRect.Y) / (float)visibleContentRect.Height;
            if (relativeY < 0.2)
                scrollableList.Scroll((int)(-3 * Math.Pow((0.2 - relativeY)*5,3)));
            if (relativeY > 0.8)
                scrollableList.Scroll((int)(3 * Math.Pow((relativeY - 0.8)*5,3)));
            
            transporter.uiTransform.OffsetMin = new(0, roundedCoordinate);
            transporter.RefreshBounds();
        }
    }
}