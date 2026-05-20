using System.Collections.Generic;
using System.Text;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AkiGames.UI
{
    public class Text : DrawableComponent, IAlignable
    {
        public string text = "";
        private string _wrappedText = "";
        protected string TextWithCurrentWrapping => HorizontalWrap == WrapModeH.None ? text : _wrappedText;
        public Color TextColor = Color.White;

        private Rectangle _prevBounds = new();
        private string _prevText = "";
        private WrapModeH _prevWrap = WrapModeH.None;
        private float _prevTextScale = 1f;
        private SpriteFont _cachedFont = null;
        private HashSet<char> _supportedCharacters = null;
        protected virtual float TextScale => 1f;

        public WrapModeH HorizontalWrap { private get; set; } = WrapModeH.None;
        public enum WrapModeH
        {
            None,//                   |text exam|ple
            DotsAfter,//              |text e...|
            NewLineControlsHeigth//   |text exam| /n |ple      |
        }

        public AlignmentH HorizontalAlignment { get; set; } = AlignmentH.Center;
        public enum AlignmentH
        {
            Left,
            Center,
            Right
        }
        public AlignmentV VerticalAlignment { get; set; } = AlignmentV.Middle;
        public enum AlignmentV
        {
            Top,
            Middle,
            Bottom
        }

        public override void Awake() => _wrappedText = text;

        protected void CopyTextStateFrom(Text source)
        {
            if (source == null) return;

            text = source.text;
            TextColor = source.TextColor;
            HorizontalAlignment = source.HorizontalAlignment;
            VerticalAlignment = source.VerticalAlignment;
            HorizontalWrap = source.HorizontalWrap;
            Enabled = source.Enabled;
            zIndex = source.zIndex;
            _wrappedText = text;
        }

        private readonly Dictionary<string, Vector2> _measureCache = [];
        protected Vector2 MeasureStringCached(string text)
        {
            string renderableText = ToRenderableText(text);
            if (_measureCache.TryGetValue(renderableText, out var size))
                return size;

            size = Fonts.main.MeasureString(renderableText);
            _measureCache[renderableText] = size;
            return size;
        }

        protected Vector2 MeasureStringScaled(string text) =>
            MeasureStringCached(text) * TextScale;

        protected string TruncateText()
        {
            float maxWidth = uiTransform.Bounds.Width;
            // Если текст помещается, возвращаем как есть
            if (MeasureStringScaled(text).X <= maxWidth) return text;

            string truncated = text;

            // Измеряем ширину многоточия
            float ellipsisWidth = MeasureStringScaled("...").X;

            // Постепенно уменьшаем текст, пока он не поместится
            while (truncated.Length > 1)
            {
                // Убираем последний символ
                truncated = truncated[..^1];

                // Проверяем, помещается ли текст с многоточием
                if (MeasureStringScaled(truncated).X + ellipsisWidth <= maxWidth)
                {
                    return truncated + "...";
                }
            }

            // Если ничего не помещается, возвращаем просто многоточие
            return "...";
        }

        protected string DivideIntoLines()
        {
            float maxWidth = uiTransform.Bounds.Width;
            if (string.IsNullOrEmpty(text))
            {
                uiTransform.Height = 0;
                return "";
            }

            // Если текст помещается, возвращаем как есть
            if (MeasureStringScaled(text).X <= maxWidth)
            {
                uiTransform.Height = (int)MeasureStringScaled(text).Y;
                return text;
            }
            // Если не помещается даже первый символ, возвращаем пустую строку
            else if (MeasureStringScaled($"{text[0]}").X > maxWidth)
            {
                uiTransform.Height = 0;
                return "";
            }

            string divided = "";
            string line = "";
            string undivided = text;

            while (undivided.Length > 0)
            {
                // считываем посимвольно
                char nextSymbol = undivided[0];
                undivided = undivided.Length > 1 ? undivided[1..] : "";

                // Проверяем, помещается ли строка с еще одним символом
                if (MeasureStringScaled(line + nextSymbol).X > maxWidth)
                {
                    divided += line + "\n";
                    line = ""; // Если нет, переходим на новую строку
                }
                line += nextSymbol;
            }
            divided += line; // Добавляем последнюю строку
            uiTransform.Height = (int)MeasureStringScaled(divided).Y;
            return divided;
        }

        public override void Update()
        {
            if (
                uiTransform.Bounds != _prevBounds ||
                text != _prevText ||
                _prevWrap != HorizontalWrap ||
                _prevTextScale != TextScale
            )
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
                _prevTextScale = TextScale;
            }
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            // Находим ближайшую родительскую маску (изображение)
            Rectangle? previousScissor = SetupMaskClip(spriteBatch);

            // Если есть родительская маска, настраиваем stencil test


            // Рисуем текст
            Rectangle rect = uiTransform.Bounds;
            string renderableText = ToRenderableText(TextWithCurrentWrapping);
            Vector2 stringSize = MeasureStringScaled(renderableText);
            int x = rect.X;
            int y = rect.Y;

            // Рассчитываем выравнивание текста
            float x_offsetMultiplier = HorizontalAlignment switch
            {
                AlignmentH.Right => 1,
                AlignmentH.Center => 0.5f,
                _ => 0
            };
            float y_offsetMultiplier = VerticalAlignment switch
            {
                AlignmentV.Bottom => 1,
                AlignmentV.Middle => 0.5f,
                _ => 0
            };

            if (x_offsetMultiplier != 0 || y_offsetMultiplier != 0)
            {
                // Учитываем выравнивание текста
                float x_offset = rect.Width - stringSize.X;
                float y_offset = rect.Height - stringSize.Y;
                x += (int)(x_offset * x_offsetMultiplier);
                y += (int)(y_offset * y_offsetMultiplier);

                // Учитываем сдвиг опорной точки из-за вращения объекта
                (x, y) = ((IAlignable)this).PositionAfterTurningParent(uiTransform, x, y);
            }

            spriteBatch.DrawString(
                Fonts.main,
                renderableText,
                new Vector2(x,y),
                TextColor,
                uiTransform.Rotation,
                uiTransform.origin,
                TextScale, SpriteEffects.None, 0
            );

            // Восстанавливаем стандартный spriteBatch если был изменен
            if (previousScissor.HasValue) RestoreSpriteBatch(spriteBatch, previousScissor.Value);
        }

        private string ToRenderableText(string value)
        {
            value ??= "";
            if (Fonts.main == null || string.IsNullOrEmpty(value))
                return value;

            EnsureSupportedCharacters();

            char fallbackCharacter = Fonts.main.DefaultCharacter ?? '?';
            StringBuilder builder = null;

            for (int i = 0; i < value.Length; i++)
            {
                char character = value[i];
                if (character == '\r' || character == '\n' || character == '\t')
                    continue;

                if (_supportedCharacters.Contains(character))
                    continue;

                builder ??= new StringBuilder(value);
                builder[i] = fallbackCharacter;
            }

            return builder?.ToString() ?? value;
        }

        private void EnsureSupportedCharacters()
        {
            if (ReferenceEquals(_cachedFont, Fonts.main) && _supportedCharacters != null)
                return;

            _cachedFont = Fonts.main;
            _supportedCharacters = _cachedFont?.Characters != null ?
                [.. _cachedFont.Characters] :
                [];

            if (_cachedFont?.DefaultCharacter is char fallbackCharacter)
                _supportedCharacters.Add(fallbackCharacter);

            _measureCache.Clear();
        }
    }
}
