using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;
using AkiGames.UI;

namespace AkiGames.Events
{
    public class EventSystem
    {
        private const float _moveThreshold = 0;
        private static Point? _previousPosition;
        private static GameObject _previousTarget = null;
        public static GameObject MainObject { private get; set; } = null;
        private static GameObject _gameInputRoot = null;
        private static UITransform _gameInputViewTransform = null;
        private static GameObject _editorInputOverrideRoot = null;
        public static bool IsGameInputMode => _gameInputRoot != null;

        public static void StartGameInput(
            GameObject gameInputRoot,
            UITransform gameInputViewTransform,
            GameObject editorInputOverrideRoot
        )
        {
            _gameInputRoot = gameInputRoot;
            _gameInputViewTransform = gameInputViewTransform;
            _editorInputOverrideRoot = editorInputOverrideRoot;
            ResetPointerState();
        }

        public static void StopGameInput()
        {
            _gameInputRoot = null;
            _gameInputViewTransform = null;
            _editorInputOverrideRoot = null;
            ResetPointerState();
        }

        private static void ResetPointerState()
        {
            Input.MouseHoverTarget?.OnMouseExit();
            Input.MouseHoverTarget = null;
            Input.EndPressing();
            _previousTarget = null;
            _previousPosition = null;
        }

        public static void Update()
        {
            // Получаем ввод
            KeyboardState keyboardState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            // Ctrl, Z, S keys pressing
            Input.Ctrl.IsPressed = keyboardState.IsKeyDown(Keys.LeftControl);
            Input.Z.IsPressed = keyboardState.IsKeyDown(Keys.Z);
            Input.S.IsPressed = keyboardState.IsKeyDown(Keys.S);

            //Hotkeys
            if (Input.Hotkey != null && !IsGameInputMode)
            {
                _previousTarget?.ProcessHotkey((Input.HotKey)Input.Hotkey);
            }

            if (IsCursorInWindow()) // mouse activity
            {
                Input.LMB.IsPressed = mouseState.LeftButton == ButtonState.Pressed;
                Input.RMB.IsPressed = mouseState.RightButton == ButtonState.Pressed;
                Input.Scroll = mouseState.ScrollWheelValue;
                Input.ScreenMousePosition = mouseState.Position;

                GameObject inputRoot = ResolveInputRoot(mouseState.Position);
                if (inputRoot == null)
                {
                    ResetPointerState();
                    return;
                }

                GameObject currentTarget = FindTarget(inputRoot);
                if (Input.MouseHoverTarget != currentTarget)
                {
                    Input.MouseHoverTarget?.OnMouseExit();
                    Input.MouseHoverTarget = currentTarget;
                    currentTarget?.OnMouseEnter();
                }

                if (Input.LMB.IsDown)//here pressing starts, also OnDown events
                {
                    if (_previousTarget is null)
                    {
                        _previousTarget = currentTarget;
                    }
                    else
                    {
                        _previousTarget?.Deactivate();
                        _previousTarget = currentTarget;
                    }
                    currentTarget?.OnMouseDown();
                    Input.StartPressing();
                }
                else if (Input.LMB.IsPressed)
                {
                    Point currentPosition = Input.mousePosition;
                    Point previousPosition = _previousPosition ?? currentPosition;
                    if (
                        Vector2.Distance(
                        previousPosition.ToVector2(),
                        currentPosition.ToVector2()
                        ) > _moveThreshold
                    )//Drag
                    {
                        Input.MousePressTarget?.Drag(Input.MousePressTargetOffset);
                    }
                }
                else if (Input.LMB.IsUp)//OnUp
                {
                    if (Input.MousePressTarget != currentTarget)
                        Input.MousePressTarget?.OnMouseUpOutside(); // if pressed on something, dragged out of it and stopped pressing
                    else currentTarget?.OnMouseUp();
                    Input.EndPressing();
                }
                else if (Input.LMB.IsReleased)
                {
                    if (Input.DeltaScroll != 0) currentTarget?.OnScroll(Input.DeltaScroll);
                }

                if (Input.RMB.IsUp) //OmRMBUp
                {
                    currentTarget?.OnRMBUp();
                }

                _previousPosition = Input.mousePosition;
            }
        }

        private static GameObject ResolveInputRoot(Point screenPosition)
        {
            if (!IsGameInputMode)
            {
                Input.mousePosition = screenPosition;
                return MainObject;
            }

            if (
                _editorInputOverrideRoot != null &&
                _editorInputOverrideRoot.uiTransform.Contains(screenPosition)
            )
            {
                Input.mousePosition = screenPosition;
                return _editorInputOverrideRoot;
            }

            if (
                _gameInputViewTransform == null ||
                !_gameInputViewTransform.Contains(screenPosition)
            )
            {
                Input.mousePosition = screenPosition;
                return null;
            }

            Input.mousePosition = ToGamePosition(screenPosition);
            return _gameInputRoot;
        }

