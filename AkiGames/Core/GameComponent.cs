using AkiGames.UI;

namespace AkiGames.Core
{
    public abstract class GameComponent : GameStructure
    {
        public bool Enabled = true;
        [DontSerialize, HideInInspector] public GameObject gameObject = null!;
        [DontSerialize, HideInInspector] public UITransform uiTransform = null!;
        public override void Update() { if (Enabled) base.Update(); }

        public virtual GameComponent Copy()
        {
            var copy = (GameComponent)MemberwiseClone();
            copy.gameObject = null!;
            copy.uiTransform = null!;
            return copy;
        }

        public override void OnScrollFromOutsideTheObject(int scrollValue) => OnScroll(scrollValue);
    }
}