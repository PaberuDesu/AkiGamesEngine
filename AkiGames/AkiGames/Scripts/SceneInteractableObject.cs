using System;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI;

namespace AkiGames.Scripts
{
    public class SceneInteractableObject : GameComponent
    {
        public UITransform source;
        public GameObject sourceObject;

        private event Action ActionOnDoubleClick;
        public void SetActionOnDoubleClick(Action func) => ActionOnDoubleClick = func;

        public override void OnMouseDown()
        {
            InspectorWindowController.Select(sourceObject);
        }

        public override void OnDoubleClick() => ActionOnDoubleClick?.Invoke();
    }
}
