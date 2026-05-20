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
            TextInputBuffer.Clear();
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
            TextInputBuffer.Clear();
        }

        public void Cancel()
        {
            Value = _originalValue;
            IsEditing = false;
            TextInputBuffer.Clear();
        }

        public string DisplayValue(double timeMs) => Value + Cursor(timeMs);

        private void ProcessKeyboardInput()
        {
            foreach (char character in TextInputBuffer.Consume())
            {
                if (!char.IsControl(character))
                    Value += character;
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

    public static class TextInputBuffer
    {
        private static readonly Queue<char> _pendingCharacters = new();

        public static void Enqueue(char character)
        {
            if (!char.IsControl(character))
                _pendingCharacters.Enqueue(character);
        }

        public static IEnumerable<char> Consume()
        {
            while (_pendingCharacters.Count > 0)
                yield return _pendingCharacters.Dequeue();
        }

        public static void Clear() => _pendingCharacters.Clear();
    }
}
