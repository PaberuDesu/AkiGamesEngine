using AkiGames.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AkiGames.Events
{
    public class EventSystem
    {
        private const float _moveThreshold = 0;
        private static Point? _previousPosition;
        private static GameObject _previousTarget = null;
        public static GameObject MainObject { private get; set; } = null;

        public static void Update()
        {
            // Получаем ввод
            KeyboardState keyboardState = Keyboard.GetState();
            MouseState mouseState = Mouse.GetState();

            // Ctrl, Z
            Input.Ctrl.IsPressed = keyboardState.IsKeyDown(Keys.LeftControl);
            Input.Z.IsPressed = keyboardState.IsKeyDown(Keys.Z);

            //Hotkeys
            if (Input.Hotkey != null)
            {
                _previousTarget?.ProcessHotkey((Input.HotKey)Input.Hotkey);
            }

            if (IsCursorInWindow()) // mouse activity
            {
                Input.LMB.IsPressed = mouseState.LeftButton == ButtonState.Pressed;
                Input.Scroll = mouseState.ScrollWheelValue;
                Input.mousePosition = mouseState.Position;

                GameObject currentTarget = FindTarget();
                if (Input.MouseHoverTarget != currentTarget)
                {
                    Input.MouseHoverTarget?.OnMouseExit();
                    Input.MouseHoverTarget = currentTarget;
                    currentTarget.OnMouseEnter();
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
                        Input.MousePressTarget.Drag(Input.MousePressTargetOffset);
                    }
                }
                else if (Input.LMB.IsUp)//OnUp
                {
                    if (Input.MousePressTarget != currentTarget)
                    {
                        Input.MousePressTarget?.OnMouseUpOutside(); // if pressed on something, dragged out of it and stopped pressing
                    }
                    ;
                    Input.EndPressing();
                    currentTarget.OnMouseUp();
                }
                else if (Input.LMB.IsReleased)
                {
                    if (Input.DeltaScroll != 0) currentTarget.OnScroll(Input.DeltaScroll);
                }
                _previousPosition = Input.mousePosition;
            }
        }

        private static GameObject FindTarget()
        {
            // Проверяем объекты в обратном порядке (от верхних к нижним по z-index)
            for (int i = MainObject.Children.Count - 1; i >= 0; i--)
            {
                GameObject obj = MainObject.Children[i];
                GameObject target = FindTargetIn(obj);
                if (target != null)
                    return target;
            }
            return MainObject;
        }

        private static GameObject FindTargetIn(GameObject parent)
        {
            if (parent.IsActive)
            {
                // Сначала проверяем дочерние объекты (в обратном порядке)
                if (parent.Children != null && parent.Children.Count > 0)
                {
                    for (int i = parent.Children.Count - 1; i >= 0; i--)
                    {
                        GameObject child = parent.Children[i];
                        GameObject target = FindTargetIn(child);
                        if (target != null)
                            return target;
                    }
                }

                // Если ни один дочерний объект не содержит курсор, проверяем родителя
                if (parent.IsMouseTargetable && parent.uiTransform.Contains(Input.mousePosition))
                    return parent;
            }

            return null;
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
        public static Key LMB { get; } = new();

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
                return null;
            }
        }
        public enum HotKey
        {
            CtrlZ
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