using AkiGames.Core;
using AkiGames.Scripts.Window;
using AkiGames.UI.ScrollableList;

namespace AkiGames.Scripts.WindowContentTypes
{
    public class InspectorWindowController : WindowController
    {
        private static ScrollableListController _contentList = new();

        public override void Awake()
        {
            scrollableContent = gameObject.Children[3].Children[0].Children[0];
            _contentList = scrollableContent.GetComponent<ScrollableListController>()!;
            base.Awake();
        }

        public static void LoadFor(GameObject? ObjToDescribe)
        {
            _contentList.gameObject.Children = [];
            
            if (ObjToDescribe is null) return;

            foreach (GameComponent component in ObjToDescribe.Components)
            {
                GameObject newObj = VeldridGame.Prefabs["InspectorContentItem"].Copy();
                newObj.GetComponent<InspectorItemController>()!.component = component;
                _contentList.gameObject.AddChild(newObj);
            }
            _contentList.Refresh();
            _contentList.gameObject.RefreshBounds();
        }
    }
}