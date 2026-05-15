using System;
using Microsoft.Xna.Framework;
using AkiGames.UI;

namespace AkiGames.Scripts
{
    public class JeopardyTextInput : InteractableComponent
    {
        private readonly TextEditSession _editSession = new();
        private Text _text;

        public string Value { get; private set; } = "";
        public string Placeholder { get; set; } = "Team name";
        public Action<string> Submitted { private get; set; }

        public override void Awake()
        {
            image = gameObject.GetComponent<Image>();
            _text = gameObject.GetComponent<Text>();
            idleColor = new Color(15, 20, 46);
            onHoverColor = new Color(24, 34, 76);
            onOpenedColor = new Color(34, 48, 110);

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
            Commit();
            base.Deactivate();
        }

        public override void Update()
        {
            if (!gameObject.IsGlobalActive || !_editSession.IsEditing) return;

            TextEditAction action = _editSession.Update();
            if (action == TextEditAction.Commit)
            {
                Commit();
            }
            else if (action == TextEditAction.Cancel)
            {
                _editSession.Cancel();
                Value = _editSession.Value;
                StopInteracting();
            }

            UpdateText();
        }

        public void Clear()
        {
            Value = "";
            if (_editSession.IsEditing)
                _editSession.Begin(Value);
            UpdateText();
        }

        private void Commit()
        {
            if (!_editSession.IsEditing) return;

            Value = _editSession.Value.Trim();
            _editSession.Finish();
            Submitted?.Invoke(Value);
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
            _text.TextColor = string.IsNullOrWhiteSpace(Value) ?
                new Color(150, 160, 190) :
                Color.White;
        }
    }
}
