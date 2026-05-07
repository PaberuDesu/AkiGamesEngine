using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AkiGames.Core;
using AkiGames.Scripts.Inspector;
using AkiGames.UI;
using AkiGames.Scripts.WindowContentTypes;

namespace AkiGames.Scripts.Hierarchy
{
    public class HierarchyListItem : EditableContentItemController
    {
        private GameObject transporter = null;
        private static GameObject draggedHierarchyObject = null;
        private static Texture2D _draggedHierarchyTexture = null;
        private int _lastInsertPosition;

        private GameObject _hierarchyList;
        internal UI.ScrollableList.ScrollableListController scrollableList;

        public GameObject RepresentedObject;
        public int Level;
        internal List<HierarchyListItem> childItems = [];
        public HierarchyExpander Opener { get; private set; } = null;
        internal bool IsOpened => Opener?.isOpened ?? false;

        private static GameObject _selectedObject = null;
        private bool _renameOnCurrentDoubleClick = false;
        private long _lastMouseDownMs = -RenameDoubleClickThresholdMs;
        private const long RenameDoubleClickThresholdMs = 500;

        private static bool _isAnyDragging = false;
        private double _hoverStartTime = -1;
        private bool _pendingOpen = false;
        private const double HoverDelayMs = 600;
        private static readonly Color PrefabLinkTextColor = new(120, 180, 255);
        private static readonly Color DefaultTextColor = Color.White;

        public override void Start()
        {
            _hierarchyList = gameObject.Parent;

            Opener = gameObject.Children[0].GetComponent<HierarchyExpander>();
            Opener.gameObject.IsActive = childItems.Count > 0;
            RefreshPrefabLinkHighlight();
        }

        public override void OnMouseDown()
        {
            long now = Environment.TickCount64;
            bool clickedSelectedObject = _selectedObject == RepresentedObject;

            if (clickedSelectedObject && !IsRenaming)
            {
                InspectorWindowController.LoadFor(RepresentedObject);
            }

            if (now - _lastMouseDownMs > RenameDoubleClickThresholdMs)
            {
                _renameOnCurrentDoubleClick = clickedSelectedObject;
            }

            _lastMouseDownMs = now;
            _selectedObject = RepresentedObject;
            InspectorWindowController.Select(RepresentedObject);
            base.OnMouseDown();
        }

        public override void OnDoubleClick()
        {
            if (IsRenaming) return;

            if (_renameOnCurrentDoubleClick)
            {
                StartRenaming();
                return;
            }

            base.OnDoubleClick();
            _selectedObject = RepresentedObject;
        }

        public override void OnRMBUp()
        {
            GameObject contextMenu = Game1.Prefabs["ContextMenu"].Copy();
            Game1.MainObject.AddChild(contextMenu);
            contextMenu.GetComponent<ContextMenuController>().Show(Events.Input.mousePosition.ToVector2(), this);
        }

        protected override bool CanStartDragging() => RepresentedObject != null;

        protected override bool IsCursorInsideLocalDragArea() =>
            IsCursorInsideHierarchyWindow();

        protected override void OnDragStarted()
        {
            _isAnyDragging = true;
            EnsureDraggedHierarchyObject();

            if (transporter is null)
            {
                transporter = new("Transporter");
                transporter.uiTransform.LocalBounds = uiTransform.LocalBounds;
                transporter.uiTransform.Height = 2;
                transporter.uiTransform.VerticalAlignment = UITransform.AlignmentV.Top;
                transporter.AddComponent(new Image { fillColor = Color.RoyalBlue });
            }
        }

        protected override void UpdateLocalDrag(Vector2 cursorPosOnObj)
        {
            HideDraggedHierarchyObject();
            ShowTransporter();
            UpdateTransporterPosition();
            AutoScrollList();
        }

        protected override void UpdateOuterDrag(Vector2 cursorPosOnObj)
        {
            HideTransporter();
            UpdateDraggedHierarchyObjectPosition();
        }

        protected override void CompleteLocalDrag()
        {
            HideDraggedHierarchyObject();
            HideTransporter();
            MoveRepresentedObjectToInsertPosition();
        }

        protected override void CompleteOuterDrag()
        {
            HideTransporter();
            HideDraggedHierarchyObject();
            TryApplyGameObjectDrop();
        }

        protected override void OnDragEnded()
        {
            _isAnyDragging = false;
            HideTransporter();
            HideDraggedHierarchyObject();
        }

