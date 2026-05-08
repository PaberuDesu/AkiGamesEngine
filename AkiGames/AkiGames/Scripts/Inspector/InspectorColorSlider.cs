using Microsoft.Xna.Framework;
using AkiGames.UI;

namespace AkiGames.Scripts.Inspector
{
    public enum InspectorColorChannel
    {
        R,
        G,
        B,
        A
    }

    public class InspectorColorSlider : Slider
    {
        private InspectorColorInputField _inputField;
        private Text _valueText;

        public InspectorColorChannel channel;
        protected override bool KeyboardInputAllowsDecimal => false;
        protected override float KeyboardInputMax => 255f;

        public override void Awake()
        {
            base.Awake();
            _valueText = FindSiblingText("Value");
        }

        protected override void OnValueChanged(float value) =>
            _inputField?.SetChannel(channel, ToByte(value));

        protected override void OnSlidingFinished() => _inputField?.ApplyPendingValue();

        internal void RefreshValue()
        {
            if (_inputField == null) return;

            int value = _inputField.GetChannel(channel);
            if (_valueText != null)
                _valueText.text = $"{value}";

            if (FillImage != null)
                FillImage.fillColor = GetFillColor(value);
            SetValueSilently(value / 255f);
        }

        private Color GetFillColor(int value)
        {
            return channel switch
            {
                InspectorColorChannel.R => new Color(value, 70, 70),
                InspectorColorChannel.G => new Color(70, value, 70),
                InspectorColorChannel.B => new Color(70, 70, value),
                InspectorColorChannel.A => new Color(value, value, value),
                _ => Color.White
            };
        }

        internal void Bind(InspectorColorInputField inputField)
        {
            _inputField = inputField;
            RefreshValue();
        }

        private Text FindSiblingText(string objectName)
        {
            foreach (GameObject child in gameObject.Parent?.Children ?? [])
            {
                if (child.ObjectName == objectName)
                    return child.GetComponent<Text>();
            }

            return null;
        }

        private static int ToByte(float value) =>
            (int)MathHelper.Clamp((float)System.Math.Round(value * 255), 0, 255);
    }
}
