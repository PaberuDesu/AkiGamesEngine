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
        private HashSet<GameObject> _openedObjects = [];


        GameObject contentObject;

        private SceneWindowController _sceneWindow;

        public override void Awake()
        {
            contentObject = gameObject.Children[3];
            scrollableContent = contentObject.Children[0].Children[0];
            contentList = scrollableContent.GetComponent<ScrollableListController>();
            
            _sceneWindow = gameObject.Parent.Children[2].GetComponent<SceneWindowController>();
            base.Awake();
        }

        public void RefreshContent(GameObject gameMainObject, string fullPath = "")
        {
            if (fullPath != "") _gamePath = fullPath;

            SaveOpenedState();
            
            contentList.gameObject.Children = [];
            ProcessChildrenRecursive(gameMainObject, null, 0);
            contentList.Refresh();

            RestoreOpenedState();
        }

        private void ProcessChildrenRecursive(GameObject objectRealization, HierarchyListItem descriptingParentObject, int level)
        {
            if (objectRealization.Children.Count > 0)
            {
                foreach (GameObject childRealization in objectRealization.Children)
                {
                    GameObject descriptorPrefabCopy = Game1.Prefabs["HierarchyContentItem"].Copy();
                    descriptorPrefabCopy.IsActive = level == 0;
                    HierarchyListItem itemController = new()
                    {
                        Name = new string(' ', level * 3) + childRealization.ObjectName,
                        scrollableList = contentList,
                        RepresentedObject = childRealization,
                        Level = level
                    };
                    itemController.SetActionOnDoubleClick((_) => { InspectorWindowController.LoadFor(childRealization); });
                    descriptorPrefabCopy.AddComponent(itemController);
                    contentList.gameObject.AddChild(descriptorPrefabCopy);
                    descriptingParentObject?.childItems.Add(itemController);
                    ProcessChildrenRecursive(childRealization, itemController, level + 1);
                }
            }
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
            RefreshContent(Game1.gameMainObject.Children[0]); // RootGameObject
            gameObject.RefreshBounds();

            // Обновляем окно сцены
            _sceneWindow?.RefreshContent(Game1.gameMainObject.Children[0]);
        }

        public override void ProcessHotkey(Input.HotKey hotkey)
        {
            if (Input.Hotkey == Input.HotKey.CtrlS)
                SaveHierarchy();
        }

        public static void SaveHierarchy()
        {
            if (_gamePath != null)
            {
                string jsonString = JsonProjectSerializer.SerializeToJson(Game1.gameMainObject.Children[0]);
                File.WriteAllText(_gamePath, jsonString);
            }
        }
    }
}