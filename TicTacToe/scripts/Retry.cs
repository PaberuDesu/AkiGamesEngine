using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using AkiGames.UI;

namespace AkiGames.Scripts
{
    public class Retry : GameComponent
    {
        private static Texture2D _texture = null;
        private static FieldController fieldController = null;

        public override void Awake()
        {
            gameObject.GetComponent<Image>().texture = _texture;
            fieldController = gameObject.Parent.Children[0].GetComponent<FieldController>();
        }

        public static void LoadContent(ContentManager content) =>
            _texture = content.Load<Texture2D>("retry");

        public override void OnMouseUp() => fieldController.Restart();
    }
}