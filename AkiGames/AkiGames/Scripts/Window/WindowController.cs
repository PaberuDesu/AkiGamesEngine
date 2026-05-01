using System.Collections.Generic;
using AkiGames.UI.ScrollableList;

namespace AkiGames.Scripts.Window
{
    public abstract class WindowController : WindowComponent
    {
        public GameObject scrollableContent = null;
        public override void Awake()
        {
            foreach (GameObject child in gameObject.Children)
                MarkChildrenAsWindowComponents(child);

            base.Awake();
        }

        protected ScrollableListController ResolveScrollableContent()
        {
            ScrollableListController controller = scrollableContent?.GetComponent<ScrollableListController>();
            if (controller != null) return controller;

            controller = FindScrollableListController(scrollableContent);
            if (controller != null)
                scrollableContent = controller.gameObject;

            return controller;
        }

        private static ScrollableListController FindScrollableListController(GameObject root)
        {
            if (root == null) return null;

            ScrollableListController controller = root.GetComponent<ScrollableListController>();
            if (controller != null) return controller;

            foreach (GameObject child in root.Children)
            {
                controller = FindScrollableListController(child);
                if (controller != null) return controller;
            }

            return null;
        }

        internal void BringToFront()
        {
            List<GameObject> windowsNew = [];
            foreach (GameObject window in gameObject.Parent.Children)
            {
                if (window != gameObject) windowsNew.Add(window);
            }
            windowsNew.Add(gameObject);
            gameObject.Parent.Children = windowsNew;
        }

        public override void OnScroll(int scrollValue) => scrollableContent?.OnScrollFromOutsideTheObject(scrollValue);
    }
}
