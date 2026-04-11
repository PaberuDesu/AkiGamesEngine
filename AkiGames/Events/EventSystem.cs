using Veldrid;
using Veldrid.Sdl2;
using AkiGames.Core;
using Point = AkiGames.Core.Point;
using AkiGames.UI;
using Image = AkiGames.UI.Image;

namespace AkiGames.Events
{
    public static class EventSystem
    {
        private const float MoveThreshold = 0f;
        private static Point? _previousPosition;
        private static GameObject _previousTarget = null!;
        public static GameObject MainObject { private get; set; } = null!;

        // Ссылки на глобальные объекты, устанавливаемые из VeldridGame
        private static Sdl2Window _window = null!;
        private static GraphicsDevice _graphicsDevice = null!;
        private static bool _isMouseInWindow = false;

        // Вспомогательные структуры для отслеживания состояния клавиш и кнопок мыши
        private static readonly HashSet<Veldrid.Key> _keysPressed = [];
        private static readonly HashSet<Veldrid.Key> _keysDownThisFrame = [];
        private static readonly HashSet<Veldrid.Key> _keysUpThisFrame = [];
        public static bool IsKeyPressed(Veldrid.Key key) => _keysPressed.Contains(key);
        public static bool IsKeyDown(Veldrid.Key key) => _keysDownThisFrame.Contains(key);
        public static bool IsKeyReleased(Veldrid.Key key) => _keysUpThisFrame.Contains(key);

        private static bool _leftButtonPressed;
        private static bool _rightButtonPressed;
        private static bool _leftButtonDownThisFrame;
        // private static bool _rightButtonDownThisFrame;
        private static bool _leftButtonUpThisFrame;
        private static bool _rightButtonUpThisFrame;

        public static void Initialize(Sdl2Window app_window, GraphicsDevice app_graphicsDevice)
        {
            _window = app_window;
            _graphicsDevice = app_graphicsDevice;
            _window.MouseEntered += () => _isMouseInWindow = true;
            _window.MouseLeft += () => _isMouseInWindow = false;
        }

        public static void Update(InputSnapshot snapshot)
        {
            if (_window == null || _graphicsDevice == null) return;

            // Сбрасываем состояние "в этом кадре" перед обработкой новых событий
            _keysDownThisFrame.Clear();
            _keysUpThisFrame.Clear();
            _leftButtonDownThisFrame = false;
            //_rightButtonDownThisFrame = false;
            _leftButtonUpThisFrame = false;
            _rightButtonUpThisFrame = false;

            // Обработка событий клавиатуры
            foreach (var keyEvent in snapshot.KeyEvents)
            {
                if (keyEvent.Down)
                {
                    _keysPressed.Add(keyEvent.Key);
                    _keysDownThisFrame.Add(keyEvent.Key);
                }
                else
                {
                    _keysPressed.Remove(keyEvent.Key);
                    _keysUpThisFrame.Add(keyEvent.Key);
                }
            }

            // Обработка событий мыши
            foreach (var mouseEvent in snapshot.MouseEvents)
            {
                switch (mouseEvent.MouseButton)
                {
                    case MouseButton.Left:
                        if (mouseEvent.Down)
                        {
                            _leftButtonPressed = true;
                            _leftButtonDownThisFrame = true;
                        }
                        else
                        {
                            _leftButtonPressed = false;
                            _leftButtonUpThisFrame = true;
                        }
                        break;
                    case MouseButton.Right:
                        if (mouseEvent.Down)
                        {
                            _rightButtonPressed = true;
                            //_rightButtonDownThisFrame = true;
                        }
                        else
                        {
                            _rightButtonPressed = false;
                            _rightButtonUpThisFrame = true;
                        }
                        break;
                }
            }

            // Обновляем состояние специальных клавиш (Ctrl, Z, S)
            // Присваивание свойству IsPressed автоматически обновляет _wasPressed
            Input.Ctrl.IsPressed = _keysPressed.Contains(Veldrid.Key.LControl);
            Input.Z.IsPressed = _keysPressed.Contains(Veldrid.Key.Z);
            Input.S.IsPressed = _keysPressed.Contains(Veldrid.Key.S);

            // Обновляем состояние кнопок мыши
            Input.LMB.IsPressed = _leftButtonPressed;
            Input.RMB.IsPressed = _rightButtonPressed;

            // Прокрутка
            Input.Scroll = (int)snapshot.WheelDelta;

            // Горячие клавиши
            if (Input.Hotkey != null)
            {
                _previousTarget?.ProcessHotkey((Input.HotKey)Input.Hotkey);
            }

            // Получаем позицию мыши из snapshot
            Point mousePos = new((int)snapshot.MousePosition.X, (int)snapshot.MousePosition.Y);
            if (_isMouseInWindow)
            {
                Input.mousePosition = mousePos;

                GameObject? currentTarget = FindTarget();
                if (Input.MouseHoverTarget != currentTarget)
                {
                    Input.MouseHoverTarget?.OnMouseExit();
                    Input.MouseHoverTarget = currentTarget!;
                    currentTarget?.OnMouseEnter();
                }

                // Определяем флаги LMB для удобства
                bool lmbDown = _leftButtonDownThisFrame;
                bool lmbPressed = _leftButtonPressed;
                bool lmbUp = _leftButtonUpThisFrame;
                bool rmbUp = _rightButtonUpThisFrame;

                if (lmbDown)
                {
                    if (_previousTarget is null)
                        _previousTarget = currentTarget!;
                    else
                    {
                        _previousTarget?.Deactivate();
                        _previousTarget = currentTarget!;
                    }
                    currentTarget?.OnMouseDown();
                    Input.StartPressing();
                }
                else if (lmbPressed)
                {
                    Point currentPosition = Input.mousePosition;
                    Point previousPosition = _previousPosition ?? currentPosition;
                    float dx = currentPosition.X - previousPosition.X;
                    float dy = currentPosition.Y - previousPosition.Y;
                    if (Math.Sqrt(dx * dx + dy * dy) > MoveThreshold)
                    {
                        Input.MousePressTarget?.Drag(Input.MousePressTargetOffset);
                    }
                }
                else if (lmbUp)
                {
                    if (Input.MousePressTarget != currentTarget)
                        Input.MousePressTarget?.OnMouseUpOutside();
                    else
                        currentTarget?.OnMouseUp();
                    Input.EndPressing();
                }
                else if (Input.LMB.IsReleased)
                {
                    if (Input.DeltaScroll != 0)
                        currentTarget?.OnScroll(Input.DeltaScroll);
                }

                if (rmbUp)
                {
                    currentTarget?.OnRMBUp();
                }

                _previousPosition = Input.mousePosition;
            }
        }

