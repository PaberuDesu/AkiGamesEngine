using Veldrid;
using AkiGames.Core;
using Color = AkiGames.Core.Color;
using TextRenderer = AkiGames.Core.TextRenderer;
using Rectangle = AkiGames.Core.Rectangle;

namespace AkiGames.UI
{
    public class Text : DrawableComponent, IAlignable
    {
        protected bool doesNeedRedraw = true;
        public void Invalidate() => doesNeedRedraw = true;
        
        private string _text = "";
        public string text
        {
            get => _text;
            set
            {
                if (_text != value)
                {
                    _text = value;
                    Invalidate();
                }
            }
        }
        private string _wrappedText = "";
        private string TextWithCurrentWrapping => HorizontalWrap == WrapModeH.None ? text : _wrappedText;
        private Color _textColor = Color.White;
        public Color TextColor
        {
            get => _textColor;
            set
            {
                if (!_textColor.Equals(value))
                {
                    _textColor = value;
                    Invalidate();
                }
            }
        }

        private Rectangle _prevBounds = new(0, 0, 0, 0);
        private string _prevText = "";
        private WrapModeH _prevWrap = WrapModeH.None;

        private Texture? _cachedTexture = null;

        private WrapModeH _horizontalWrap = WrapModeH.None;
        public WrapModeH HorizontalWrap
        {
            get => _horizontalWrap;
            set
            {
                if (_horizontalWrap != value)
                {
                    _horizontalWrap = value;
                    Invalidate();
                }
            }
        }
        public enum WrapModeH
        {
            None,
            DotsAfter,
            NewLineControlsHeigth
        }

        private AlignmentH _horizontalAlignment = AlignmentH.Center;
        public AlignmentH HorizontalAlignment
        {
            get => _horizontalAlignment;
            set
            {
                if (_horizontalAlignment != value)
                {
                    _horizontalAlignment = value;
                    Invalidate();
                }
            }
        }
        public enum AlignmentH { Left, Center, Right }
        private AlignmentV _verticalAlignment = AlignmentV.Middle;
        public AlignmentV VerticalAlignment
        {
            get => _verticalAlignment;
            set
            {
                if (_verticalAlignment != value)
                {
                    _verticalAlignment = value;
                    Invalidate();
                }
            }
        }
        public enum AlignmentV { Top, Middle, Bottom }
        

        public override void Awake()
        {
            base.Awake();
            _wrappedText = _text;
            if (!string.IsNullOrEmpty(_text))
                Invalidate();
        }

        private static Vector2 MeasureStringCached(string text) =>
            TextRenderer.MeasureString(text);

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
                _cachedTexture = null; // пометим для пересоздания
            }
        }

        private void UpdateCachedTexture(GraphicsDevice gd)
        {
            string currentText = TextWithCurrentWrapping;
            if (!doesNeedRedraw && _cachedTexture != null)
                return;
        
            _cachedTexture?.Dispose();
            _cachedTexture = TextRenderer.RenderTextToTexture(gd, currentText, TextColor, out int width, out int height);
            doesNeedRedraw = false;
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

            int lastTextureWidth = (int)_cachedTexture.Width;
            int lastTextureHeight = (int)_cachedTexture.Height;

            // Горизонтальное выравнивание
            switch (HorizontalAlignment)
            {
                case AlignmentH.Center:
                    x += (rect.Width - lastTextureWidth) / 2;
                    break;
                case AlignmentH.Right:
                    x += rect.Width - lastTextureWidth;
                    break;
            }

            // Вертикальное выравнивание
            switch (VerticalAlignment)
            {
                case AlignmentV.Middle:
                    y += (rect.Height - lastTextureHeight) / 2;
                    break;
                case AlignmentV.Bottom:
                    y += rect.Height - lastTextureHeight;
                    break;
            }

            spriteBatch.Draw(_cachedTexture, new Rectangle(x, y, lastTextureWidth, lastTextureHeight), TextColor, 0, Vector2.Zero); // пока без возможности вращать
        }
    }
}