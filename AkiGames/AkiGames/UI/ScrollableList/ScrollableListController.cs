using System;
using Microsoft.Xna.Framework;
using AkiGames.Core;
using AkiGames.Events;

namespace AkiGames.UI.ScrollableList
{
    public class ScrollableListController : Column
    {
        private Image _chosenItem = null;
        private Color _highlightColor = new Color(60, 60, 80);

        private Scrollbar _scrollbar;
        public int? scrollValueThisFrame = null;
        private int _maxScroll = 0;

        private int _parentHeight = 0;
        private int _prevListLength = 0;
        private UITransform _parentTransform;

        [DontSerialize, HideInInspector] public int PixelOffset => _scrollbar.Offset * (itemHeight+Spacing);

        [DontSerialize, HideInInspector] public bool IsLimitReached => _scrollbar.IsLimitReached;
        public void ScrollToTop() => _scrollbar.ScrollToTop();
        public void ScrollToBottom() => _scrollbar.ScrollToBottom();

        public override void Awake()
        {
            _scrollbar = gameObject.Parent.Children[1].GetComponent<Scrollbar>();
            _scrollbar.ScrollStep = itemHeight;
            _parentTransform = gameObject.Parent.uiTransform;
        }

        internal void SetStartIndex()
        {
            int offsetPixels = PixelOffset;
            int accumulatedHeight = 0;
            for (int i = 0; i < gameObject.Children.Count; i++)
            {
                if (!gameObject.Children[i].IsActive) continue;

                accumulatedHeight += gameObject.Children[i].uiTransform.Height + Spacing;
                if (accumulatedHeight > offsetPixels) break;
            }

            gameObject.uiTransform.OffsetMin = new Vector2(0, -offsetPixels);
            gameObject.RefreshBounds();
        }

        internal void ChooseItem(Image image)
        {
            Recolor(Color.Transparent);
            _chosenItem = image;
            Recolor(_highlightColor);
        }

        private void Recolor(Color color)
        {
            if (_chosenItem != null) _chosenItem.fillColor = color;
        }

        public override void Update()
        {
            scrollValueThisFrame = null;
            int listHeight = _listHeight;
            if (_parentHeight != _parentTransform.Bounds.Height || _prevListLength != listHeight)
            {//при изменении размера или изменении длины списка элементов
                _parentHeight = _parentTransform.Bounds.Height;
                _prevListLength = listHeight;

                _maxScroll = (int)Math.Ceiling((listHeight - _parentHeight) / (float)itemHeight);
                _scrollbar.UpdateItems(listHeight, _parentHeight, _maxScroll);
                uiTransform.OffsetMax = new Vector2
                (
                    _scrollbar.gameObject.IsActive ? 15 : 0,
                    0
                );
                SetStartIndex();
            }
        }

        private int _listHeight
        {
            get
            {
                int accumulatedHeight = 0;
                for (int i = 0; i < gameObject.Children.Count; i++)
                {
                    if (!gameObject.Children[i].IsActive) continue;

                    accumulatedHeight += gameObject.Children[i].uiTransform.Height + Spacing;
                }
                return accumulatedHeight;
            }
        }

        public int ActiveMembers
        {
            get
            {
                int count = 0;
                for (int i = 0; i < gameObject.Children.Count; i++)
                {
                    if (gameObject.Children[i].IsActive) count++;
                }
                return count;
            }
        }

        public override void OnScroll(int scrollValue)
        {
            if (gameObject.Parent.uiTransform.Contains(Input.mousePosition))//if list mask contains cursor
            {
                Scroll(scrollValue);
            }
        }

        public override void OnScrollFromOutsideTheObject(int scrollValue) => Scroll(scrollValue);

        public void Scroll(int scrollValue)
        {
            if (scrollValueThisFrame != null) return;
            int offsetPrev = _scrollbar.Offset;
            _scrollbar.Offset = scrollValue > 0 ?
                Math.Min(_maxScroll, _scrollbar.Offset + 1) :
                Math.Max(0, _scrollbar.Offset - 1);
            scrollValueThisFrame = _scrollbar.Offset - offsetPrev;
        }
    }
}
