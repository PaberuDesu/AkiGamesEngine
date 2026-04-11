using Veldrid;
using AkiGames.Core;
using Color = AkiGames.Core.Color;
using TextRenderer = AkiGames.Core.TextRenderer;
using Rectangle = AkiGames.Core.Rectangle;

namespace AkiGames.UI
{
    public class Text : DrawableComponent, IAlignable
    {
        public string text = "";
        private string _wrappedText = "";
        private string TextWithCurrentWrapping => HorizontalWrap == WrapModeH.None ? text : _wrappedText;
        public Color TextColor = Color.White;

        private Rectangle _prevBounds = new(0, 0, 0, 0);
        private string _prevText = "";
        private WrapModeH _prevWrap = WrapModeH.None;

        private Texture _cachedTexture = null!;
        private string _lastRenderedText = "";
        private Color _lastColor;
        private int _lastTextureWidth, _lastTextureHeight;

        public WrapModeH HorizontalWrap { get; set; } = WrapModeH.None;
        public enum WrapModeH
        {
            None,
            DotsAfter,
            NewLineControlsHeigth
        }

        public AlignmentH HorizontalAlignment { get; set; } = AlignmentH.Center;
        public enum AlignmentH { Left, Center, Right }
        public AlignmentV VerticalAlignment { get; set; } = AlignmentV.Middle;
        public enum AlignmentV { Top, Middle, Bottom }

        public override void Awake() => _wrappedText = text;

        private Vector2 MeasureStringCached(string text)
        {
            return TextRenderer.MeasureString(text);
        }

        private string TruncateText()
        {
            float maxWidth = uiTransform.Bounds.Width;
            if (MeasureStringCached(text).X <= maxWidth) return text;

            string truncated = text;
            float ellipsisWidth = MeasureStringCached("...").X;

            while (truncated.Length > 1)
            {
                truncated = truncated[..^1];
                if (MeasureStringCached(truncated).X + ellipsisWidth <= maxWidth)
                    return truncated + "...";
            }
            return "...";
        }

        private string DivideIntoLines()
        {
            float maxWidth = uiTransform.Bounds.Width;
            if (MeasureStringCached(text).X <= maxWidth)
            {
                uiTransform.Height = (int)MeasureStringCached(text).Y;
                return text;
            }
            if (MeasureStringCached($"{text[0]}").X > maxWidth)
            {
                uiTransform.Height = 0;
                return "";
            }

            string divided = "";
            string line = "";
            string undivided = text;

            while (undivided.Length > 0)
            {
                char nextSymbol = undivided[0];
                undivided = undivided.Length > 1 ? undivided[1..] : "";

                if (MeasureStringCached(line + nextSymbol).X > maxWidth)
                {
                    divided += line + "\n";
                    line = "";
                }
                line += nextSymbol;
            }
            divided += line;
            uiTransform.Height = (int)MeasureStringCached(divided).Y;
            return divided;
        }

        public override void Update()
        {
            if (uiTransform.Bounds != _prevBounds || text != _prevText || _prevWrap != HorizontalWrap)
            {
                switch (HorizontalWrap)
                {
                    case WrapModeH.DotsAfter:
                        _wrappedText = TruncateText();
                        break;
                    case WrapModeH.NewLineControlsHeigth:
                        _wrappedText = DivideIntoLines();
                        break;
                }
                _prevBounds = uiTransform.Bounds;
                _prevText = text;
                _prevWrap = HorizontalWrap;
                _cachedTexture = null!; // пометим для пересоздания
            }
        }

        private void UpdateCachedTexture(GraphicsDevice gd)
        {
            string currentText = TextWithCurrentWrapping;
            if (_cachedTexture != null && _lastRenderedText == currentText && _lastColor == TextColor)
                return;
        
            _cachedTexture?.Dispose();
            int width, height;
            _cachedTexture = TextRenderer.RenderTextToTexture(gd, currentText, TextColor, out width, out height);
            _lastRenderedText = currentText;
            _lastColor = TextColor;
            _lastTextureWidth = width;
            _lastTextureHeight = height;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            if (string.IsNullOrEmpty(TextWithCurrentWrapping))
                return;

            UpdateCachedTexture(spriteBatch.GraphicsDevice);
            if (_cachedTexture == null) return;

            Rectangle rect = uiTransform.Bounds;
            int x = rect.X;
            int y = rect.Y;

            // Горизонтальное выравнивание
            switch (HorizontalAlignment)
            {
                case AlignmentH.Center:
                    x += (rect.Width - _lastTextureWidth) / 2;
                    break;
                case AlignmentH.Right:
                    x += rect.Width - _lastTextureWidth;
                    break;
            }

            // Вертикальное выравнивание
            switch (VerticalAlignment)
            {
                case AlignmentV.Middle:
                    y += (rect.Height - _lastTextureHeight) / 2;
                    break;
                case AlignmentV.Bottom:
                    y += rect.Height - _lastTextureHeight;
                    break;
            }

            spriteBatch.Draw(_cachedTexture, new Rectangle(x, y, _lastTextureWidth, _lastTextureHeight), TextColor, 0, Vector2.Zero); // пока без возможности вращать
        }
    }
}