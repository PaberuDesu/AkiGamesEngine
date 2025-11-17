using AkiGames.UI;

namespace AkiGames.Scripts.Menu
{
    public class MenuItemController : UI.DropDown.DropDown
    {
        public override void Awake()
        {
            submenu = gameObject.Children[0];
            image = gameObject.GetComponent<Image>();
            idleColor = new(45, 45, 45);
            onHoverColor = new(65, 65, 65);
            onOpenedColor = new(75, 75, 75);
        }
    }
}