using AkiGames.Core;
using AkiGames.UI;
using AkiGames.UI.DropDown;

namespace AkiGames.Scripts.Menu.WindowsActivity
{
    public class WindowSwitch : DropDownItem
    {
        public string WindowName;
        private GameObject Window;
        private static GameObject WindowContainer;

        private void ToggleWindow()
        {
            Window ??= FindByName();
            Window?.IsActive = !Window.IsActive;
        }

        private GameObject FindByName()
        {
            WindowContainer ??= Game1.MainObject?.Children[0];
            if (WindowContainer == null) return null;

            foreach (GameObject window in WindowContainer.Children)
            {
                if (window.ObjectName == WindowName) return window;
            }

            return null;
        }

        public override void OnMouseUp()
        {
            ToggleWindow();
            base.OnMouseUp();
        }
    }
}
