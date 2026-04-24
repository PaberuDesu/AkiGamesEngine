using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI;

namespace AkiGames.Scripts
{
    public class BackButtonController : GameComponent
    {
        private ExplorerWindowController _window;
        private string _text;
        private int _parentWidth = 0;

        public override void Awake()
        {
            _window = gameObject.Parent.Parent.Parent.GetComponent<ExplorerWindowController>();
            _text = gameObject.GetComponent<Text>().text;
        }

        public override void Update()
        {
            UITransform parentTransform = gameObject.Parent.uiTransform;
            if (_parentWidth != parentTransform.Bounds.Width)
            {
                uiTransform.Width = (int)Fonts.main.MeasureString(_text).X;
                _parentWidth = parentTransform.Bounds.Width;
            }
            gameObject.RefreshBounds();
        }

        public override void OnMouseUp() => _window.GoBack();
    }
}