using Veldrid;

namespace AkiGames.Core
{
    public class SpriteFont(Texture tex, int charW, int charH)
    {
        public Texture Texture { get; } = tex;
        public int CharWidth { get; } = charW;
        public int CharHeight { get; } = charH;

        // Измеряет размер текста в пикселях (приближённо)
        public Vector2 MeasureString(string text)
        {
            if (string.IsNullOrEmpty(text))
                return Vector2.Zero;

            int maxWidth = 0;
            int currentWidth = 0;
            int lines = 1;

            foreach (char c in text)
            {
                if (c == '\n')
                {
                    lines++;
                    if (currentWidth > maxWidth) maxWidth = currentWidth;
                    currentWidth = 0;
                }
                else
                {
                    currentWidth += CharWidth;
                }
            }
            if (currentWidth > maxWidth) maxWidth = currentWidth;

            return new Vector2(maxWidth, lines * CharHeight);
        }
    }
}