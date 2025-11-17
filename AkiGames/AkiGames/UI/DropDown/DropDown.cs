using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AkiGames.Core;
using AkiGames.Events;

namespace AkiGames.UI.DropDown
{
    public class DropDown : GameComponent
    {
        protected Image image;
        protected Color idleColor = Color.White;
        protected Color onHoverColor = new(200, 200, 200);
        protected Color onOpenedColor = Color.White;

        public List<string> menuItems = [];
        public List<string> MenuItems
        {
            private get => menuItems;
            set
            {
                menuItems = value;
                if (submenuColumn != null && _submenuItemPrefab != null)
                {
                    submenu.Children = [];
                    foreach (string itemName in value)
                    {
                        GameObject submenuItem = _submenuItemPrefab.Copy();
                        submenuItem.ObjectName = itemName;
                        submenuItem.GetComponent<Text>().text = itemName;

                        Events.EventHandler eventHandler = submenuItem.GetComponent<Events.EventHandler>();
                        if (eventHandler != null)
                        {
                            // Сохраняем itemName в локальную переменную для замыкания
                            string currentItem = itemName;
                            eventHandler.OnMouseDownEvent += () => DefaultActionOnChoose?.Invoke(currentItem);
                        }
                        submenu.AddChild(submenuItem);
                    }
                    submenuColumn.Refresh();
                }
            }
        }
        protected GameObject submenu;
        protected Column submenuColumn;
        private static GameObject _submenuPrefab;
        private static GameObject _submenuItemPrefab;

        public Action<string> DefaultActionOnChoose { private get; set; } = (_) => {};

        public override void Awake()
        {
            if (submenu == null || _submenuItemPrefab == null)
            {
                _submenuPrefab ??= Game1.Prefabs["DropDownSubmenu"];
                _submenuItemPrefab ??= Game1.Prefabs["DropDownSubmenuItem"];
            }

            submenu = _submenuPrefab.Copy();
            submenuColumn = submenu.GetComponent<Column>();
            gameObject.AddChild(submenu);
            image = gameObject.GetComponent<Image>();

            // создаем выпадающее меню по пунктам, если пункты были заданы до префабов меню
            if (MenuItems.Count > 0) MenuItems = menuItems;
        }

        internal void Hide()
        {
            submenu.IsActive = false;
            image.fillColor = idleColor;
        }
        public override void OnMouseEnter() =>
            image.fillColor = submenu.IsActive ? onOpenedColor : onHoverColor;
        public override void OnMouseExit() =>
            image.fillColor = submenu.IsActive ? onOpenedColor : idleColor;
        public override void OnMouseDown()
        {
            // Открываем или закрываем меню
            submenu.IsActive = !submenu.IsActive;
            image.fillColor = submenu.IsActive ? onOpenedColor : onHoverColor;
        }
        public override void Deactivate()
        {
            if (!gameObject.IsParentFor(Input.MouseHoverTarget)) Hide();
        }
    }
}