using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AkiGames.UI;

namespace AkiGames.Scripts
{
    internal class SceneGridImage : Image
    {
        private const float ScenePixelsPerTile = 30f;
        public float TileScale = 1f;

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (IsMask || imageMode != ImageMode.Tile || texture == null)
            {
                base.Draw(spriteBatch);
                return;
            }

            Rectangle? previousScissor = SetupMaskClip(spriteBatch);

            if (fillColor.A != 0)
            {
                DrawCenteredScaledTiles(spriteBatch, uiTransform.Bounds);
            }

            if (previousScissor.HasValue)
            {
                RestoreSpriteBatch(spriteBatch, previousScissor.Value);
            }
        }

        private void DrawCenteredScaledTiles(SpriteBatch spriteBatch, Rectangle rect)
        {
            int tileWidth = Math.Max(1, (int)Math.Round(ScenePixelsPerTile * TileScale));
            int tileHeight = Math.Max(1, (int)Math.Round(ScenePixelsPerTile * TileScale));
            Point center = rect.Center;

            int firstX = center.X - (int)Math.Ceiling((center.X - rect.Left) / (float)tileWidth) * tileWidth;
            int firstY = center.Y - (int)Math.Ceiling((center.Y - rect.Top) / (float)tileHeight) * tileHeight;

            for (int x = firstX; x < rect.Right; x += tileWidth)
            {
                for (int y = firstY; y < rect.Bottom; y += tileHeight)
                {
                    Rectangle destination = new(x, y, tileWidth, tileHeight);
                    Rectangle clippedDestination = Rectangle.Intersect(destination, rect);
                    if (clippedDestination.Width <= 0 || clippedDestination.Height <= 0) continue;

                    spriteBatch.Draw(
                        texture,
                        clippedDestination,
                        GetSourceRectangle(destination, clippedDestination),
                        fillColor
                    );
                }
            }
        }

        private Rectangle GetSourceRectangle(Rectangle destination, Rectangle clippedDestination)
        {
            float sourceX = (clippedDestination.X - destination.X) / (float)destination.Width * texture.Width;
            float sourceY = (clippedDestination.Y - destination.Y) / (float)destination.Height * texture.Height;
            float sourceWidth = clippedDestination.Width / (float)destination.Width * texture.Width;
            float sourceHeight = clippedDestination.Height / (float)destination.Height * texture.Height;

            int x = Math.Clamp((int)Math.Floor(sourceX), 0, texture.Width - 1);
            int y = Math.Clamp((int)Math.Floor(sourceY), 0, texture.Height - 1);
            int width = Math.Clamp((int)Math.Ceiling(sourceWidth), 1, texture.Width - x);
            int height = Math.Clamp((int)Math.Ceiling(sourceHeight), 1, texture.Height - y);

            return new Rectangle(x, y, width, height);
        }
    }
}
