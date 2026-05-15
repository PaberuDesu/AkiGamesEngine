using System;
using Microsoft.Xna.Framework;
using AkiGames.UI;

namespace AkiGames.Scripts
{
    public class JeopardyButton : InteractableComponent
    {
        public Action Clicked { private get; set; }

        public override void Awake()
        {
            image = gameObject.GetComponent<Image>();
            idleColor = new Color(30, 66, 155);
            onHoverColor = new Color(45, 94, 205);
            onOpenedColor = new Color(18, 43, 105);

            if (image != null)
                image.fillColor = idleColor;
        }

        public override void OnMouseUp()
        {
            StopInteracting();
            if (gameObject.IsGlobalActive)
                Clicked?.Invoke();
        }
    }
}
