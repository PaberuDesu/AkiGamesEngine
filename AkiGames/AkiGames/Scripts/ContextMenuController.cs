using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AkiGames.Core;
using AkiGames.UI;
using AkiGames.Events;
using AkiGames.Scripts.WindowContentTypes;

namespace AkiGames.Scripts
{
    public class ContextMenuController : GameComponent
    {
        private static GameObject _contextMenuItemPrefab;
        private HierarchyListItem _targetItem;

        public void Show(Vector2 screenPosition, HierarchyListItem targetItem)
        {
            _contextMenuItemPrefab ??= Game1.Prefabs["ContextMenuItem"];

            _targetItem = targetItem;

            Game1.MainObject.AddChild(gameObject);

            uiTransform.OffsetMin = screenPosition;
            uiTransform.Width = 180;
            uiTransform.Height = 0; // авто-высота от Column
            uiTransform.RefreshBounds();

            Column column = gameObject.GetComponent<Column>();
            if (column == null)
            {
                column = new Column();
                gameObject.AddComponent(column);
            }

            AddMenuItem("Переименовать", RenameObject);
            AddMenuItem("Создать дочерний", CreateChild);
            AddMenuItem("Переместить вверх", MoveUp);
            AddMenuItem("Переместить вниз", MoveDown);
            AddMenuItem("Удалить", DeleteObject);

            column.Refresh();
            gameObject.RefreshBounds();
        }

        private void AddMenuItem(string text, Action action)
        {
            GameObject item = _contextMenuItemPrefab.Copy();

            Text textComp = item.GetComponent<Text>();
            textComp?.text = text;

            Events.EventHandler eventHandler = item.GetComponent<Events.EventHandler>();
            eventHandler?.OnMouseUpEvent += () =>
            {
                action?.Invoke();
                CloseMenu();
            };

            gameObject.AddChild(item);
        }

        private void RenameObject()
        {
            if (_targetItem?.RepresentedObject == null) return;
            string newName = Microsoft.VisualBasic.Interaction.InputBox(
                "Введите новое имя", "Переименование", _targetItem.RepresentedObject.ObjectName);
            if (!string.IsNullOrWhiteSpace(newName))
            {
                _targetItem.RepresentedObject.ObjectName = newName;
                UpdateHierarchyAndScene();
            }
        }

        private void CreateChild()
        {
            if (_targetItem?.RepresentedObject == null) return;
            GameObject newChild = new("New Object");
            _targetItem.RepresentedObject.AddChild(newChild);
            OpenObjectIfClosed(_targetItem);
            UpdateHierarchyAndScene();
        }

        private void MoveUp() // Повысить уровень (сделать sibling'ом своего родителя)
        {
            GameObject obj = _targetItem?.RepresentedObject;
            GameObject oldParent = obj?.Parent;
            if (oldParent == null) return;

            // Корневой объект сцены
            GameObject rootSceneObject = Game1.editableGameMainObject;
            if (oldParent == rootSceneObject) return; // Нельзя поднять объект, находящийся прямо в корне

            GameObject newParent = oldParent?.Parent;
            if (newParent == null) return;

            // Запоминаем позицию старого родителя в новом родителе
            int index = newParent.Children.IndexOf(oldParent);

            // Открепляем объект от старого родителя
            oldParent.RemoveChild(obj);

            // Вставляем объект после старого родителя
            List<GameObject> children = newParent.Children;
            children.Insert(index+1, obj);
            newParent.Children = children;
            obj.Parent = newParent;

            UpdateHierarchyAndScene();
        }

        private void MoveDown() // Понизить уровень (вложить в предыдущий sibling)
        {
            GameObject obj = _targetItem?.RepresentedObject;
            GameObject parent = obj?.Parent;
            if (parent == null) return;

            List<GameObject> siblings = parent.Children;
            int idx = siblings.IndexOf(obj);
            if (idx <= 0) return; // нет предыдущего sibling

            GameObject targetParent = siblings[idx - 1]; // Старший брат, который окажется родителем

            parent.RemoveChild(obj);
            targetParent.AddChild(obj);

            HierarchyListItem targetParentInHierarchy = FindListItemByGameObject(targetParent);
            OpenObjectIfClosed(targetParentInHierarchy);
            UpdateHierarchyAndScene();
        }

        private void DeleteObject()
        {
            if (_targetItem?.RepresentedObject == null) return;
            _targetItem.RepresentedObject.Dispose();
            UpdateHierarchyAndScene();
        }

        private static void OpenObjectIfClosed(HierarchyListItem item)
        {
            // Раскрываем родителя, если он свёрнут
            if (item != null && !item.IsOpened && item.Opener != null)
                item.Opener.ShowOrHideChildren();
        }

        private HierarchyListItem FindListItemByGameObject(GameObject go)
        {
            foreach (GameObject child in _targetItem.gameObject.Parent.Children)
            {
                HierarchyListItem item = child.GetComponent<HierarchyListItem>();
                if (item != null && item.RepresentedObject == go)
                    return item;
            }
            return null;
        }

        private void UpdateHierarchyAndScene()
        {
            _targetItem.gameObject.GetAncestry()[2]
                .GetComponent<WindowContentTypes.HierarchyWindowController>()
                ?.UpdateScene();
        }

        private void CloseMenu() => gameObject?.Dispose();

        public override void Update()
        {
            if ((Input.RMB.IsDown || Input.LMB.IsDown)
                && !gameObject.IsParentFor(Input.MouseHoverTarget))
            {
                CloseMenu();
            }
            if (Input.LMB.IsUp) CloseMenu();
        }
    }
}
