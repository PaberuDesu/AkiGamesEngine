using Microsoft.Xna.Framework;
using AkiGames.UI;

namespace AkiGames.Scripts.Menu
{
    public class SubmenuItemController : GameComponent
    {
        private Image _image = null;
        private Color _idleColor = new(55, 55, 55);
        private Color _onHoverColor = new(75, 75, 75);
        private MenuItemController _menuItem;

        public override void Start()
        {
            _image = gameObject.GetComponent<Image>();
            _menuItem = gameObject.Parent.Parent.GetComponent<MenuItemController>();
        }
        public override void OnMouseEnter() => _image.fillColor = _onHoverColor;
        public override void OnMouseExit() => _image.fillColor = _idleColor;
        public override void OnMouseUp() => _menuItem?.Hide();
    }
}