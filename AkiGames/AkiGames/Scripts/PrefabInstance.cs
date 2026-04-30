using System.Collections.Generic;
using AkiGames.Core;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI;
using AkiGames.UI.ScrollableList;

namespace AkiGames.Scripts
{
    public class PrefabInstance : GameComponent
    {
        public string PrefabName = "";
        public int ScrollableListSpacing = 0;

        private bool _instantiated = false;

        public override void Awake()
        {
            if (_instantiated) return;
            _instantiated = true;

            if (string.IsNullOrWhiteSpace(PrefabName)) return;
            if (!Game1.Prefabs.TryGetValue(PrefabName, out GameObject prefab))
            {
                ConsoleWindowController.Log($"Prefab {PrefabName} can't be instantiated: prefab wasn't found.");
                return;
            }

            List<GameObject> existingChildren = gameObject.Children;
            foreach (GameObject child in existingChildren)
                child.Parent = null;

            gameObject.Children = [];
            CopyRootComponents(prefab);

            foreach (GameObject child in prefab.Children)
                gameObject.AddChild(child.Copy());

            ScrollableListController scrollableList = FindScrollableListController(gameObject);
            if (scrollableList != null)
                scrollableList.Spacing = ScrollableListSpacing;

            GameObject contentList = scrollableList?.gameObject ?? gameObject;
            foreach (GameObject child in existingChildren)
                contentList.AddChild(child);
        }

        private void CopyRootComponents(GameObject prefab)
        {
            foreach (GameComponent component in prefab.Components)
            {
                if (component is UITransform || component is PrefabInstance)
                    continue;

                if (gameObject.GetComponent(component.GetType()) != null)
                    continue;

                gameObject.AddComponent(component.Copy());
            }
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
    }
}
