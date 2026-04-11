using AkiGames.Core;
using AkiGames.Events;
using EventHandler = AkiGames.Events.EventHandler;

namespace AkiGames.UI.DropDown
{
    public class DropDown : InteractableComponent
    {
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
                        Text textComp = submenuItem.GetComponent<Text>()!;
                        if (textComp != null) textComp.text = itemName;

                        EventHandler eventHandler = submenuItem.GetComponent<EventHandler>()!;
                        if (eventHandler != null)
                        {
                            string currentItem = itemName;
                            eventHandler.OnMouseDownEvent += () => ActionOnChoose?.Invoke(currentItem);
                        }
                        submenu.AddChild(submenuItem);
                    }
                    submenuColumn.Refresh();
                }
            }
        }
        protected GameObject submenu = null!;
        protected Column submenuColumn = null!;
        private static GameObject? _submenuPrefab;
        private static GameObject? _submenuItemPrefab;

        public Action<string> ActionOnChoose { private get; set; } = (_) => {};

        public override void Awake()
        {
            // Загружаем префабы из VeldridGame.Prefabs (аналог Game1.Prefabs)
            if (_submenuPrefab == null && !VeldridGame.Prefabs.TryGetValue("DropDownSubmenu", out _submenuPrefab))
                Console.WriteLine("Warning: Prefab 'DropDownSubmenu' not found");
            if (_submenuItemPrefab == null && !VeldridGame.Prefabs.TryGetValue("DropDownSubmenuItem", out _submenuItemPrefab))
                Console.WriteLine("Warning: Prefab 'DropDownSubmenuItem' not found");

            if (_submenuPrefab == null || _submenuItemPrefab == null)
            {
                Console.WriteLine("Failed to load DropDown prefabs, component will not work");
                return;
            }

            submenu = _submenuPrefab.Copy();
            submenuColumn = submenu.GetComponent<Column>()!;
            gameObject.AddChild(submenu);
            image = gameObject.GetComponent<Image>()!;

            if (MenuItems.Count > 0) MenuItems = menuItems;
        }

        internal void Hide()
        {
            submenu.IsActive = false;
            isRedacting = false;
            if (image != null) image.fillColor = idleColor;
        }

        public override void OnMouseDown()
        {
            submenu.IsActive = !submenu.IsActive;
            isRedacting = !isRedacting;
            if (image != null)
                image.fillColor = submenu.IsActive ? onOpenedColor : onHoverColor;
        }

        public override void Deactivate()
        {
            if (!gameObject.IsParentFor(Input.MouseHoverTarget)) Hide();
        }
    }
}