using System;
using Microsoft.Xna.Framework;
using AkiGames.Events;

namespace AkiGames.UI.ScrollableList
{
    public class Thumb : GameComponent
    {
        private Image _image;
        private UITransform _scrollbarTransform;
        private Scrollbar _scrollbar;
        private bool _isDragging = false;

        public override void Awake()
        {
            _image = gameObject.GetComponent<Image>();
            _scrollbarTransform = gameObject.Parent.uiTransform;
            _scrollbar = _scrollbarTransform.gameObject.GetComponent<Scrollbar>();
        }

        internal void Refresh(int offset, int maxOffset, float listHeight, int maskHeight)
        {
            // Размер ползунка
            float visibleRatio = listHeight == 0 ? 0 : maskHeight / listHeight;
            uiTransform.Height = Math.Max((int)(visibleRatio * _scrollbarTransform.Bounds.Height), 0);
            // Позиция ползунка
            int maxScroll = maxOffset;
            int trackHeight = _scrollbarTransform.Bounds.Height - uiTransform.Height;
            float scrollPercent = maxScroll > 0 ? offset / (float)maxScroll : 0;
            uiTransform.OffsetMin = new Vector2(0, (int)(scrollPercent * trackHeight));

            gameObject.RefreshBounds(_scrollbarTransform);
        }

        public override void OnMouseEnter() => _image.fillColor = Color.DarkGray;

        public override void OnMouseExit()
        {
            if (!_isDragging) _image.fillColor = Color.Gray;
        }

        public override void OnMouseDown()
        {
            _image.fillColor = Color.LightGray;
            _isDragging = true;
        }

        public override void OnMouseUp()
        {
            _image.fillColor = Color.DarkGray;
            _isDragging = false;
        }

        public override void OnMouseUpOutside()
        {
            _image.fillColor = Color.Gray;
            _isDragging = false;
        }

        public override void Drag(Vector2 cursorPosOnObj)
        {
            int newY = Input.mousePosition.Y - (int)cursorPosOnObj.Y;

            _scrollbar.SetOffsetByPosition(newY);
        }
    }
}