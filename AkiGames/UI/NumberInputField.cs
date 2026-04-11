using AkiGames.Events;
using Key = Veldrid.Key;

namespace AkiGames.UI
{
    public class NumberInputField : InteractableComponent
    {
        private string _inputString = "";
        protected float result = 0;
        public bool isInteger = false;
        
        private Text textField = null!;

        // Словарь для клавиш, которые добавляют символы (используем Veldrid.Key)
        private readonly Dictionary<Key, string> charKeys = new()
        {
            // Основные цифры
            { Key.Number0, "0" }, { Key.Number1, "1" }, { Key.Number2, "2" },
            { Key.Number3, "3" }, { Key.Number4, "4" }, { Key.Number5, "5" },
            { Key.Number6, "6" }, { Key.Number7, "7" }, { Key.Number8, "8" }, { Key.Number9, "9" },
            // Цифровая клавиатура
            { Key.Keypad0, "0" }, { Key.Keypad1, "1" }, { Key.Keypad2, "2" }, { Key.Keypad3, "3" },
            { Key.Keypad4, "4" }, { Key.Keypad5, "5" }, { Key.Keypad6, "6" }, { Key.Keypad7, "7" },
            { Key.Keypad8, "8" }, { Key.Keypad9, "9" }, { Key.KeypadPlus, "+" }, { Key.KeypadMinus, "-" },
            { Key.KeypadMultiply, "*" }, { Key.KeypadDivide, "/" }, { Key.KeypadPeriod, "." },
            // Знаки препинания и прочее
            { Key.Comma, "," }, { Key.Space, " " }, { Key.Period, "." }, { Key.Minus, "-" }, { Key.Plus, "+" }
        };
        public override void Awake()
        {
            image = gameObject.GetComponent<Image>()!;
            textField = gameObject.Children[0].GetComponent<Text>()!;
        }

        public override void Update()
        {
            if (isRedacting)
            {
                // Выход по Escape
                if (EventSystem.IsKeyPressed(Key.Escape))
                {
                    EndRedacting();
                    return;
                }
                // Enter для вычисления
                if (EventSystem.IsKeyPressed(Key.Enter))
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
            foreach (var keyPair in charKeys)
            {
                if (EventSystem.IsKeyPressed(keyPair.Key))
                {
                    _inputString += keyPair.Value;
                }
            }

            if (EventSystem.IsKeyPressed(Key.BackSpace) && _inputString.Length > 0)
            {
                _inputString = _inputString[..^1];
            }

            if (EventSystem.IsKeyPressed(Key.Delete))
            {
                _inputString = "";
            }
        }

        private void CalculateResult()
        {
            string cleanedInput = _inputString?.Replace(" ", "") ?? "";
            if (string.IsNullOrEmpty(cleanedInput))
            {
                result = 0;
                return;
            }

            try
            {
                cleanedInput = cleanedInput.Replace(",", ".");
                result = EvaluateWithDataTable(cleanedInput);
            }
            catch { }
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
            textField.text = isInteger ? ((int)result).ToString() : result.ToString();
        }

        public override void OnMouseDown()
        {
            base.OnMouseDown();
            _inputString = textField.text;
            if (float.TryParse(_inputString, out float parsed))
                result = parsed;
        }

        public override void Deactivate()
        {
            CalculateResult();
            EndRedacting();
        }
    }
}