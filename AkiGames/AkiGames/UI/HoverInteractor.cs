using Microsoft.Xna.Framework;

namespace AkiGames.UI
{
    public class HoverInteractor : GameComponent
    {
        private Image _image;
        private Color _idleColor = new(55, 55, 55);
        private Color _onHoverColor = new(75, 75, 75);

        public override void Start() => _image = gameObject.GetComponent<Image>();
        public override void OnMouseEnter() => _image.fillColor = _onHoverColor;
        public override void OnMouseExit() => _image.fillColor = _idleColor;
    }
}