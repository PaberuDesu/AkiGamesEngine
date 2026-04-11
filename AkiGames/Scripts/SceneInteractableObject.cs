using AkiGames.Core;
using AkiGames.UI;

namespace AkiGames.Scripts
{
    public class SceneInteractableObject : GameComponent
    {
        public UITransform source = null!;

        private event Action? ActionOnDoubleClick;
        public void SetActionOnDoubleClick(Action func) => ActionOnDoubleClick = func;

        public override void OnDoubleClick() => ActionOnDoubleClick?.Invoke();
    }
}