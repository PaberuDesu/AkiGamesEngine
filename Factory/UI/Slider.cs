using System;
using System.Collections.Generic;
using System.Globalization;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Input;
using AkiGames.Events;

namespace AkiGames.UI
{
    public class Slider : GameComponent
    {
        private static readonly Color TrackColor = new(15, 15, 15);
        private static readonly Color HoverTrackColor = new(26, 26, 26);
        private static readonly Color OpenedTrackColor = new(36, 36, 36);

        private int _lastTrackWidth = -1;
        private int _lastXOffset = -1;
        private bool _isDragging;
        private bool _isHovered;
        private bool _keyboardInputActive;
        private float _value;
        private string _keyboardInput = "";
        private KeyboardState _currentKeyboardState;
        private KeyboardState _previousKeyboardState;

        protected Image TrackImage { get; private set; }
        protected Image FillImage { get; private set; }
        protected UITransform FillTransform { get; private set; }
        protected UITransform ThumbTransform { get; private set; }

        protected virtual bool KeyboardInputEnabled => true;
        protected virtual bool KeyboardInputAllowsDecimal => true;
        protected virtual float KeyboardInputMin => 0f;
        protected virtual float KeyboardInputMax => 1f;

        private readonly Dictionary<Keys, string> _digitKeys = new()
        {
            { Keys.D0, "0" },
            { Keys.D1, "1" },
            { Keys.D2, "2" },
            { Keys.D3, "3" },
            { Keys.D4, "4" },
            { Keys.D5, "5" },
            { Keys.D6, "6" },
            { Keys.D7, "7" },
            { Keys.D8, "8" },
            { Keys.D9, "9" },
            { Keys.NumPad0, "0" },
            { Keys.NumPad1, "1" },
            { Keys.NumPad2, "2" },
            { Keys.NumPad3, "3" },
            { Keys.NumPad4, "4" },
            { Keys.NumPad5, "5" },
            { Keys.NumPad6, "6" },
            { Keys.NumPad7, "7" },
            { Keys.NumPad8, "8" },
            { Keys.NumPad9, "9" }
        };

        public float Value
        {
            get => _value;
            set => SetValue(value, notify: false);
        }

        public override void Awake()
        {
            TrackImage = gameObject.GetComponent<Image>();
            FillImage = FindChildImage("Fill");
            FillTransform = FillImage?.uiTransform;
            ThumbTransform = FindChildTransform("Thumb");

            SetTrackColor();
        }

        public override void Update()
        {
            if (_isDragging)
                ProcessKeyboardInput();

            RefreshSliderLayout();
        }

        public override void OnMouseEnter()
        {
            _isHovered = true;
            SetTrackColor();
        }

        public override void OnMouseExit()
        {
            _isHovered = false;
            SetTrackColor();
        }

        public override void OnMouseDown()
        {
            _isDragging = true;
            _keyboardInputActive = false;
            _keyboardInput = "";
            _currentKeyboardState = Keyboard.GetState();
            _previousKeyboardState = _currentKeyboardState;
            SetTrackColor();
            SetValueFromMouse();
        }

        public override void Drag(Vector2 cursorPosOnObj) => SetValueFromMouse();

        public override void OnMouseUp() => FinishDragging();

        public override void OnMouseUpOutside() => FinishDragging();

        protected void SetValueSilently(float value) => SetValue(value, notify: false);

        protected virtual void OnValueChanged(float value) { }

        protected virtual void OnSlidingFinished() { }

        private void SetValueFromMouse()
        {
            if (_keyboardInputActive) return;
            if (uiTransform.Bounds.Width <= 0) return;

            float percent = (Input.mousePosition.X - uiTransform.Bounds.X) / (float)uiTransform.Bounds.Width;
            SetValue(percent, notify: true);
        }

        private void ProcessKeyboardInput()
        {
            if (!KeyboardInputEnabled) return;

            _previousKeyboardState = _currentKeyboardState;
            _currentKeyboardState = Keyboard.GetState();

            foreach (KeyValuePair<Keys, string> digitKey in _digitKeys)
            {
                if (IsKeyPressed(digitKey.Key))
                    AddKeyboardInput(digitKey.Value);
            }

            if (KeyboardInputAllowsDecimal && IsDecimalPressed())
                AddDecimalInput();

            if (IsKeyPressed(Keys.Back))
                RemoveLastKeyboardInputSymbol();

            if (IsKeyPressed(Keys.Delete) || IsKeyPressed(Keys.Escape))
                ClearKeyboardInput();
        }

