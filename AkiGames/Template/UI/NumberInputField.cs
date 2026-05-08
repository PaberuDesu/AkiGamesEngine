using System.Collections.Generic;
using Microsoft.Xna.Framework.Input;

namespace AkiGames.UI
{
    public class NumberInputField : InteractableComponent
    {
        private KeyboardState _currentKeyboardState;
        private KeyboardState _previousKeyboardState;

        private string _inputString = "";
        protected float result = 0;
        public bool isInteger = false;
        
        private Text textField;

        // Словарь для клавиш, которые добавляют символы
        private readonly Dictionary<Keys, string> charKeys = new()
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
            { Keys.NumPad9, "9" },
            { Keys.Add, "+" },
            { Keys.Subtract, "-" },
            { Keys.Multiply, "*" },
            { Keys.Divide, "/" },
            { Keys.Decimal, "." },
            { Keys.OemComma, "," },
            { Keys.Space, " " },
            { Keys.OemPeriod, "." }
        };

        public override void Awake()
        {
            image = gameObject.GetComponent<Image>();
            textField = gameObject.Children[0].GetComponent<Text>();
        }

        public override void Update()
        {
            if (isRedacting)
            {
                _previousKeyboardState = _currentKeyboardState;
                _currentKeyboardState = Keyboard.GetState();

                // Выход по Escape
                if (_currentKeyboardState.IsKeyDown(Keys.Escape))
                {
                    EndRedacting();
                    return;
                }
                // Enter для вычисления
                if (IsKeyPressed(Keys.Enter))
                {
                    CalculateResult();
                    EndRedacting();
                    return;
                }

                ProcessKeyboardInput();
                textField.text = _inputString;
            }

            base.Update();
        }

        private void ProcessKeyboardInput()
        {
            // Обработка клавиш, добавляющих символы
            foreach (var keyPair in charKeys)
            {
                if (IsKeyPressed(keyPair.Key))
                {
                    _inputString += keyPair.Value;
                }
            }

            // Специальные клавиши (действия)
            if (IsKeyPressed(Keys.Back) && _inputString.Length > 0)
            {
                _inputString = _inputString[..^1];
            }

            if (IsKeyPressed(Keys.Delete))
            {
                _inputString = "";
            }
        }
        private bool IsKeyPressed(Keys key) => _currentKeyboardState.IsKeyDown(key) && _previousKeyboardState.IsKeyUp(key);

        private void CalculateResult()
        {
            string cleanedInput = _inputString?.Replace(" ", "") ?? "";

            if (string.IsNullOrEmpty(_inputString))
            {
                result = 0;
            }

            try
            {
                cleanedInput = cleanedInput.Replace(",", ".");

                result = EvaluateWithDataTable(cleanedInput);
            }
            catch {}
        }

        private static float EvaluateWithDataTable(string expression)
        {
            var table = new System.Data.DataTable();
            table.Columns.Add("expression", typeof(string), expression);
            System.Data.DataRow row = table.NewRow();
            table.Rows.Add(row);
            return float.Parse((string)row["expression"]);
        }

        protected virtual void EndRedacting()
        {
            StopInteracting();
            textField.text = isInteger ? $"{(int) result}" : $"{result}";
        }

        public override void OnMouseDown()
        {
            base.OnMouseDown();
            _inputString = textField.text;
            result = float.Parse(_inputString);
        }
        public override void Deactivate()
        {
            CalculateResult();
            EndRedacting();
        }
    }
}