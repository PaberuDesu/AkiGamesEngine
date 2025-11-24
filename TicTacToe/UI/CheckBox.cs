using AkiGames.Core;

namespace AkiGames.UI
{
    public class CheckBox : InteractableComponent
    {
        public bool value;

        public override void Awake() => image = gameObject.GetComponent<Image>();

        protected virtual void ChangeValue()
        {
            value = !value;
            image.texture = value ? Game1.UIImages["CheckboxApproved"] : Game1.UIImages["CheckboxEmpty"];
        }

        public override void OnMouseUp()
        {
            if (image != null) ChangeValue();
            StopInteracting();
        }
    }
}