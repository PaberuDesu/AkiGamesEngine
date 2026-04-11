using System;
using SixLabors.Fonts;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using SixLabors.ImageSharp.Drawing.Processing;
using SixLabors.ImageSharp.Processing;
using Veldrid;

namespace AkiGames.Core
{
    public static class TextRenderer
    {
        private static SixLabors.Fonts.Font _font = null!;

        public static void LoadFont(string fontPath, float fontSize)
        {
            var fontCollection = new FontCollection();
            var family = fontCollection.Add(fontPath);
            _font = family.CreateFont(fontSize, SixLabors.Fonts.FontStyle.Regular);
        }

        public static Texture RenderTextToTexture(GraphicsDevice gd, string text, Color color, out int width, out int height)
        {
            if (_font == null)
            {
                width = 1; height = 1;
                var dummy = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D(1, 1, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
                gd.UpdateTexture(dummy, new byte[] { 255, 255, 255, 255 }, 0, 0, 0, 1, 1, 1, 0, 0);
                return dummy;
            }

            var textOptions = new TextOptions(_font) { Dpi = 96 };
            var size = TextMeasurer.MeasureSize(text, textOptions);
            width = (int)Math.Ceiling(size.Width);
            height = (int)Math.Ceiling(size.Height);
            if (width == 0) width = 1;
            if (height == 0) height = 1;

            using var image = new Image<Rgba32>(width, height);
            image.Mutate(ctx =>
            {
                ctx.Clear(SixLabors.ImageSharp.Color.Transparent);
                var sixColor = new SixLabors.ImageSharp.Color(new Rgba32(color.R, color.G, color.B, color.A));
                ctx.DrawText(text, _font, sixColor, new SixLabors.ImageSharp.PointF(0, 0));
            });

            var texture = gd.ResourceFactory.CreateTexture(TextureDescription.Texture2D((uint)width, (uint)height, 1, 1, PixelFormat.R8_G8_B8_A8_UNorm, TextureUsage.Sampled));
            var pixels = new byte[width * height * 4];
            image.CopyPixelDataTo(pixels);
            gd.UpdateTexture(texture, pixels, 0, 0, 0, (uint)width, (uint)height, 1, 0, 0);

            return texture;
        }

        public static Vector2 MeasureString(string text)
        {
            if (_font == null) return Vector2.Zero;
            var textOptions = new TextOptions(_font) { Dpi = 96 };
            var size = TextMeasurer.MeasureSize(text, textOptions);
            return new Vector2((float)size.Width, (float)size.Height);
        }
    }
}