using Microsoft.Xna.Framework;
using AkiGames.UI;

namespace AkiGames.Scripts
{
    public class FactorySeedInput : InteractableComponent
    {
        private readonly TextEditSession _editSession = new();
        private Text _text;

        public string Value { get; private set; } = "";
        public string CurrentValue => _editSession.IsEditing ? _editSession.Value : Value;
        public string Placeholder { get; set; } = "Seed (blank = random)";

        public override void Awake()
        {
            image = gameObject.GetComponent<Image>();
            _text = gameObject.GetComponent<Text>();
            idleColor = new Color(30, 34, 35);
            onHoverColor = new Color(44, 52, 53);
            onOpenedColor = new Color(60, 72, 73);

            if (image != null)
                image.fillColor = idleColor;

            UpdateText();
        }

        public override void OnMouseUp()
        {
            isRedacting = true;
            _editSession.Begin(Value);
            UpdateText();
        }

        public override void Deactivate()
        {
            CommitPending();
            base.Deactivate();
        }

        public override void Update()
        {
            if (!gameObject.IsGlobalActive || !_editSession.IsEditing)
                return;

            TextEditAction action = _editSession.Update();
            if (action == TextEditAction.Commit)
            {
                CommitPending();
            }
            else if (action == TextEditAction.Cancel)
            {
                _editSession.Cancel();
                Value = _editSession.Value;
                StopInteracting();
            }

            UpdateText();
        }

        public void SetValue(string value)
        {
            Value = value?.Trim() ?? "";
            if (_editSession.IsEditing)
                _editSession.Begin(Value);
            UpdateText();
        }

        public void CommitPending()
        {
            if (!_editSession.IsEditing) return;

            Value = _editSession.Value.Trim();
            _editSession.Finish();
            StopInteracting();
            UpdateText();
        }

        private void UpdateText()
        {
            if (_text == null) return;

            if (_editSession.IsEditing)
            {
                _text.text = _editSession.DisplayValue(gameTime?.TotalGameTime.TotalMilliseconds ?? 0);
                _text.TextColor = Color.White;
                return;
            }

            _text.text = string.IsNullOrWhiteSpace(Value) ? Placeholder : Value;
            _text.TextColor = string.IsNullOrWhiteSpace(Value)
                ? new Color(146, 154, 150)
                : Color.White;
        }
    }
}
