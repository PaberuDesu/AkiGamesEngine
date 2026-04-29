using System.IO;
using System.Collections.Generic;
using AkiGames.Core;
using AkiGames.Scripts.Window;
using AkiGames.UI;
using AkiGames.UI.ScrollableList;
using AkiGames.Events;

namespace AkiGames.Scripts.WindowContentTypes
{
    public class HierarchyWindowController : WindowController
    {
        internal ScrollableListController contentList {get; private set;}
        private static string _gamePath = null;
        private static HierarchyWindowController _activeController = null;
        private HashSet<GameObject> _openedObjects = [];
        private bool _showRootObject = false;

        private GameWindowController _gameWindow;
        private SceneWindowController _sceneWindow;

        public override void Awake()
        {
            _activeController = this;
            contentList = scrollableContent.GetComponent<ScrollableListController>();
            
            _gameWindow = gameObject.Parent.Children[0].GetComponent<GameWindowController>();
            _sceneWindow = gameObject.Parent.Children[2].GetComponent<SceneWindowController>();
            base.Awake();
        }

        public static void ApplyInspectorChanges() =>
            _activeController?.RefreshViewsAfterInspectorChange();

        private void RefreshViewsAfterInspectorChange()
        {
            if (Game1.editableGameMainObject == null) return;

            Game1.editableGameMainObject.EnsureUniqueObjectIdsInTree();
            RefreshContent(Game1.editableGameMainObject);
            _sceneWindow?.RefreshContent(Game1.editableGameMainObject, _showRootObject);
            _gameWindow?.RefreshContent(Game1.editableGameMainObject);
        }

        public void RefreshContent(
            GameObject gameMainObject,
            string fullPath = "",
            bool? showRootObject = null
        )
        {
            bool openedNewFile = fullPath != "" && fullPath != _gamePath;
            if (fullPath != "") _gamePath = fullPath;
            if (showRootObject.HasValue) _showRootObject = showRootObject.Value;

            if (openedNewFile)
                _openedObjects.Clear();
            else
                SaveOpenedState();

            if (openedNewFile && _showRootObject)
                _openedObjects.Add(gameMainObject);
            
            ClearContentItems();
            if (_showRootObject)
            {
                HierarchyListItem rootItem = CreateHierarchyItem(gameMainObject, null, 0, true);
                ProcessChildrenRecursive(gameMainObject, rootItem, 1);
            }
            else
            {
                ProcessChildrenRecursive(gameMainObject, null, 0);
            }
            RestoreOpenedState();
            RefreshListLayout(openedNewFile);
        }

        private void ClearContentItems()
        {
            foreach (GameObject child in contentList.gameObject.Children)
            {
                child.Dispose();
            }

            contentList.gameObject.Children = [];
        }

        private void RefreshListLayout(bool resetScroll)
        {
            contentList.Refresh();
            gameObject.RefreshBounds();

            if (resetScroll)
                contentList.ScrollToTop();

            contentList.Update();
            gameObject.RefreshBounds();
        }

        private void ProcessChildrenRecursive(GameObject objectRealization, HierarchyListItem descriptingParentObject, int level)
        {
            if (objectRealization.Children.Count > 0)
            {
                foreach (GameObject childRealization in objectRealization.Children)
                {
                    HierarchyListItem itemController = CreateHierarchyItem(
                        childRealization,
                        descriptingParentObject,
                        level,
                        level == 0
                    );
                    ProcessChildrenRecursive(childRealization, itemController, level + 1);
                }
            }
        }

        private HierarchyListItem CreateHierarchyItem(
            GameObject objectRealization,
            HierarchyListItem descriptingParentObject,
            int level,
            bool isActive
        )
        {
            GameObject descriptorPrefabCopy = Game1.Prefabs["HierarchyContentItem"].Copy();
            descriptorPrefabCopy.IsActive = isActive;
            HierarchyListItem itemController = new()
            {
                Name = new string(' ', level * 3) + objectRealization.ObjectName,
                scrollableList = contentList,
                RepresentedObject = objectRealization,
                Level = level
            };
            itemController.SetActionOnDoubleClick((_) => { InspectorWindowController.LoadFor(objectRealization); });
            descriptorPrefabCopy.AddComponent(itemController);
            contentList.gameObject.AddChild(descriptorPrefabCopy);
            descriptingParentObject?.childItems.Add(itemController);
            return itemController;
        }