        private static Point ToGamePosition(Point screenPosition)
        {
            if (_gameInputRoot == null || _gameInputViewTransform == null)
                return screenPosition;

            Rectangle viewBounds = _gameInputViewTransform.Bounds;
            Rectangle gameBounds = _gameInputRoot.uiTransform.Bounds;
            if (viewBounds.Width <= 0 || viewBounds.Height <= 0)
                return screenPosition;

            return new Point(
                (int)Math.Round((screenPosition.X - viewBounds.X) * gameBounds.Width / (float)viewBounds.Width),
                (int)Math.Round((screenPosition.Y - viewBounds.Y) * gameBounds.Height / (float)viewBounds.Height)
            );
        }

        private static GameObject FindTarget(GameObject root)
        {
            (GameObject obj, int zIndex) bestCandidate = (null, int.MinValue);
            FindTargetInHierarchy(root, ref bestCandidate);
            return bestCandidate.obj;
        }
        
        private static void FindTargetInHierarchy(GameObject parent, ref (GameObject obj, int zIndex) bestCandidate)
        {
            if (parent == null) return;
            if (!parent.IsActive) return;
            
            // Проверяем родителя
            if (parent.IsMouseTargetable && parent.uiTransform.Contains(Input.mousePosition))
            {
                Image image = parent.GetComponent<Image>();
                if (image != null && image.Enabled && image.zIndex >= bestCandidate.zIndex)
                {
                    bestCandidate = (parent, image.zIndex);
                }
                Text text = parent.GetComponent<Text>();
                if (text != null && text.Enabled && text.zIndex >= bestCandidate.zIndex)
                {
                    bestCandidate = (parent, text.zIndex);
                }
            }
            
            // Проверяем детей
            if (parent.Children != null)
            {
                foreach (var child in parent.Children)
                {
                    FindTargetInHierarchy(child, ref bestCandidate);
                }
            }
        }

        public static bool IsCursorInWindow()
        {
            MouseState mouseState = Mouse.GetState();
            Viewport viewport = Core.Game1.AppGraphicsDevice.Viewport;
            
            return mouseState.X >= 0 && 
                   mouseState.Y >= 0 &&
                   mouseState.X < viewport.Width && 
                   mouseState.Y < viewport.Height;
        }
    }

    public static class Input
    {
        public static Key Ctrl { get; } = new();
        public static Key Z { get; } = new();
        public static Key S { get; } = new();
        public static Key LMB { get; } = new();
        public static Key RMB { get; } = new();

        private static int _prevScroll = 0;
        private static int _scroll = 0;
        public static int Scroll
        {
            set
            {
                _prevScroll = _scroll;
                _scroll = value;
            }
        }
        public static int DeltaScroll
        {
            get => _prevScroll - _scroll;
        }

        public static Point mousePosition;
        public static Point ScreenMousePosition { get; internal set; }

        private static GameObject _mouseHoverTarget = null;
        public static GameObject MouseHoverTarget // what object is cursor on now
        {
            get => _mouseHoverTarget;
            set => _mouseHoverTarget = value;
        }
        private static GameObject _mousePressTarget = null;
        public static Vector2 MousePressTargetOffset { get; private set; } // Which place of obj is press started at
        public static GameObject MousePressTarget // what object was cursor on when started pressing
        {
            get => _mousePressTarget;
        }

        public static void StartPressing()
        {
            _mousePressTarget = _mouseHoverTarget;
            if (_mousePressTarget == null)
            {
                MousePressTargetOffset = Vector2.Zero;
                return;
            }

            MousePressTargetOffset = new Vector2(
                mousePosition.X - _mousePressTarget.uiTransform.Bounds.X,
                mousePosition.Y - _mousePressTarget.uiTransform.Bounds.Y
            );
        }

        public static void EndPressing()
        {
            _mousePressTarget = null;
            MousePressTargetOffset = Vector2.Zero;
        }

        public static HotKey? Hotkey
        {
            get
            {
                if (Ctrl.IsPressed && Z.IsDown)
                    return HotKey.CtrlZ;
                if (Ctrl.IsPressed && S.IsPressed)
                    return HotKey.CtrlS;
                return null;
            }
        }
        public enum HotKey
        {
            CtrlZ,
            CtrlS
        }
    }

    public class Key
    {
        private bool _wasPressed = false;
        private bool _isPressed = false;
        public bool IsDown => _isPressed && !_wasPressed;
        public bool IsUp => !_isPressed && _wasPressed;
        public bool IsPressed
        {
            get => _isPressed;
            set
            {
                _wasPressed = _isPressed;
                _isPressed = value;
            }
        }
        public bool IsReleased => !_isPressed;
    }
}
