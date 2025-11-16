using System.Linq;
using System.Reflection;
using AkiGames.Events;
using AkiGames.Scripts.WindowContentTypes;//TODO: delete
using AkiGames.UI;
using AkiGames.UI.ScrollableList;

namespace AkiGames.Scripts.Window
{
    public class WindowComponent : GameComponent
    {
        private WindowController _windowController = null;

        public override void Awake()
        {
            GameObject window = gameObject.GetAncestry()[2];
            _windowController = window.GetComponent<WindowController>();
            gameObject.ChildAdded += MarkChildrenAsWindowComponents;
        }

        protected static void MarkChildrenAsWindowComponents(GameObject child)
        {
            if (child.GetComponent<WindowComponent>() is null)
                child.AddComponent(new WindowComponent());
            foreach (GameObject grandchild in child.Children)
            {
                MarkChildrenAsWindowComponents(grandchild);
            }
        }

        public override void OnMouseDown() => _windowController?.BringToFront();
        public override void OnScroll(int scrollValue)
        {
            if (!IsScrollable) _windowController?.OnScroll(scrollValue);
        }

        public override void ProcessHotkey(Input.HotKey hotkey) => _windowController?.ProcessHotkey(hotkey);

        private bool IsScrollable => gameObject.Components.Any(component =>
            {
                // Получаем метод OnScroll из типа элемента
                MethodInfo componentOnScroll = component.GetType().GetMethod("OnScroll");
                // Проверяем, что метод существует и не объявлен в WindowComponent, GameStructure, ScrollableListController, попытка листать который не удалась или который и листается в случае, когда листать больше нечего
                return componentOnScroll != null &&
                       componentOnScroll.DeclaringType != typeof(WindowComponent) &&
                       componentOnScroll.DeclaringType != typeof(GameStructure) &&
                       !(componentOnScroll.DeclaringType == typeof(ScrollableListController) &&
                            (((ScrollableListController)component).scrollValueThisFrame == null ||
                                ((ScrollableListController)component).scrollValueThisFrame == 0 && _windowController?.scrollableContent != gameObject));
            }
        );
    }
}