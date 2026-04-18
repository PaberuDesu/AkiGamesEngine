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
            private get => _title != null ? _title.text : name;
            set
            {
                if (_title != null) _title.text = value;
                else name = value;
            }
        }

        private event Action<string> ActionOnDoubleClick;
        public void SetActionOnDoubleClick(Action<string> func) => ActionOnDoubleClick = func;

        private Text _title;
        private Image _image;
        private static ScrollableListController _list;

        private Color _baseColor;
        private Color _hoveredColor = new(40,40,50);

        public override void Awake()
        {
            _title = gameObject.Children[1].GetComponent<Text>();
            _title.text = name;
            _image = gameObject.GetComponent<Image>();
            _list ??= gameObject.Parent.GetComponent<ScrollableListController>();
            _baseColor = _image.fillColor;
        }

        public override void OnMouseEnter()
        {
            if (_image.fillColor == _baseColor)
                _image.fillColor = _hoveredColor;
        }

        public override void OnMouseExit()
        {
            if (_image.fillColor == _hoveredColor)
                _image.fillColor = _baseColor;
        }

        public override void OnMouseDown() => _list.ChooseItem(_image);
        public override void OnDoubleClick() => ActionOnDoubleClick?.Invoke(Name);
        public override void Deactivate() => _list.ChooseItem(null);
        public override void OnScroll(int scrollValue) => gameObject.Parent.OnScroll(scrollValue);
    }
}