        private bool IsKeyPressed(Keys key) =>
            _currentKeyboardState.IsKeyDown(key) &&
            _previousKeyboardState.IsKeyUp(key);

        private bool IsDecimalPressed() =>
            IsKeyPressed(Keys.Decimal) ||
            IsKeyPressed(Keys.OemPeriod) ||
            IsKeyPressed(Keys.OemComma);

        private void AddKeyboardInput(string symbol)
        {
            _keyboardInputActive = true;
            _keyboardInput += symbol;
            ApplyKeyboardInput();
        }

        private void AddDecimalInput()
        {
            if (_keyboardInput.Contains('.')) return;

            if (string.IsNullOrEmpty(_keyboardInput))
                _keyboardInput = "0";

            AddKeyboardInput(".");
        }

        private void RemoveLastKeyboardInputSymbol()
        {
            if (_keyboardInput.Length == 0) return;

            _keyboardInput = _keyboardInput[..^1];
            _keyboardInputActive = _keyboardInput.Length > 0;
            ApplyKeyboardInput();
        }

        private void ClearKeyboardInput()
        {
            _keyboardInput = "";
            _keyboardInputActive = false;
        }

        private void ApplyKeyboardInput()
        {
            if (string.IsNullOrEmpty(_keyboardInput) || _keyboardInput == ".") return;
            if (!float.TryParse(
                    _keyboardInput,
                    NumberStyles.Float,
                    CultureInfo.InvariantCulture,
                    out float inputValue
                ))
            {
                return;
            }

            float valueRange = KeyboardInputMax - KeyboardInputMin;
            if (Math.Abs(valueRange) < 0.0001f) return;

            float clampedValue = MathHelper.Clamp(inputValue, KeyboardInputMin, KeyboardInputMax);
            SetValue((clampedValue - KeyboardInputMin) / valueRange, notify: true);
        }

        private void SetValue(float value, bool notify)
        {
            float clampedValue = MathHelper.Clamp(value, 0, 1);
            if (Math.Abs(clampedValue - _value) < 0.0001f)
            {
                RefreshSliderLayout();
                return;
            }

            _value = clampedValue;
            RefreshSliderLayout(force: true);

            if (notify)
                OnValueChanged(_value);
        }

        private void RefreshSliderLayout(bool force = false)
        {
            int trackWidth = uiTransform.Bounds.Width;
            if (trackWidth <= 0) return;

            int xOffset = (int)Math.Round(trackWidth * _value);
            if (!force && xOffset == _lastXOffset && trackWidth == _lastTrackWidth) return;

            if (FillTransform != null)
                FillTransform.Width = xOffset;

            if (ThumbTransform != null)
                ThumbTransform.OffsetMin = new Vector2(xOffset, ThumbTransform.OffsetMin.Y);

            RefreshChildBounds(FillImage?.gameObject);
            RefreshChildBounds(ThumbTransform?.gameObject);

            _lastXOffset = xOffset;
            _lastTrackWidth = trackWidth;
        }

        private void FinishDragging()
        {
            bool wasDragging = _isDragging;
            _isDragging = false;
            _keyboardInputActive = false;
            _keyboardInput = "";
            SetTrackColor();

            if (wasDragging)
                OnSlidingFinished();
        }

        private void SetTrackColor()
        {
            if (TrackImage == null) return;

            TrackImage.fillColor = _isDragging ?
                OpenedTrackColor :
                _isHovered ? HoverTrackColor : TrackColor;
        }

        private void RefreshChildBounds(GameObject child)
        {
            if (child != null)
                child.RefreshBounds(uiTransform);
        }

        private Image FindChildImage(string objectName)
        {
            foreach (GameObject child in gameObject.Children)
            {
                if (child.ObjectName == objectName)
                    return child.GetComponent<Image>();
            }

            return null;
        }

        private UITransform FindChildTransform(string objectName)
        {
            foreach (GameObject child in gameObject.Children)
            {
                if (child.ObjectName == objectName)
                    return child.uiTransform;
            }

            return null;
        }
    }
}
