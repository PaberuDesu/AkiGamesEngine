using System;

namespace AkiGames.Scripts
{
    public class SceneInteractableObject : GameComponent
    {
        private event Action ActionOnDoubleClick;
        public void SetActionOnDoubleClick(Action func) => ActionOnDoubleClick = func;

        public override void OnDoubleClick() => ActionOnDoubleClick?.Invoke();
    }
}