using AkiGames.Core;
using AkiGames.UI;

namespace AkiGames
{
    public abstract class GameComponent : GameStructure
    {
        public bool Enabled = true;
        [DontSerialize, HideInInspector] public GameObject gameObject;
        [DontSerialize, HideInInspector] public UITransform uiTransform;
        public override void Update() { if (Enabled) base.Update(); }

        public virtual GameComponent Copy()
        {
            var copy = (GameComponent)MemberwiseClone();
            copy.gameObject = null;
            copy.uiTransform = null;
            return copy;
        }

        public override void OnScrollFromOutsideTheObject(int scrollValue) => OnScroll(scrollValue);

        public override void Dispose()
        {
            // Удаляем компонент из родительского объекта
            gameObject?.RemoveComponent(this);
    
            base.Dispose();
        }
    }
}