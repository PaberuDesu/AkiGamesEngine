using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using AkiGames.UI;

namespace AkiGames.Scripts
{
    public class CellController : GameComponent
    {
        private static Texture2D _idle = null;
        private static Texture2D _cross = null;
        private static Texture2D _circle = null;
        public FieldController fieldController {private get; set;} = null;

        private Image _image = null;

        public bool IsEmpty => _image.texture == _idle;

        public override void Awake()
        {
            _image = gameObject.GetComponent<Image>();
            _image.texture = _idle;
        }

        public static void LoadContent(ContentManager content)
        {
            _idle = content.Load<Texture2D>("invis");
            _cross = content.Load<Texture2D>("cross");
            _circle = content.Load<Texture2D>("circle");
        }

        public int Sign
        {
            get => _image.texture == _cross ? 1 : _image.texture == _circle ? 0 : -1;
            set => _image.texture = value == 1 ? _cross : value == 0 ? _circle : _idle;
        }
        public void Reset() => _image.texture = _idle;
        
        public override void OnMouseUp() => fieldController.ClickOn(this);
    }
}