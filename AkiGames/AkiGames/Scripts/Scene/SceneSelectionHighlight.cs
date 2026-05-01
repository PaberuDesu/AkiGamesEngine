using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI;

namespace AkiGames.Scripts.Scene
{
    internal class SceneSelectionHighlight : DrawableComponent
    {
        private static Texture2D _pixel;
        private static readonly Color FillColor = new(96, 150, 220, 24);
        private static readonly Color BorderColor = new(96, 150, 220, 170);

        public GameObject SourceObject;
        public int BorderThickness = 2;

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (SourceObject == null || SourceObject != InspectorWindowController.SelectedObject)
                return;

            Rectangle bounds = uiTransform.Bounds;
            if (bounds.Width <= 0 || bounds.Height <= 0)
                return;

            EnsureTexture(spriteBatch.GraphicsDevice);
            Rectangle? previousScissor = SetupMaskClip(spriteBatch);

            spriteBatch.Draw(_pixel, bounds, FillColor);
            DrawBorder(spriteBatch, bounds);

            if (previousScissor.HasValue)
            {
                RestoreSpriteBatch(spriteBatch, previousScissor.Value);
            }
        }

        private static void EnsureTexture(GraphicsDevice graphicsDevice)
        {
            if (_pixel != null) return;

            _pixel = new Texture2D(graphicsDevice, 1, 1);
            _pixel.SetData([Color.White]);
        }

        private void DrawBorder(SpriteBatch spriteBatch, Rectangle bounds)
        {
            int thickness = MathHelper.Clamp(BorderThickness, 1, 8);
            spriteBatch.Draw(_pixel, new Rectangle(bounds.Left, bounds.Top, bounds.Width, thickness), BorderColor);
            spriteBatch.Draw(_pixel, new Rectangle(bounds.Left, bounds.Bottom - thickness, bounds.Width, thickness), BorderColor);
            spriteBatch.Draw(_pixel, new Rectangle(bounds.Left, bounds.Top, thickness, bounds.Height), BorderColor);
            spriteBatch.Draw(_pixel, new Rectangle(bounds.Right - thickness, bounds.Top, thickness, bounds.Height), BorderColor);
        }
    }
}
