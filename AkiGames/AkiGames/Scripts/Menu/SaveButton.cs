using AkiGames.Scripts.WindowContentTypes;

namespace AkiGames.Scripts.Menu
{
    public class SaveButton : SubmenuItemController
    {
        public override void OnMouseUp()
        {
            HierarchyWindowController.SaveHierarchy();
            base.OnMouseUp();
        }
    }
}