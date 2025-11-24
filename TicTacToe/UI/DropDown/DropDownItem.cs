using Microsoft.Xna.Framework;

namespace AkiGames.UI.DropDown
{
    public class DropDownItem : GameComponent
    {
        private Image _image;
        private Color _idleColor = new(55, 55, 55);
        private Color _onHoverColor = new(75, 75, 75);
        private DropDown _dropDown;

        public override void Start()
        {
            _image = gameObject.GetComponent<Image>();
            _dropDown = gameObject.Parent.Parent.GetComponent<DropDown>();
        }
        public override void OnMouseEnter() => _image.fillColor = _onHoverColor;
        public override void OnMouseExit() => _image.fillColor = _idleColor;
        public override void OnMouseUp() => _dropDown?.Hide();
    }
}