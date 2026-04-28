using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using AkiGames.Core;
using AkiGames.UI;
using AkiGames.Scripts.WindowContentTypes;

namespace AkiGames.Scripts
{
    public class HierarchyListItem() : ContentItemController
    {
        private GameObject transporter = null;
        private bool _isDragging = false;
        private int _lastInsertPosition;

        private GameObject _hierarchyList;
        internal UI.ScrollableList.ScrollableListController scrollableList;

        public GameObject RepresentedObject;
        public int Level;
        internal List<HierarchyListItem> childItems = [];
        public HierarchyExpander Opener {get; private set;} = null;
        internal bool IsOpened => Opener?.isOpened ?? false;

        private static GameObject _selectedObject = null;
        private bool _renameOnCurrentDoubleClick = false;
        private long _lastMouseDownMs = -RenameDoubleClickThresholdMs;
        private bool _isRenaming = false;
        private string _renameValue = "";
        private KeyboardState _currentKeyboardState;
        private KeyboardState _previousKeyboardState;
        private const long RenameDoubleClickThresholdMs = 500;
        private const double RenameCursorBlinkMs = 500;

        private static readonly Dictionary<Keys, (string normal, string shifted)> _symbolKeys = new()
        {
            { Keys.D0, ("0", ")") },
            { Keys.D1, ("1", "!") },
            { Keys.D2, ("2", "@") },
            { Keys.D3, ("3", "#") },
            { Keys.D4, ("4", "$") },
            { Keys.D5, ("5", "%") },
            { Keys.D6, ("6", "^") },
            { Keys.D7, ("7", "&") },
            { Keys.D8, ("8", "*") },
            { Keys.D9, ("9", "(") },
            { Keys.NumPad0, ("0", "0") },
            { Keys.NumPad1, ("1", "1") },
            { Keys.NumPad2, ("2", "2") },
            { Keys.NumPad3, ("3", "3") },
            { Keys.NumPad4, ("4", "4") },
            { Keys.NumPad5, ("5", "5") },
            { Keys.NumPad6, ("6", "6") },
            { Keys.NumPad7, ("7", "7") },
            { Keys.NumPad8, ("8", "8") },
            { Keys.NumPad9, ("9", "9") },
            { Keys.Space, (" ", " ") },
            { Keys.OemComma, (",", "<") },
            { Keys.OemPeriod, (".", ">") },
            { Keys.OemMinus, ("-", "_") },
            { Keys.OemPlus, ("=", "+") },
            { Keys.OemQuestion, ("/", "?") },
            { Keys.OemSemicolon, (";", ":") },
            { Keys.OemQuotes, ("'", "\"") },
            { Keys.OemOpenBrackets, ("[", "{") },
            { Keys.OemCloseBrackets, ("]", "}") },
            { Keys.OemPipe, ("\\", "|") },
            { Keys.OemTilde, ("`", "~") },
            { Keys.Decimal, (".", ".") },
            { Keys.Add, ("+", "+") },
            { Keys.Subtract, ("-", "-") },
            { Keys.Multiply, ("*", "*") },
            { Keys.Divide, ("/", "/") }
        };

        private static bool _isAnyDragging = false; // идёт ли сейчас перетаскивание какого-либо элемента
        private double _hoverStartTime = -1; // время начала наведения курсора (мс)
        private bool _pendingOpen = false; // ожидание открытия
        private const double HoverDelayMs = 600; // задержка в мс

        private void SetIsDragging(bool value)
        {
            _isDragging = value;
            _isAnyDragging = value;
        }

        public override void Start()
        {
            _hierarchyList = gameObject.Parent;

            Opener = gameObject.Children[0].GetComponent<HierarchyExpander>();
            Opener.gameObject.IsActive = childItems.Count > 0;
        }