        private void MoveRepresentedObjectToInsertPosition()
        {
            if (RepresentedObject == null) return;

            int spacing = scrollableList.Spacing;
            int itemHeightWithSpacing = uiTransform.Bounds.Height + spacing;
            int insertIndex = (_lastInsertPosition + spacing) / itemHeightWithSpacing;
            insertIndex = Math.Clamp(insertIndex, 0, scrollableList.ActiveMembers);

            (GameObject newRepresentationParent, int siblingIndex) = GetTargetParentAndIndex(insertIndex);
            if (newRepresentationParent == null) return;

            if (newRepresentationParent == RepresentedObject ||
                RepresentedObject.IsParentFor(newRepresentationParent))
            {
                return;
            }

            if (RepresentedObject.Parent == newRepresentationParent &&
                siblingIndex == RepresentedObject.Parent.Children.IndexOf(RepresentedObject))
            {
                return;
            }

            if (RepresentedObject.Parent == newRepresentationParent &&
                siblingIndex > RepresentedObject.Parent.Children.IndexOf(RepresentedObject))
            {
                siblingIndex--;
            }

            RepresentedObject.Parent?.RemoveChild(RepresentedObject);

            List<GameObject> childrenList = newRepresentationParent.Children;
            childrenList.Insert(siblingIndex, RepresentedObject);
            RepresentedObject.Parent = newRepresentationParent;
            RepresentedObject.Parent.Children = childrenList;

            gameObject.GetAncestry()[2].GetComponent<HierarchyWindowController>().UpdateScene();
        }

        private void AutoScrollList()
        {
            Rectangle visibleContentRect = _hierarchyList.Parent.uiTransform.Bounds;
            float relativeY = (Events.Input.mousePosition.Y - visibleContentRect.Y) / (float)visibleContentRect.Height;
            if (relativeY < 0.2)
                scrollableList.Scroll((int)(-3 * Math.Pow((0.2 - relativeY) * 5, 3)));
            if (relativeY > 0.8)
                scrollableList.Scroll((int)(3 * Math.Pow((relativeY - 0.8) * 5, 3)));
        }

        private void ShowTransporter()
        {
            if (transporter != null && transporter.Parent == null)
                _hierarchyList.AddChild(transporter);
        }

        private void HideTransporter()
        {
            if (transporter?.Parent != null)
                transporter.Parent.RemoveChild(transporter);
        }

        private static void EnsureDraggedHierarchyObject()
        {
            if (draggedHierarchyObject != null) return;
            if (Game1.MainObject == null || Game1.MainObject.Children.Count <= 2) return;

            draggedHierarchyObject = Game1.MainObject.Children[2];
            draggedHierarchyObject.RefreshBounds();
        }

        private static void UpdateDraggedHierarchyObjectPosition()
        {
            EnsureDraggedHierarchyObject();
            if (draggedHierarchyObject == null) return;

            draggedHierarchyObject.IsActive = true;
            Image draggedImage = draggedHierarchyObject.GetComponent<Image>();
            if (draggedImage != null)
            {
                draggedImage.fillColor = Color.White;
                _draggedHierarchyTexture ??= Game1.LoadGameTexture("Content/InspectorIcons/GameObjectDescription_Component");
                draggedImage.texture = _draggedHierarchyTexture;
            }

            draggedHierarchyObject.uiTransform.OffsetMin = Events.Input.mousePosition.ToVector2();
            draggedHierarchyObject.RefreshBounds();
        }

        private static void HideDraggedHierarchyObject() =>
            draggedHierarchyObject?.IsActive = false;

        private bool IsCursorInsideHierarchyWindow()
        {
            List<GameObject> ancestry = gameObject.GetAncestry();
            if (ancestry.Count > 2)
                return ancestry[2].uiTransform.Contains(Events.Input.mousePosition);

            return _hierarchyList?.Parent?.uiTransform.Contains(Events.Input.mousePosition) ?? false;
        }

        private void TryApplyGameObjectDrop()
        {
            InspectorGameComponentDropField gameComponentDropField =
                InspectorDropFieldFinder.FindInAncestry<InspectorGameComponentDropField>(
                    Events.Input.MouseHoverTarget
                );
            if (gameComponentDropField?.TryApplyGameObject(RepresentedObject) == true)
                return;

            InspectorGameObjectDropField gameObjectDropField =
                InspectorDropFieldFinder.FindInAncestry<InspectorGameObjectDropField>(
                    Events.Input.MouseHoverTarget
                );

            gameObjectDropField?.TryApplyGameObject(RepresentedObject);
        }

