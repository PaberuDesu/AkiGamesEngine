using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AkiGames.Events;
using AkiGames.UI;

namespace AkiGames.Scripts.Menu
{
    public class MenuItemController : GameComponent
    {
        private static List<GameObject> _allSubmenus = [];
        private GameObject _submenu = null;
        public Image image = null;
        private Color _idleColor = new(45, 45, 45);
        private Color _onHoverColor = new(65, 65, 65);
        private Color _onOpenedColor = new(75, 75, 75);

        public override void Start()
        {
            _submenu = gameObject.Children[0];
            _allSubmenus.Add(_submenu);
            image = gameObject.GetComponent<Image>();
        }

        internal void Hide()
        {
            _submenu.IsActive = false;
            image.fillColor = _idleColor;
        }
        public override void OnMouseEnter() =>
            image.fillColor = _submenu.IsActive ? _onOpenedColor : _onHoverColor;
        public override void OnMouseExit() =>
            image.fillColor = _submenu.IsActive ? _onOpenedColor : _idleColor;
        public override void OnMouseDown()
        {
            GameObject activeSubmenu = null;
            foreach (GameObject submenu in _allSubmenus)
            {
                if (submenu.IsActive)
                {
                    activeSubmenu = submenu;
                    break;
                }
            }
            // При смене активного подменю закрываем предыдущее
            if (activeSubmenu != null && _submenu != activeSubmenu)
            {
                activeSubmenu.IsActive = false;
                activeSubmenu.Parent.GetComponent<Image>().fillColor = _idleColor;
            }
            // Открываем или закрываем меню
            _submenu.IsActive = !_submenu.IsActive;
            image.fillColor = _submenu.IsActive ? _onOpenedColor : _onHoverColor;
        }
        public override void Deactivate()
        {
            if (!gameObject.IsParentFor(Input.MouseHoverTarget)) Hide();
        }
    }
}