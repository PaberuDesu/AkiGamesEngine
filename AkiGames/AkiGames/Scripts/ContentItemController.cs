using System;
using AkiGames.UI;
using AkiGames.UI.ScrollableList;
using Microsoft.Xna.Framework;

namespace AkiGames.Scripts
{
    public class ContentItemController : GameComponent
    {
        private string name = "";
        public string Name
        {
            get => Title != null ? Title.text : name;
            set
            {
                if (Title != null) Title.text = value;
                else name = value;
            }
        }

        private event Action<string> ActionOnDoubleClick;
        public void SetActionOnDoubleClick(Action<string> func) => ActionOnDoubleClick = func;

        protected Text Title;
        protected Image RowImage;
        private ScrollableListController _list;

        private Color _baseColor;
        private Color _hoveredColor = new(40,40,50);

        public override void Awake()
        {
            Title = gameObject.Children[1].GetComponent<Text>();
            Title.text = name;
            RowImage = gameObject.GetComponent<Image>();
            _list = gameObject.Parent.GetComponent<ScrollableListController>();
            _baseColor = RowImage.fillColor;
        }

        public override void OnMouseEnter()
        {
            if (RowImage.fillColor == _baseColor)
                RowImage.fillColor = _hoveredColor;
        }

        public override void OnMouseExit()
        {
            if (RowImage.fillColor == _hoveredColor)
                RowImage.fillColor = _baseColor;
        }

        public override void OnMouseDown() => SelectItem();
        public override void OnDoubleClick() => ActionOnDoubleClick?.Invoke(Name);
        public override void Deactivate() => _list?.ChooseItem(null);
        public override void OnScroll(int scrollValue) => gameObject.Parent.OnScroll(scrollValue);

        protected void SelectItem() => _list?.ChooseItem(RowImage);
    }
}
