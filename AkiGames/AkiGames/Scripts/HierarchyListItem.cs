using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using AkiGames.UI;
using AkiGames.UI.ScrollableList;
using System.Linq;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.Core;

namespace AkiGames.Scripts
{
    public class HierarchyListItem() : ContentItemController
    {
        private GameObject transporter = null;
        private bool _isDragging = false;
        private int _lastInsertPosition;

        private GameObject _hierarchyList;
        internal ScrollableListController scrollableList;

        public GameObject RepresentedObject;
        public int Level;
        internal List<HierarchyListItem> childItems = [];
        private HierarchyExpander _opener = null;
        internal bool IsOpened => _opener?.isOpened ?? false;

        private static bool _isAnyDragging = false; // идёт ли сейчас перетаскивание какого-либо элемента
        private double _hoverStartTime = -1; // время начала наведения курсора (мс)
        private bool _pendingOpen = false; // ожидание открытия
        private const double HoverDelayMs = 600; // задержка в мс

        public static bool IsAnyDragging => _isAnyDragging;

        private void SetIsDragging(bool value)
        {
            _isDragging = value;
            _isAnyDragging = value;
        }

        public override void Start()
        {
            _hierarchyList = gameObject.Parent;

            _opener = gameObject.Children[0].GetComponent<HierarchyExpander>();
            _opener.gameObject.IsActive = childItems.Count > 0;
        }

        public override void OnRMBUp()
        {
            //TODO: menu (rename, delete)
        }

        public void StartDrag()
        {
            SetIsDragging(true);
            
            if (transporter is null)
            {
                transporter = new("Transporter");
                transporter.uiTransform.LocalBounds = uiTransform.LocalBounds;
                transporter.uiTransform.Height = 2;
                transporter.uiTransform.VerticalAlignment = UITransform.AlignmentV.Top;
                transporter.AddComponent(new Image{fillColor = Color.RoyalBlue});
            }
            _hierarchyList.AddChild(transporter);
        }

        public override void OnMouseUpOutside() => OnMouseUp();

        public override void OnMouseUp()
        {
            if (_isDragging)
            {
                SetIsDragging(false);
                _hierarchyList.RemoveChild(transporter);
        
                if (RepresentedObject != null)
                {
                    int spacing = scrollableList.Spacing;
                    int itemHeightWithSpacing = uiTransform.Bounds.Height + spacing;
                    int insertIndex = (_lastInsertPosition + spacing) / itemHeightWithSpacing;
                    insertIndex = Math.Clamp(insertIndex, 0, scrollableList.ActiveMembers);
        
                    (GameObject newRepresentationParent, int siblingIndex) = GetTargetParentAndIndex(insertIndex);
                    if (newRepresentationParent != null)
                    {
                        // ЗАЩИТА: нельзя переместить объект внутрь себя или своего потомка
                        if (newRepresentationParent == RepresentedObject ||
                            RepresentedObject.IsParentFor(newRepresentationParent))
                        {
                            return;
                        }

                        // незачем перемещать объект, если он уже в нужном месте
                        if (RepresentedObject.Parent == newRepresentationParent &&
                            siblingIndex == RepresentedObject.Parent.Children.IndexOf(RepresentedObject))
                        {
                            return;
                        }

                        // если объект остается у того же родителя, мы все равно удаляем с прошлой позиции, сдвигая нумерацию
                        if (RepresentedObject.Parent == newRepresentationParent &&
                            siblingIndex > RepresentedObject.Parent.Children.IndexOf(RepresentedObject))
                        {
                            siblingIndex--;
                        }
                        // Удаляем из текущего родителя
                        RepresentedObject.Parent?.RemoveChild(RepresentedObject);
        
                        // Корректируем позицию среди детей нового родителя
                        List<GameObject> childrenList = newRepresentationParent.Children;
                        childrenList.Insert(siblingIndex, RepresentedObject);
                        RepresentedObject.Parent = newRepresentationParent;
                        RepresentedObject.Parent.Children = childrenList;
                    }

                    gameObject.GetAncestry()[2].GetComponent<HierarchyWindowController>().UpdateScene();
                }
            }
        }

        public override void Drag(Vector2 cursorPosOnObj)
        {
            if (!_isDragging) StartDrag();

            UpdateTransporterPosition();

            // Автоскролл
            Rectangle visibleContentRect = _hierarchyList.Parent.uiTransform.Bounds;
            float relativeY = (Events.Input.mousePosition.Y - visibleContentRect.Y) / (float)visibleContentRect.Height;
            if (relativeY < 0.2)
                scrollableList.Scroll((int)(-3 * Math.Pow((0.2 - relativeY)*5,3)));
            if (relativeY > 0.8)
                scrollableList.Scroll((int)(3 * Math.Pow((relativeY - 0.8)*5,3)));
        }

        private void UpdateTransporterPosition()
        {
            int spacing = scrollableList.Spacing;
            int itemHeightWithSpacing = uiTransform.Bounds.Height+spacing;

            Rectangle hierarchyListRect = _hierarchyList.uiTransform.Bounds;
            int parentOffsetY = hierarchyListRect.Y;

            // расчет координат полоски для вставки (располагается между элементами списка)
            int positionY = Math.Clamp(
                Events.Input.mousePosition.Y - parentOffsetY,
                0, (scrollableList.ActiveMembers-1) * itemHeightWithSpacing);
            int roundedCoordinate = (int) Math.Round((double)positionY / itemHeightWithSpacing) * itemHeightWithSpacing - spacing;
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

            if (leftItem == this) // если объект пытается осуществить вставку после самого себя
                return (RepresentedObject.Parent, RepresentedObject.Parent.Children.IndexOf(RepresentedObject));

            if (leftItem == null && rightItem != null) // если перемещение в самый верх списка
                return (rightItem.RepresentedObject.Parent, 0); // в корневом объекте

            if (rightItem == null && leftItem != null) // если перемещение в самый низ списка
            {
                GameObject parent = leftItem.RepresentedObject.Parent;
                int index = parent.Children.IndexOf(leftItem.RepresentedObject) + 1;
                return (parent, index); // внутрь родителя последнего объекта
            }

            if (leftItem != null && rightItem != null) // самый распространенный случай
            {
                if (rightItem.Level > leftItem.Level) // если следующий объект вложен в предыдущий
                    return (leftItem.RepresentedObject, 0); // вложить первым в левый
                else
                {
                    GameObject parent = leftItem.RepresentedObject.Parent;
                    int index = parent.Children.IndexOf(leftItem.RepresentedObject) + 1;
                    return (parent, index);
                }
            }

            return (RepresentedObject.Parent, 0); // необработанные случаи
        }

        public override void OnMouseEnter()
        {
            base.OnMouseEnter();

            // Автораскрытие только:
            // - если идёт перетаскивание любого элемента
            // - этот элемент НЕ является перетаскиваемым
            // - у него есть дочерние элементы
            // - он ещё не раскрыт

            // Здесь засекаем время удержания курсора над элементом списка,
            // чтобы автоматически открыть список дочерних элементов
            if (_isAnyDragging && !_isDragging && childItems.Count > 0 && !IsOpened)
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
                    if (_opener != null && !IsOpened && childItems.Count > 0)
                        _opener.ShowOrHideChildren();
                }
            }
        }

        private void StopHovering()
        {
            _hoverStartTime = -1;
            _pendingOpen = false;
        }
    }
}