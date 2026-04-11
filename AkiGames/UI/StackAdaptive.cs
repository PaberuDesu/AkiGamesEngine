using AkiGames.Core;

namespace AkiGames.UI
{
    public class StackAdaptive : GameComponent
    {
        private int _minOffset = 10;
        private int _textBorder = 20;

        public override void Awake()
        {
            int xOffset = _minOffset;
            int textWidth;
            string text;
            foreach (GameObject child in gameObject.Children)
            {
                text = child.GetComponent<Text>()!.text;
                textWidth = (int)Core.TextRenderer.MeasureString(text).X + _textBorder;
                UITransform transform = child.uiTransform;

                transform.HorizontalAlignment = UITransform.AlignmentH.Left;
                transform.OffsetMin = new Vector2(xOffset, 0);
                transform.Width = textWidth;

                xOffset += textWidth;
            }
        }
    }
}