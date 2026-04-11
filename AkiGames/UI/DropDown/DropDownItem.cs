using AkiGames.Core;
using Color = AkiGames.Core.Color;

namespace AkiGames.UI.DropDown
{
    public class DropDownItem : GameComponent
    {
        private Image _image = null!;
        private Color _idleColor = new(55, 55, 55, 255);
        private Color _onHoverColor = new(75, 75, 75, 255);
        private DropDown _dropDown = null!;

        public override void Start()
        {
            _image = gameObject.GetComponent<Image>()!;
            _dropDown = gameObject.Parent.Parent.GetComponent<DropDown>()!;
        }
        public override void OnMouseEnter() => _image.fillColor = _onHoverColor;
        public override void OnMouseExit() => _image.fillColor = _idleColor;
        public override void OnMouseUp() => _dropDown?.Hide();
    }
}