        private void UpdateTransporterPosition()
        {
            int spacing = scrollableList.Spacing;
            int itemHeightWithSpacing = uiTransform.Bounds.Height + spacing;

            Rectangle hierarchyListRect = _hierarchyList.uiTransform.Bounds;
            int parentOffsetY = hierarchyListRect.Y;

            int positionY = Math.Clamp(
                Events.Input.mousePosition.Y - parentOffsetY,
                0,
                (scrollableList.ActiveMembers - 1) * itemHeightWithSpacing
            );
            int roundedCoordinate = (int)Math.Round((double)positionY / itemHeightWithSpacing) *
                itemHeightWithSpacing -
                spacing;
            if (roundedCoordinate < 0) roundedCoordinate = 0;

            _lastInsertPosition = roundedCoordinate;

            transporter.uiTransform.OffsetMin = new(0, roundedCoordinate);
            transporter.RefreshBounds();
        }

        private (GameObject parent, int siblingIndex) GetTargetParentAndIndex(int insertIndex)
        {
            List<HierarchyListItem> visibleItems = [.. _hierarchyList.Children
                .Where(child => child.IsActive && child.GetComponent<HierarchyListItem>() != null)
                .Select(child => child.GetComponent<HierarchyListItem>())];

            if (visibleItems.Count == 0) return (RepresentedObject.Parent, 0);

            HierarchyListItem leftItem = insertIndex > 0 ? visibleItems[insertIndex - 1] : null;
            HierarchyListItem rightItem = insertIndex < visibleItems.Count ? visibleItems[insertIndex] : null;

            if (leftItem == this)
                return (RepresentedObject.Parent, RepresentedObject.Parent.Children.IndexOf(RepresentedObject));

            if (leftItem == null && rightItem != null)
                return (rightItem.RepresentedObject.Parent, 0);

            if (rightItem == null && leftItem != null)
            {
                GameObject parent = leftItem.RepresentedObject.Parent;
                int index = parent.Children.IndexOf(leftItem.RepresentedObject) + 1;
                return (parent, index);
            }

            if (leftItem != null && rightItem != null)
            {
                if (rightItem.Level > leftItem.Level)
                    return (leftItem.RepresentedObject, 0);

                GameObject parent = leftItem.RepresentedObject.Parent;
                int index = parent.Children.IndexOf(leftItem.RepresentedObject) + 1;
                return (parent, index);
            }

            return (RepresentedObject.Parent, 0);
        }

        public override void OnMouseEnter()
        {
            base.OnMouseEnter();

            if (_isAnyDragging && !IsDragging && childItems.Count > 0 && !IsOpened)
            {
                _hoverStartTime = gameTime.TotalGameTime.TotalMilliseconds;
                _pendingOpen = true;
            }
        }

        public override void OnMouseExit()
        {
            base.OnMouseExit();
            StopHovering();
        }

        public override void Update()
        {
            base.Update();

            if (_pendingOpen && _hoverStartTime > 0 && gameTime != null)
            {
                double now = gameTime.TotalGameTime.TotalMilliseconds;
                if (now - _hoverStartTime >= HoverDelayMs)
                {
                    StopHovering();
                    if (Opener != null && !IsOpened && childItems.Count > 0)
                        Opener.ShowOrHideChildren();
                }
            }
        }

        private void StopHovering()
        {
            _hoverStartTime = -1;
            _pendingOpen = false;
        }

        private void RefreshPrefabLinkHighlight()
        {
            if (Title == null || RepresentedObject == null) return;

            Title.TextColor = string.IsNullOrWhiteSpace(RepresentedObject.SourcePrefabLink) ?
                DefaultTextColor :
                PrefabLinkTextColor;
        }

        protected override bool CanStartRenaming() => RepresentedObject != null;

        protected override void BeforeStartRenaming()
        {
            _selectedObject = RepresentedObject;
        }

        protected override string GetRenameInitialValue() =>
            RepresentedObject?.ObjectName ?? "";

        protected override string GetDisplayName() =>
            RepresentedObject?.ObjectName ?? "";

        protected override string FormatDisplayedName(string visibleName) =>
            new string(' ', Level * 3) + visibleName;

        protected override bool OnRenameCommitted(string newName)
        {
            if (!string.IsNullOrWhiteSpace(newName))
            {
                RepresentedObject.ObjectName = newName;
            }

            gameObject.GetAncestry()[2].GetComponent<HierarchyWindowController>().UpdateScene();
            return true;
        }
    }
}
