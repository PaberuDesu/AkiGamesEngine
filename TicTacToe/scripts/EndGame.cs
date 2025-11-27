using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using AkiGames.UI;

namespace AkiGames.Scripts
{
    public class EndGame : GameComponent
    {
        private static Texture2D _win = null;
        private static Texture2D _lose = null;
        private static Texture2D _draw = null;
        private Image _image = null;

        public override void Awake()
        {
            _image = gameObject.Children[0].GetComponent<Image>();
            Close();
        }

        public static void LoadContent(ContentManager content)
        {
            _win = content.Load<Texture2D>("win");
            _lose = content.Load<Texture2D>("lose");
            _draw = content.Load<Texture2D>("draw");
        }
        
        public void ShowEndScreen(int winner, int player)
        {
            gameObject.IsActive = true;
            _image.texture = winner == player ? _win :
                            winner == 1 - player ? _lose :
                            winner < 0 ? _draw : null;
        }

        public void Close() => gameObject.IsActive = false;
    }
}