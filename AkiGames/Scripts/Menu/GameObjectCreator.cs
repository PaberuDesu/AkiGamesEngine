using AkiGames.Core;
using AkiGames.UI.DropDown;

namespace AkiGames.Scripts.Menu
{
    public class GameObjectCreator : DropDownItem
    {
        private WindowContentTypes.HierarchyWindowController _hierarchyWindowController = null!;

        public override void Awake() => _hierarchyWindowController = gameObject.
            Parent.Parent.Parent.Parent.Children[0].Children[1].
            GetComponent<WindowContentTypes.HierarchyWindowController>()!;

        private void CreateGameObject()
        {
            if (VeldridGame.GameMainObject is null || _hierarchyWindowController is null) return;

            GameObject newObject = new("new object");
            newObject.Components = [newObject.uiTransform];
            GameObject root = VeldridGame.GameMainObject.Children[0];
            root.AddChild(newObject);
            _hierarchyWindowController.RefreshContent(root);
        }

        public override void OnMouseUp()
        {
            CreateGameObject();
            base.OnMouseUp();
        }
    }
}