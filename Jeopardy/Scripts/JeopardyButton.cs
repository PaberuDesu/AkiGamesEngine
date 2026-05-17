using System;
using Microsoft.Xna.Framework;
using AkiGames.UI;

namespace AkiGames.Scripts
{
    public class JeopardyButton : InteractableComponent
    {
        public Action Clicked { private get; set; }
        public bool UseCustomColors { get; set; }
        public bool LockVisualState { get; set; }

        public override void Awake()
        {
            image = gameObject.GetComponent<Image>();
            idleColor = new Color(30, 66, 155);
            onHoverColor = new Color(45, 94, 205);
            onOpenedColor = new Color(18, 43, 105);

            if (image != null)
                image.fillColor = idleColor;
        }

        public void ApplyColors(Color idle, Color hover, Color pressed)
        {
            idleColor = idle;
            onHoverColor = hover;
            onOpenedColor = pressed;

            if (image != null)
                image.fillColor = idleColor;
        }

        public override void OnMouseEnter()
        {
            if (LockVisualState) return;
            base.OnMouseEnter();
        }

        public override void OnMouseExit()
        {
            if (LockVisualState) return;
            base.OnMouseExit();
        }

        public override void OnMouseDown()
        {
            if (LockVisualState) return;
            base.OnMouseDown();
        }

        public override void OnMouseUp()
        {
            StopInteracting();
            if (gameObject.IsGlobalActive)
                Clicked?.Invoke();
        }
    }
}
