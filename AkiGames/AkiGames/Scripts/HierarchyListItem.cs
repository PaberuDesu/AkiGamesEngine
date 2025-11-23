using System.Collections.Generic;
using AkiGames.Scripts.WindowContentTypes;

namespace AkiGames.Scripts
{
    public class HierarchyListItem() : ContentItemController
    {
        internal List<HierarchyListItem> childItems = [];
        private HierarchyExpander _opener = null;
        internal bool IsOpened => _opener?.isOpened ?? false;

        public override void Start()
        {
            _opener = gameObject.Children[0].GetComponent<HierarchyExpander>();
            _opener.gameObject.IsActive = childItems.Count > 0;
        }

        public override void OnRMBUp()
        {
            //TODO: menu (rename, delete)
        }
    }
}