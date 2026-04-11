using Veldrid;
using AkiGames.Core;
using Rectangle = AkiGames.Core.Rectangle;
using Color = AkiGames.Core.Color;

namespace AkiGames.UI
{
    public class Image : DrawableComponent
    {
        public Texture? texture = null;
        public Color fillColor = Color.White;
        // Эффект для тайлинга – пока не реализован в Veldrid, оставляем заглушкой
        // public static Effect TileEffect; 

        public ImageMode imageMode = ImageMode.Stretch;
        public bool IsMask { get; set; } = false;
        private static int nextMaskId = 1;
        internal int maskId = 0;

        public enum ImageMode
        {
            Stretch,
            Tile
        }

        private void CreateTexture(SpriteBatch spriteBatch)
        {
            if (texture != null) return;
        
            var texDesc = TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled);
            texture = spriteBatch.GraphicsDevice.ResourceFactory.CreateTexture(texDesc);
            spriteBatch.GraphicsDevice.UpdateTexture(texture, new byte[] { 255, 255, 255, 255 }, 0, 0, 0, 1, 1, 1, 0, 0);
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            CreateTexture(spriteBatch);

            // Находим ближайшую родительскую маску (пока заглушка)
            Image? parentMask = FindParentMask();

            // Если это маска – настраиваем stencil и выходим (временно отключено)
            if (IsMask)
            {
                if (maskId == 0) maskId = nextMaskId++;
                // SetupMaskStencil(spriteBatch, maskId); // TODO: реализовать
                return; // Не рисуем маску как видимое изображение
            }

            // Если есть родительская маска, настраиваем stencil test (заглушка)
            if (parentMask != null)
            {
                // SetupStencilTest(spriteBatch, parentMask.maskId); // TODO: реализовать
            }

            // Рисуем обычное изображение (не маску)
            if (fillColor.A != 0)
            {
                DrawTexture(spriteBatch, uiTransform.Bounds, fillColor);
            }

            // Восстанавливаем стандартный spriteBatch (заглушка)
            // if (parentMask != null) RestoreSpriteBatch(spriteBatch);
        }

        private void DrawTexture(SpriteBatch spriteBatch, Rectangle rect, Color color)
        {
            switch (imageMode)
            {
                case ImageMode.Stretch:
                    float rad = uiTransform.Rotation * (float)Math.PI / 180.0f;
                    spriteBatch.Draw(texture!, rect, color,  rad, new Vector2(uiTransform.origin.X * rect.Width, uiTransform.origin.Y * rect.Height));
                    break;

                case ImageMode.Tile:
                    if (texture == null)
                    {
                        Console.WriteLine("Tile error: texture is null");
                        return;
                    }
                    int tileW = (int)texture.Width;
                    int tileH = (int)texture.Height;
                    if (tileW <= 0 || tileH <= 0)
                    {
                        Console.WriteLine("Tile error: invalid texture size");
                        return;
                    }
                    for (int x = rect.X; x < rect.X + rect.Width; x += tileW)
                    {
                        for (int y = rect.Y; y < rect.Y + rect.Height; y += tileH)
                        {
                            int w = Math.Min(tileW, rect.X + rect.Width - x);
                            int h = Math.Min(tileH, rect.Y + rect.Height - y);
                            Rectangle dest = new(x, y, w, h);
                            spriteBatch.Draw(texture, dest, color, 0, Vector2.Zero); // без возможности вращать
                        }
                    }
                    break;
            }
        }
    }
}