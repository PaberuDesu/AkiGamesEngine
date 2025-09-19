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

            texture = new Texture2D(spriteBatch.GraphicsDevice, 1, 1);
            texture.SetData([Color.White]);
        }

        public void Draw(SpriteBatch spriteBatch)
        {
            CreateTexture(spriteBatch);

            // Находим ближайшую родительскую маску
            Image parentMask = FindParentMask();

            // Если это маска, устанавливаем stencil buffer и выходим
            if (IsMask)
            {
                if (maskId == 0) maskId = nextMaskId++;
                SetupMaskStencil(spriteBatch, maskId);
                return; // Не рисуем маску как видимое изображение
            }

            // Если есть родительская маска, настраиваем stencil test
            if (parentMask != null)
            {
                SetupStencilTest(spriteBatch, parentMask.maskId);
            }

            // Рисуем обычное изображение (не маску)
            if (fillColor.A != 0)
            {
                DrawTexture(spriteBatch, uiTransform.Bounds, fillColor);
            }

            // Восстанавливаем стандартный spriteBatch если был изменен
            if (parentMask != null)
            {
                RestoreSpriteBatch(spriteBatch);
            }
        }

        private void SetupMaskStencil(SpriteBatch spriteBatch, int maskId)
        {
            // Завершаем текущий batch
            spriteBatch.End();
            
            // Настраиваем stencil state для записи маски
            DepthStencilState stencilState = new()
            {
                StencilEnable = true,
                StencilFunction = CompareFunction.Always,
                StencilPass = StencilOperation.Replace,
                ReferenceStencil = maskId,
                DepthBufferEnable = false
            };
            
            // Специальный BlendState, который не записывает цвет
            BlendState noColorWrite = new()
            {
                ColorWriteChannels = ColorWriteChannels.None
            };
            
            // Начинаем новый batch для записи маски в stencil buffer
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                noColorWrite, // Не записываем цвет
                SamplerState.LinearClamp,
                stencilState,
                RasterizerState.CullNone
            );
            
            // Рисуем маску в stencil buffer (невидимо)
            DrawTextureToStencil(spriteBatch, uiTransform.Bounds);
            
            // Завершаем batch для маски
            spriteBatch.End();
            
            // Восстанавливаем стандартный batch для дальнейшей отрисовки
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone
            );
        }

        private void DrawTextureToStencil(SpriteBatch spriteBatch, Rectangle rect)
        {
            // Этот метод рисует текстуру только в stencil buffer, не в цветовой буфер
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
                        Color.White, // Цвет не важен, так как мы не записываем в цветовой буфер
                        uiTransform.Rotation,
                        uiTransform.origin,
                        SpriteEffects.None,
                        0
                    );
                    break;
                case ImageMode.Tile:
                    // Для плиточного режима используем fallback реализацию
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

                            spriteBatch.Draw(texture, destRect, Color.White);
                        }
                    }
                    break;
            }
        }

        private void DrawTexture(SpriteBatch spriteBatch, Rectangle rect, Color color)
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
                    if (TileEffect != null)
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
                    else
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
                    break;
            }
        }
    }
}