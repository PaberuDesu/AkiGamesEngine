using AkiGames.Core;
using AkiGames.UI;

namespace AkiGames.Scripts.InspectorRedactor
{
    public class InspectorGameObjectParameters : GameComponent
    {
        [DontSerialize, HideInInspector] public GameObject Target { get; set; }

        public string ObjectName
        {
            get => Target?.ObjectName ?? "";
            set => Target?.ObjectName = value ?? "";
        }

        public int ObjectID => Target?.ObjectID ?? 0;

        public bool IsActive
        {
            get => Target?.IsActive ?? false;
            set => Target?.IsActive = value;
        }

        public bool IsMouseTargetable
        {
            get => Target?.IsMouseTargetable ?? false;
            set => Target?.IsMouseTargetable = value;
        }

        public static InspectorGameObjectParameters For(GameObject target) =>
            new()
            {
                // we only mark parent as target but not this as parent's child,
                // so that we can use this component for changing parameters in inspector
                // without adding this component to the resulting hierarchy
                Target = target,
                gameObject = target,
                uiTransform = target?.uiTransform
            };
    }
}
