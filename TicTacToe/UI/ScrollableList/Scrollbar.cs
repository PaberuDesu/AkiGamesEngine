using System;
using AkiGames.Events;

namespace AkiGames.UI.ScrollableList
{
    public class Scrollbar : GameComponent
    {
        private ScrollableListController _listController;
        private Thumb _thumb;

        private int _listHeight = 0;
        private int _maskHeight = 0;
        public float ScrollStep { private get; set; }

        private int _offset = 0;
        private int _maxOffset = 0;
        internal int Offset
        {
            get => _offset;
            set
            {
                _offset = Math.Clamp(value, 0, _maxOffset);
                _listController.SetStartIndex();
                _listController.gameObject.RefreshBounds();
                RefreshThumb();
            }
        }
        internal bool IsLimitReached => _offset == _maxOffset || !gameObject.IsActive;
        internal void ScrollToTop() => Offset = 0;
        internal void ScrollToBottom() => Offset = _maxOffset;

        internal void SetOffsetByPosition(float newY) =>
            Offset = (int)((newY - uiTransform.Bounds.Y) / uiTransform.Bounds.Height * (_listHeight / ScrollStep));

        public override void Awake()
        {
            _listController = gameObject.Parent.Children[0].GetComponent<ScrollableListController>();
            _thumb = gameObject.Children[0].GetComponent<Thumb>();
        }

        internal void UpdateItems(int listHeight, int maskHeight, int scrollSteps)
        {
            if (scrollSteps <= 0)
            {
                _maxOffset = 0;
                gameObject.IsActive = false;
                Offset = 0;
                RefreshThumb();
                return;
            }

            _maxOffset = scrollSteps;
            _listHeight = listHeight;
            _maskHeight = maskHeight;

            gameObject.IsActive = true;
            Offset = _offset;
            RefreshThumb();
        }

        private void RefreshThumb() => _thumb.Refresh(Offset, _maxOffset, _listHeight, _maskHeight);

        public override void OnMouseDown()
        {
            float newThumbOffset = Input.mousePosition.Y - uiTransform.Bounds.Y - (0.5f * _thumb.uiTransform.Height);
            int trackHeight = uiTransform.Bounds.Height - _thumb.uiTransform.Height;
            float scrollPercent = newThumbOffset / trackHeight;
            Offset = (int)(scrollPercent * _maxOffset);
            RefreshThumb();
            _thumb.OnMouseDown();
            Input.MouseHoverTarget = _thumb.gameObject;
            Input.StartPressing();
        }
    }
}
