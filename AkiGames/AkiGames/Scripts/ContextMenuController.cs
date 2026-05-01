using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AkiGames.Core;
using AkiGames.Events;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.Scripts.Hierarchy;
using AkiGames.UI;

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
            uiTransform.Height = 0;
            uiTransform.RefreshBounds();

            Column column = gameObject.GetComponent<Column>();
            if (column == null)
            {
                column = new Column();
                gameObject.AddComponent(column);
            }

            AddMenuItem("Rename", RenameObject);
            AddMenuItem("Create Child", CreateChild);
            AddMenuItem("Move Up", MoveUp);
            AddMenuItem("Move Down", MoveDown);
            AddMenuItem("Delete", DeleteObject);

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
            _targetItem.StartRenaming();
        }

        private void CreateChild()
        {
            if (_targetItem?.RepresentedObject == null) return;
            GameObject newChild = new("New Object");
            _targetItem.RepresentedObject.AddChild(newChild);
            OpenObjectIfClosed(_targetItem);
            UpdateHierarchyAndScene();
        }

        private void MoveUp()
        {
            GameObject obj = _targetItem?.RepresentedObject;
            GameObject oldParent = obj?.Parent;
            if (oldParent == null) return;

            GameObject rootSceneObject = Game1.editableGameMainObject;
            if (oldParent == rootSceneObject) return;

            GameObject newParent = oldParent.Parent;
            if (newParent == null) return;

            int index = newParent.Children.IndexOf(oldParent);
            oldParent.RemoveChild(obj);

            List<GameObject> children = newParent.Children;
            children.Insert(index + 1, obj);
            newParent.Children = children;
            obj.Parent = newParent;

            UpdateHierarchyAndScene();
        }

        private void MoveDown()
        {
            GameObject obj = _targetItem?.RepresentedObject;
            GameObject parent = obj?.Parent;
            if (parent == null) return;

            List<GameObject> siblings = parent.Children;
            int index = siblings.IndexOf(obj);
            if (index <= 0) return;

            GameObject targetParent = siblings[index - 1];
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
            if (item != null && !item.IsOpened && item.Opener != null)
            {
                item.Opener.ShowOrHideChildren();
            }
        }

        private HierarchyListItem FindListItemByGameObject(GameObject gameObjectToFind)
        {
            foreach (GameObject child in _targetItem.gameObject.Parent.Children)
            {
                HierarchyListItem item = child.GetComponent<HierarchyListItem>();
                if (item != null && item.RepresentedObject == gameObjectToFind)
                {
                    return item;
                }
            }

            return null;
        }

        private void UpdateHierarchyAndScene()
        {
            _targetItem.gameObject.GetAncestry()[2]
                .GetComponent<HierarchyWindowController>()
                ?.UpdateScene();
        }

        private void CloseMenu()
        {
            GameObject menuObject = gameObject;
            if (menuObject == null) return;

            menuObject.Dispose();
        }

        public override void Update()
        {
            if (gameObject == null) return;

            bool clickedOutsideMenu =
                (Input.RMB.IsDown || Input.LMB.IsDown) &&
                (Input.MouseHoverTarget == null || !gameObject.IsParentFor(Input.MouseHoverTarget));

            if (clickedOutsideMenu)
            {
                CloseMenu();
                return;
            }

            if (Input.LMB.IsUp) CloseMenu();
        }
    }
}
