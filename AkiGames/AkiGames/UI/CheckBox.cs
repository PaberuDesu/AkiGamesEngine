using AkiGames.Core;

namespace AkiGames.UI
{
    public class CheckBox : GameComponent
    {
        private Image _image;
        public bool value;
        public bool isSettable = true;

        public override void Awake() => _image = gameObject.GetComponent<Image>();
        public override void OnMouseUp()
        {
            if (_image != null) ChangeValue();
        }

        protected virtual void ChangeValue()
        {
            value = !value;
            _image.texture = value ? Game1.UIImages["CheckboxApproved"] : Game1.UIImages["CheckboxEmpty"];
        }
    }
}