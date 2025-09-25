using AkiGames.Core;
using AkiGames.UI;

namespace AkiGames
{
    public abstract class GameComponent : GameStructure
    {
        public bool Enabled = true;
        [DontSerialize] public GameObject gameObject;
        [DontSerialize] public UITransform uiTransform;
        public override void Update() { if (Enabled) base.Update(); }

        public virtual GameComponent Copy()
        {
            var copy = (GameComponent)MemberwiseClone();
            copy.gameObject = null;
            copy.uiTransform = null;
            return copy;
        }

        public override void OnScrollFromOutsideTheObject(int scrollValue) => OnScroll(scrollValue);
    }
}