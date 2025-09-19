using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using AkiGames.Scripts.Window;
using AkiGames.UI;

namespace AkiGames.Scripts.WindowContentTypes
{
    public class SceneWindowController : WindowController
    {
        private static Texture2D _tex = null;
        public override void Awake()
        {
            gameObject.Children[3].GetComponent<Image>().texture = _tex;
            base.Awake();
        }
        public static void LoadContent(ContentManager content) =>
            _tex = content.Load<Texture2D>("scene_grid");
    }
}