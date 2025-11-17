using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI.DropDown;

namespace AkiGames.Scripts.Menu
{
    public class SaveButton : DropDownItem
    {
        public override void OnMouseUp()
        {
            HierarchyWindowController.SaveHierarchy();
            base.OnMouseUp();
        }
    }
}