using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using AkiGames.UI;
using AkiGames.Scripts.WindowContentTypes;

namespace AkiGames.Scripts
{
    public class HierarchyExpander : GameComponent
    {
        private static Texture2D _texture = null;
        internal bool isOpened = false;

        private HierarchyWindowController _hierarchyWindow;
        private HierarchyListItem _item;

        public override void Awake()
        {
            gameObject.GetComponent<Image>().texture = _texture;
            _hierarchyWindow = gameObject.GetAncestry()[2].GetComponent<HierarchyWindowController>();
            _item = gameObject.Parent.GetComponent<HierarchyListItem>();
        }

        public static void LoadContent(ContentManager content) =>
            _texture = content.Load<Texture2D>("arrow");

        public override void OnMouseDown()
        {
            uiTransform.LocalRotation = isOpened ? 0 : 90;

            isOpened = !isOpened;
            _hierarchyWindow.ShowChildrenOf(_item, isOpened);
        }
    }
}