using Microsoft.Xna.Framework;

namespace AkiGames.UI
{
    public class InteractableComponent : GameComponent// all UI objects that need to show user can interact with them
    {
        protected Image image;
        protected Color idleColor = Color.White;
        protected Color onHoverColor = new(200, 200, 200);
        protected Color onOpenedColor = new(170, 170, 170);
        protected bool isRedacting = false;
        private bool isHovered = false;

        protected void StopInteracting()
        {
            if (gameObject.IsMouseTargetable)
            {
                isRedacting = false;
                image.fillColor = isHovered ? onHoverColor : idleColor;
            }
        }

        public override void OnMouseEnter()
        {
            if (gameObject.IsMouseTargetable)
                image.fillColor = isRedacting ? onOpenedColor : onHoverColor;
            isHovered = true;
        }
        public override void OnMouseExit()
        {
            if (gameObject.IsMouseTargetable)
                image.fillColor = isRedacting ? onOpenedColor : idleColor;
            isHovered = false;
        }
        public override void OnMouseDown()
        {
            if (gameObject.IsMouseTargetable)
            {
                isRedacting = true;
                image.fillColor = onOpenedColor;
            }
        }
        public override void Deactivate() => StopInteracting();
    }
}