        public override void OnMouseDown()
        {
            long now = Environment.TickCount64;
            bool clickedSelectedObject = _selectedObject == RepresentedObject;

            if (clickedSelectedObject && !_isRenaming)
            {
                InspectorWindowController.LoadFor(RepresentedObject);
            }

            if (now - _lastMouseDownMs > RenameDoubleClickThresholdMs)
            {
                _renameOnCurrentDoubleClick = clickedSelectedObject;
            }

            _lastMouseDownMs = now;
            _selectedObject = RepresentedObject;
            base.OnMouseDown();
        }

        public override void OnDoubleClick()
        {
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
            if (_isRenaming) UpdateRenaming();

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

        public void StartRenaming()
        {
            if (RepresentedObject == null) return;

            _selectedObject = RepresentedObject;
            _isRenaming = true;
            _renameValue = RepresentedObject.ObjectName;
            _currentKeyboardState = Keyboard.GetState();
            _previousKeyboardState = _currentKeyboardState;
            UpdateDisplayedName();
        }

        private void UpdateRenaming()
        {
            _previousKeyboardState = _currentKeyboardState;
            _currentKeyboardState = Keyboard.GetState();

            if (
                Events.Input.LMB.IsDown &&
                Events.Input.MouseHoverTarget != gameObject &&
                !gameObject.IsParentFor(Events.Input.MouseHoverTarget)
            )
            {
                CommitRenaming();
                return;
            }

            if (IsKeyPressed(Keys.Escape))
            {
                CancelRenaming();
                return;
            }

            if (IsKeyPressed(Keys.Enter))
            {
                CommitRenaming();
                return;
            }

            ProcessRenameKeyboardInput();
            UpdateDisplayedName();
        }

        private void ProcessRenameKeyboardInput()
        {
            bool shiftPressed =
                _currentKeyboardState.IsKeyDown(Keys.LeftShift) ||
                _currentKeyboardState.IsKeyDown(Keys.RightShift);
            bool capsLock = _currentKeyboardState.CapsLock;

            for (Keys key = Keys.A; key <= Keys.Z; key++)
            {
                if (!IsKeyPressed(key)) continue;

                char character = (char)('a' + (key - Keys.A));
                bool upper = shiftPressed ^ capsLock;
                _renameValue += upper ? char.ToUpper(character) : character;
            }

            foreach (var keyPair in _symbolKeys)
            {
                if (IsKeyPressed(keyPair.Key))
                {
                    _renameValue += shiftPressed ? keyPair.Value.shifted : keyPair.Value.normal;
                }
            }

            if (IsKeyPressed(Keys.Back) && _renameValue.Length > 0)
            {
                _renameValue = _renameValue[..^1];
            }

            if (IsKeyPressed(Keys.Delete))
            {
                _renameValue = "";
            }
        }

        private bool IsKeyPressed(Keys key) =>
            _currentKeyboardState.IsKeyDown(key) && _previousKeyboardState.IsKeyUp(key);

        private void CommitRenaming()
        {
            string newName = _renameValue.Trim();
            if (!string.IsNullOrWhiteSpace(newName))
            {
                RepresentedObject.ObjectName = newName;
            }

            _isRenaming = false;
            UpdateDisplayedName();
            gameObject.GetAncestry()[2].GetComponent<HierarchyWindowController>().UpdateScene();
        }

        private void CancelRenaming()
        {
            _isRenaming = false;
            UpdateDisplayedName();
        }

        public override void Deactivate()
        {
            if (_isRenaming)
            {
                CommitRenaming();
                return;
            }

            base.Deactivate();
        }

        private void UpdateDisplayedName()
        {
            Text title = gameObject.Children[1].GetComponent<Text>();
            string visibleName = _isRenaming ? _renameValue : RepresentedObject?.ObjectName ?? "";
            title.text = new string(' ', Level * 3) + visibleName + RenameCursor;
        }

        private string RenameCursor
        {
            get
            {
                if (!_isRenaming) return "";
                double timeMs = gameTime?.TotalGameTime.TotalMilliseconds ?? 0;
                return (int)(timeMs / RenameCursorBlinkMs) % 2 == 0 ? "_" : "";
            }
        }
    }
}
