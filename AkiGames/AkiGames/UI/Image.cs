using System;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AkiGames.UI
{
    public class Image : DrawableComponent
    {
        public Texture2D texture = null;
        public Color fillColor = Color.White;
        public static Effect TileEffect;

        public ImageMode imageMode = ImageMode.Stretch;
        public bool IsMask { get; set; } = false;

        public enum ImageMode
        {
            Stretch,
            Tile
        }

        private void CreateTexture(SpriteBatch spriteBatch)
        {
            if (texture != null) return;

            texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            texture.SetData([Color.White]);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (IsMask) return;

            CreateTexture(spriteBatch);

            Rectangle? previousScissor = SetupMaskClip(spriteBatch);
            if (fillColor.A != 0)
            {
                DrawTexture(spriteBatch, uiTransform.Bounds, fillColor, previousScissor.HasValue);
            }

            if (previousScissor.HasValue)
            {
                RestoreSpriteBatch(spriteBatch, previousScissor.Value);
            }
        }

        private void DrawTexture(SpriteBatch spriteBatch, Rectangle rect, Color color, bool hasMaskClip)
        {
            switch (imageMode)
            {
                case ImageMode.Stretch:
                    spriteBatch.Draw(
                        texture,
                        new Rectangle(
                            (int)(rect.X + rect.Width * uiTransform.origin.X),
                            (int)(rect.Y + rect.Height * uiTransform.origin.Y),
                            rect.Width,
                            rect.Height
                        ),
                        null,
                        color,
                        (float)(uiTransform.Rotation * Math.PI / 180.0),
                        new Vector2(
                            texture.Width * uiTransform.origin.X,
                            texture.Height * uiTransform.origin.Y
                        ),
                        SpriteEffects.None,
                        0
                    );
                    break;
                case ImageMode.Tile:
                    if (TileEffect != null && !hasMaskClip)
                    {
                        DrawTiledWithShader(spriteBatch, rect, color);
                    }
                    else
                    {
                        DrawTiledFallback(spriteBatch, rect, color);
                    }

                    break;
            }
        }

        private void DrawTiledWithShader(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            spriteBatch.End();

            TileEffect.Parameters["TilingFactors"].SetValue(
                new Vector2(
                    rect.Width / (float)texture.Width,
                    rect.Height / (float)texture.Height
                ));

            TileEffect.CurrentTechnique.Passes[0].Apply();

            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                SamplerState.LinearWrap,
                DepthStencilState.None,
                RasterizerState.CullNone,
                TileEffect
            );

            spriteBatch.Draw(texture, rect, color);

            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone
            );
        }

        private void DrawTiledFallback(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            int width = texture.Width;
            int height = texture.Height;

            for (int x = rect.Left; x < rect.Right; x += width)
            {
                for (int y = rect.Top; y < rect.Bottom; y += height)
                {
                    Rectangle destRect = new(
                        x,
                        y,
                        Math.Min(width, rect.Right - x),
                        Math.Min(height, rect.Bottom - y)
                    );

                    spriteBatch.Draw(texture, destRect, color);
                }
            }
        }
    }
}
