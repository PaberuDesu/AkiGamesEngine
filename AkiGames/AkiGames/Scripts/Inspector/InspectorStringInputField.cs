using System;
using System.Reflection;
using Microsoft.Xna.Framework;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI;

namespace AkiGames.Scripts.Inspector
{
    public class InspectorStringInputField : InteractableComponent
    {
        private readonly TextEditSession _editor = new()
        {
            AllowsNewLine = true
        };

        public MemberInfo Info { private get; set; }
        public GameComponent Component { private get; set; }
        public Text TextField { private get; set; }

        public override void Awake()
        {
            image = gameObject.GetComponent<Image>();
            idleColor = new Color(15, 15, 15);
            onHoverColor = new Color(26, 26, 26);
            onOpenedColor = new Color(36, 36, 36);
            image.fillColor = idleColor;
        }

        public override void Update()
        {
            if (isRedacting)
            {
                switch (_editor.Update())
                {
                    case TextEditAction.Cancel:
                        CancelRedacting();
                        return;
                    case TextEditAction.Commit:
                        EndRedacting();
                        return;
                }

                UpdateDisplayedText();
            }

            base.Update();
        }

        public override void OnMouseDown()
        {
            base.OnMouseDown();
            _editor.Begin(TextField?.text ?? "");
            UpdateDisplayedText();
        }

        public override void Deactivate()
        {
            if (isRedacting)
            {
                EndRedacting();
                return;
            }

            base.Deactivate();
        }

        private void EndRedacting()
        {
            string value = _editor.Value;
            _editor.Finish();
            StopInteracting();
            if (TextField == null) return;

            TextField.text = value;
            SetMemberValue(value);
        }

        private void CancelRedacting()
        {
            _editor.Cancel();
            StopInteracting();
            if (TextField != null)
            {
                TextField.text = _editor.Value;
            }
        }

        private void UpdateDisplayedText()
        {
            if (TextField == null) return;
            double timeMs = gameTime?.TotalGameTime.TotalMilliseconds ?? 0;
            TextField.text = _editor.DisplayValue(timeMs);
        }

        private void SetMemberValue(string value)
        {
            if (Info is null || Component is null) return;

            try
            {
                if (Info is FieldInfo fieldInfo)
                {
                    fieldInfo.SetValue(Component, value);
                }

                if (Info is PropertyInfo propertyInfo)
                {
                    propertyInfo.SetValue(Component, value);
                }

                InspectorChangeApplier.Apply(Component);
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log($"String inspector field {Info.Name} can't be changed: {ex.Message}");
            }
        }

    }
}
