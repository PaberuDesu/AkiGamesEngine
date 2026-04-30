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
        public static GameObject SelectedObject { get; private set; }

        public override void Awake()
        {
            _contentList = ResolveScrollableContent();
            base.Awake();
        }

        public static void LoadFor(GameObject ObjToDescribe)
        {
            Select(ObjToDescribe);
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
            _contentList.gameObject.AddChild(CreateAddComponentRow(ObjToDescribe));

            _contentList.Refresh();
            _contentList.gameObject.RefreshBounds();
        }

        private static GameObject CreateAddComponentRow(GameObject targetObject)
        {
            GameObject row = Game1.Prefabs["InspectorAddComponent"].Copy();
            row.Children[0].GetComponent<InspectorAddComponentDropDown>().TargetObject = targetObject;

            return row;
        }

        public static void Select(GameObject selectedObject) =>
            SelectedObject = selectedObject;
    }
}