        private void SaveOpenedState()
        {
            _openedObjects.Clear();
            // Рекурсивно обходим все текущие элементы иерархии
            SaveOpenedStateRecursive(contentList.gameObject.Children);
        }

        private void SaveOpenedStateRecursive(List<GameObject> items)
        {
            foreach (GameObject item in items)
            {
                HierarchyListItem listItem = item.GetComponent<HierarchyListItem>();
                if (listItem != null && listItem.RepresentedObject != null && listItem.IsOpened)
                {
                    _openedObjects.Add(listItem.RepresentedObject);
                }
                // Проверяем дочерние элементы
                if (item.Children.Count > 0)
                    SaveOpenedStateRecursive(item.Children);
            }
        }

        private void RestoreOpenedState()
        {
            // После перестроения обходим все созданные элементы и открываем те, чей RepresentedObject есть в сохранённом наборе
            RestoreOpenedStateRecursive(contentList.gameObject.Children);
        }

        private void RestoreOpenedStateRecursive(List<GameObject> items)
        {
            foreach (GameObject item in items)
            {
                HierarchyListItem listItem = item.GetComponent<HierarchyListItem>();
                if (listItem != null && listItem.RepresentedObject != null
                    && _openedObjects.Contains(listItem.RepresentedObject) && !listItem.IsOpened)
                {
                    // Открываем узел, если он ещё не открыт
                    item.Children[0]?.GetComponent<HierarchyExpander>()?
                        .ShowOrHideChildren();
                        
                    // Рекурсивно для дочерних элементов
                    if (item.Children.Count > 0)
                        RestoreOpenedStateRecursive(item.Children);
                }
            }
        }

        public void ShowChildrenOf(HierarchyListItem item, bool toOpen)
        {
            if (item.childItems.Count == 0) return;

            // Используем стек для итеративного обхода иерархии
            Stack<HierarchyListItem> stack = new();

            // Добавляем непосредственных детей в стек
            foreach (HierarchyListItem child in item.childItems)
            {
                child.gameObject.IsActive = toOpen;
                stack.Push(child);
            }

            // Обрабатываем все элементы в стеке
            while (stack.Count > 0)
            {
                HierarchyListItem current = stack.Pop();

                // Показываем/скрываем детей текущего элемента
                foreach (HierarchyListItem child in current.childItems)
                {
                    child.gameObject.IsActive = current.gameObject.IsActive &&
                                                current.IsOpened && toOpen;

                    stack.Push(child);
                }
            }

            contentList.Refresh();
        }

        public void UpdateScene() // когда изменили иерархию сцены, обновляем окна иерархии и сцены
        {
            // Обновляем отображение иерархии
            if (Game1.editableGameMainObject == null) return;
            Game1.editableGameMainObject.EnsureUniqueObjectIdsInTree();
            RefreshContent(Game1.editableGameMainObject); // RootGameObject
            gameObject.RefreshBounds();

            // Обновляем окно сцены
            _sceneWindow?.RefreshContent(Game1.editableGameMainObject, _showRootObject);
            _gameWindow?.RefreshContent(Game1.editableGameMainObject);
        }

        public override void ProcessHotkey(Input.HotKey hotkey)
        {
            if (Input.Hotkey == Input.HotKey.CtrlS)
                SaveHierarchy();
        }

        public static void SaveHierarchy()
        {
            if (_gamePath != null && Game1.editableGameMainObject != null)
            {
                string jsonString = JsonProjectSerializer.SerializeToJson(Game1.editableGameMainObject);
                File.WriteAllText(_gamePath, jsonString);
            }
        }
    }
}
