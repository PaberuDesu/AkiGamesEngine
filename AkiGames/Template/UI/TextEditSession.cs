using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace AkiGames.UI
{
    public enum TextEditAction
    {
        None,
        Commit,
        Cancel
    }

    public class TextEditSession
    {
        private const double CursorBlinkMs = 500;

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

        private KeyboardState _currentKeyboardState;
        private KeyboardState _previousKeyboardState;
        private string _originalValue = "";

        public string Value { get; private set; } = "";
        public bool IsEditing { get; private set; }
        public bool AllowsNewLine { get; set; }

        public void Begin(string value)
        {
            Value = value ?? "";
            _originalValue = Value;
            IsEditing = true;
            _currentKeyboardState = Keyboard.GetState();
            _previousKeyboardState = _currentKeyboardState;
        }

        public TextEditAction Update()
        {
            if (!IsEditing) return TextEditAction.None;

            _previousKeyboardState = _currentKeyboardState;
            _currentKeyboardState = Keyboard.GetState();

            if (IsKeyPressed(Keys.Escape))
            {
                return TextEditAction.Cancel;
            }

            if (IsKeyPressed(Keys.Enter))
            {
                if (AllowsNewLine && IsShiftPressed())
                {
                    Value += "\n";
                }
                else
                {
                    return TextEditAction.Commit;
                }
            }

            ProcessKeyboardInput();
            return TextEditAction.None;
        }

        public void Finish()
        {
            IsEditing = false;
        }

        public void Cancel()
        {
            Value = _originalValue;
            IsEditing = false;
        }

        public string DisplayValue(double timeMs) => Value + Cursor(timeMs);

        private void ProcessKeyboardInput()
        {
            bool shiftPressed = IsShiftPressed();
            bool capsLock = _currentKeyboardState.CapsLock;

            for (Keys key = Keys.A; key <= Keys.Z; key++)
            {
                if (!IsKeyPressed(key)) continue;

                char character = (char)('a' + (key - Keys.A));
                bool upper = shiftPressed ^ capsLock;
                Value += upper ? char.ToUpper(character) : character;
            }

            foreach (var keyPair in _symbolKeys)
            {
                if (IsKeyPressed(keyPair.Key))
                {
                    Value += shiftPressed ? keyPair.Value.shifted : keyPair.Value.normal;
                }
            }

            if (IsKeyPressed(Keys.Back) && Value.Length > 0)
            {
                Value = Value[..^1];
            }

            if (IsKeyPressed(Keys.Delete))
            {
                Value = "";
            }
        }

        private bool IsKeyPressed(Keys key) =>
            _currentKeyboardState.IsKeyDown(key) && _previousKeyboardState.IsKeyUp(key);

        private bool IsShiftPressed() =>
            _currentKeyboardState.IsKeyDown(Keys.LeftShift) ||
            _currentKeyboardState.IsKeyDown(Keys.RightShift);

        private string Cursor(double timeMs)
        {
            if (!IsEditing) return "";
            return (int)(timeMs / CursorBlinkMs) % 2 == 0 ? "_" : "";
        }
    }
}
