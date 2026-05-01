using AkiGames.Scripts.WindowContentTypes;

namespace AkiGames.Scripts.Hierarchy
{
    public class HierarchyExpander : GameComponent
    {
        internal bool isOpened = false;

        private HierarchyWindowController _hierarchyWindow;
        private HierarchyListItem _item;

        public override void Awake()
        {
            _hierarchyWindow = gameObject.GetAncestry()[2].GetComponent<HierarchyWindowController>();
            _item = gameObject.Parent.GetComponent<HierarchyListItem>();
        }

        public void ShowOrHideChildren()
        {
            uiTransform.LocalRotation = isOpened ? 0 : 90;

            isOpened = !isOpened;
            _hierarchyWindow.ShowChildrenOf(_item, isOpened);
        }

        public override void OnMouseDown() => ShowOrHideChildren();
    }
}