using System.Collections.Generic;
using AkiGames.Events;
using AkiGames.UI;

namespace AkiGames.Scripts.Window
{
    public abstract class WindowController : WindowComponent
    {
        public GameObject scrollableContent = null;
        public override void Awake()
        {
            foreach (GameObject child in gameObject.Children)
                MarkChildrenAsWindowComponents(child);

            base.Awake();
        }

        internal void BringToFront()
        {
            List<GameObject> windowsNew = [];
            foreach (GameObject window in gameObject.Parent.Children)
            {
                if (window != gameObject) windowsNew.Add(window);
            }
            windowsNew.Add(gameObject);
            gameObject.Parent.Children = windowsNew;
        }

        public override void OnScroll(int scrollValue) => scrollableContent?.OnScrollFromOutsideTheObject(scrollValue);
        public override void ProcessHotkey(Input.HotKey hotkey) {}
    }
}