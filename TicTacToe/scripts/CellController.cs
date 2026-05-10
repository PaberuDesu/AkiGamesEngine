using Microsoft.Xna.Framework.Graphics;
using AkiGames.Core;
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
            LoadTextures();
            _image = gameObject.GetComponent<Image>();
            _image.texture = _idle;
        }

        private static void LoadTextures()
        {
            _idle ??= Game1.LoadGameTexture("Content/invis.png");
            _cross ??= Game1.LoadGameTexture("Content/cross.png");
            _circle ??= Game1.LoadGameTexture("Content/circle.png");
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
