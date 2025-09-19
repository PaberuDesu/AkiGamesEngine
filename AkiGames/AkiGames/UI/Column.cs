using Microsoft.Xna.Framework;

namespace AkiGames.UI
{
    public class Column : GameComponent
    {
        protected int itemHeight = 25;
        public int Spacing { protected get; set; } = 0;

        public override void Awake() => Refresh();

        public void Refresh()
        {
            int yOffset = 0;
            foreach (GameObject child in gameObject.Children)
            {
                if (!child.IsActive) continue;
                UITransform childTransform = child.uiTransform;
                childTransform.OffsetMin = new Vector2(0, yOffset);
                if (childTransform.Height == 0) childTransform.Height = itemHeight;
                yOffset += childTransform.Height + Spacing;
            }
            uiTransform.Height = itemHeight * gameObject.Children.Count;
        }
    }
}