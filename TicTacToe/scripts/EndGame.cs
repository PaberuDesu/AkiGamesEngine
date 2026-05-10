using Microsoft.Xna.Framework.Graphics;
using AkiGames.Core;
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
            LoadTextures();
            _image = gameObject.Children[0].GetComponent<Image>();
            Close();
        }

        private static void LoadTextures()
        {
            _win ??= Game1.LoadGameTexture("Content/win.png");
            _lose ??= Game1.LoadGameTexture("Content/lose.png");
            _draw ??= Game1.LoadGameTexture("Content/draw.png");
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