        private static GameObject? FindTarget()
        {
            (GameObject? obj, int zIndex) bestCandidate = (null, int.MinValue);
            if (MainObject != null)
                FindTargetInHierarchy(MainObject, ref bestCandidate);
            return bestCandidate.obj;
        }

        private static void FindTargetInHierarchy(GameObject parent, ref (GameObject? obj, int zIndex) bestCandidate)
        {
            if (!parent.IsActive) return;

            // Проверяем родителя
            if (parent.IsMouseTargetable && parent.uiTransform.Contains(Input.mousePosition))
            {
                Image? image = parent.GetComponent<Image>();
                if (image != null && image.Enabled && image.zIndex >= bestCandidate.zIndex)
                {
                    bestCandidate = (parent, image.zIndex);
                }
                Text? text = parent.GetComponent<Text>();
                if (text != null && text.Enabled && text.zIndex >= bestCandidate.zIndex)
                {
                    bestCandidate = (parent, text.zIndex);
                }
            }

            // Рекурсивно проверяем детей
            foreach (var child in parent.Children)
            {
                FindTargetInHierarchy(child, ref bestCandidate);
            }
        }

        private static bool IsCursorInWindow(Point mousePos)
        {
            if (_window == null || _graphicsDevice == null) return false;
            int width = (int)_graphicsDevice.SwapchainFramebuffer.Width;
            int height = (int)_graphicsDevice.SwapchainFramebuffer.Height;
            return mousePos.X >= 0 && mousePos.Y >= 0 && mousePos.X < width && mousePos.Y < height;
        }
    }

    // Статический класс Input – без изменений, кроме уточнения типа в HotKey enum
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
        public static int DeltaScroll => _prevScroll - _scroll;

        public static Point mousePosition;

        private static GameObject _mouseHoverTarget = null!;
        public static GameObject MouseHoverTarget
        {
            get => _mouseHoverTarget;
            set => _mouseHoverTarget = value;
        }

        private static GameObject _mousePressTarget = null!;
        public static Vector2 MousePressTargetOffset { get; private set; }
        public static GameObject MousePressTarget => _mousePressTarget;

        public static void StartPressing()
        {
            _mousePressTarget = _mouseHoverTarget;
            if (_mousePressTarget?.uiTransform?.Bounds != null)
            {
                MousePressTargetOffset = new Vector2(
                    mousePosition.X - _mousePressTarget.uiTransform.Bounds.X,
                    mousePosition.Y - _mousePressTarget.uiTransform.Bounds.Y
                );
            }
            else
            {
                MousePressTargetOffset = Vector2.Zero;
            }
        }

        public static void EndPressing()
        {
            _mousePressTarget = null!;
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