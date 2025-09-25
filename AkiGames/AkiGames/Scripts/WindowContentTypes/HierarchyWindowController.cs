using System.IO;
using System.Text.Json;
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
        private ScrollableListController _contentList;
        private static string _gamePath = null;
        private static GameObject _gameMainObject = null;


        GameObject contentObject;

        public override void Awake()
        {
            contentObject = gameObject.Children[3];
            scrollableContent = contentObject.Children[0].Children[0];
            _contentList = scrollableContent.GetComponent<ScrollableListController>();
            base.Awake();
        }

        public void RefreshContent(string fullPath)
        {
            InspectorWindowController.LoadFor(null);// clearing obj description
            _gamePath = fullPath;

            // Получаем иерархию объектов
            JsonElement akiContent = JsonSerializer.Deserialize<JsonElement>(File.ReadAllText(fullPath));
            _gameMainObject = JsonProjectSerializer.LoadFromJson(akiContent);
            // Десериализуем данные
            string jsonText = File.ReadAllText(fullPath);
            using JsonDocument document = JsonDocument.Parse(jsonText);
            JsonElement rootElement = document.RootElement;

            _contentList.gameObject.Children = [];

            if (rootElement.ValueKind == JsonValueKind.Object)
            {
                ProcessChildrenRecursive(rootElement, null, 0);
            }

            _contentList.Refresh();
        }

        private void ProcessChildrenRecursive(JsonElement parentJsonElem, HierarchyListItem parentObject, int level)
        {
            if (parentJsonElem.TryGetProperty("Children", out JsonElement childrenElement) &&
                childrenElement.ValueKind == JsonValueKind.Array)
            {
                foreach (JsonElement childElement in childrenElement.EnumerateArray())
                {
                    if (childElement.TryGetProperty("ObjectName", out JsonElement nameElement) &&
                        nameElement.ValueKind == JsonValueKind.String)
                    {
                        GameObject prefabCopy = Game1.Prefabs["HierarchyContentItem"].Copy();
                        prefabCopy.IsActive = level == 0;
                        HierarchyListItem itemController = new()
                        {
                            Name = new string(' ', level * 3) + nameElement.GetString()
                        };
                        GameObject describableObj = JsonProjectSerializer.ParseGameObject(childElement);
                        itemController.SetActionOnDoubleClick((_) => { InspectorWindowController.LoadFor(describableObj); });
                        prefabCopy.AddComponent(itemController);
                        _contentList.gameObject.AddChild(prefabCopy);
                        parentObject?.childItems.Add(itemController);
                        ProcessChildrenRecursive(childElement, itemController, level + 1);
                    }
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

            _contentList.Refresh();
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
                string jsonString = JsonProjectSerializer.SerializeToJson(_gameMainObject);
                File.WriteAllText(_gamePath, jsonString);
            }
        }
    }
}