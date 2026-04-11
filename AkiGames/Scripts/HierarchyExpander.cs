using AkiGames.Core;
using AkiGames.Scripts.WindowContentTypes;

namespace AkiGames.Scripts
{
    public class HierarchyExpander : GameComponent
    {
        private static Veldrid.Texture _texture = null!;
        internal bool isOpened = false;

        private HierarchyWindowController _hierarchyWindow = null!;
        private HierarchyListItem _item = null!;

        public override void Awake()
        {
            gameObject.GetComponent<UI.Image>()!.texture = _texture;
            _hierarchyWindow = gameObject.GetAncestry()[2].GetComponent<HierarchyWindowController>()!;
            _item = gameObject.Parent.GetComponent<HierarchyListItem>()!;
        }

        public static void LoadContent() =>
            _texture = VeldridGame.UIImages.GetValueOrDefault("arrow")!;

        public override void OnMouseDown()
        {
            uiTransform.LocalRotation = isOpened ? 0 : 90;

            isOpened = !isOpened;
            _hierarchyWindow.ShowChildrenOf(_item, isOpened);
        }
    }
}