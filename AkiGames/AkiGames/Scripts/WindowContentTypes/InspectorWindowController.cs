using AkiGames.Core;
using AkiGames.Scripts.InspectorRedactor;
using AkiGames.Scripts.Window;
using AkiGames.UI;
using AkiGames.UI.ScrollableList;

namespace AkiGames.Scripts.WindowContentTypes
{
    public class InspectorWindowController : WindowController
    {
        private static ScrollableListController _contentList = new();

        public override void Awake()
        {
            _contentList = scrollableContent.GetComponent<ScrollableListController>();
            base.Awake();
        }

        public static void LoadFor(GameObject ObjToDescribe)
        {
            _contentList.gameObject.Children = [];
            
            if (ObjToDescribe is null) return;

            GameObject gameObjectParameters = Game1.Prefabs["InspectorContentItem"].Copy();
            gameObjectParameters.GetComponent<InspectorItemController>().component =
                InspectorGameObjectParameters.For(ObjToDescribe);
            _contentList.gameObject.AddChild(gameObjectParameters);

            foreach (GameComponent component in ObjToDescribe.Components)
            {
                GameObject newObj = Game1.Prefabs["InspectorContentItem"].Copy();
                newObj.GetComponent<InspectorItemController>().component = component;
                _contentList.gameObject.AddChild(newObj);
            }
            _contentList.Refresh();
            _contentList.gameObject.RefreshBounds();
        }
    }
}
