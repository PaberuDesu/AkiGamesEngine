using AkiGames.Core.Serialization;
using Microsoft.Xna.Framework;

namespace AkiGames.Tests.Support;

public sealed class TrackingComponent : GameComponent
{
    public int AwakeCount;
    public int StartCount;
    public int UpdateCount;
    public int MouseEnterCount;
    public int ScrollCount;

    public override void Awake() => AwakeCount++;
    public override void Start() => StartCount++;
    public override void Update() => UpdateCount++;
    public override void OnMouseEnter() => MouseEnterCount++;
    public override void OnScroll(int scrollValue) => ScrollCount += scrollValue;
}

public sealed class ReferenceComponent : GameComponent
{
    public GameObject? TargetObject;
    public TrackingComponent? TargetComponent;
    [DontSerialize] public int RuntimeOnly = 99;
}

public sealed class SerializableComponent : GameComponent
{
    public int Number;
    public string Text = "";
    public SampleMode Mode;
    public Color Tint;
    public Vector2 Offset;
    public GameObject? ObjectReference;
    public TrackingComponent? ComponentReference;
    [DontSerialize] public string RuntimeOnly = "runtime";

    public string PropertyValue { get; set; } = "";
}

public enum SampleMode
{
    First,
    Second
}

public sealed class TrackingGameObject(string name) : GameObject(name)
{
    public int AwakeCount;

    public override void Awake()
    {
        base.Awake();
        AwakeCount++;
    }
}
