using AkiGames.Core;
using AkiGames.Tests.Support;
using Microsoft.Xna.Framework;

namespace AkiGames.Tests.Core;

internal static class GameObjectTests
{
    public static void AddChildAndComponentWireReferences()
    {
        GameObject root = new("Root");
        GameObject child = new("Child");
        TrackingComponent component = new();

        root.AddChild(child);
        child.AddComponent(component);

        Assert.Same(root, child.Parent);
        Assert.Same(child, component.gameObject);
        Assert.Same(child.uiTransform, component.uiTransform);

        List<GameObject> childrenSnapshot = root.Children;
        childrenSnapshot.Clear();
        Assert.Equal(1, root.Children.Count, "Children getter should not expose the internal list.");

        List<GameComponent> componentsSnapshot = child.Components;
        componentsSnapshot.Clear();
        Assert.Equal(1, child.Components.Count, "Components getter should not expose the internal list.");
    }

    public static void GlobalActiveReflectsAncestorState()
    {
        GameObject root = new("Root");
        GameObject child = new("Child");
        GameObject grandchild = new("Grandchild");

        root.AddChild(child);
        child.AddChild(grandchild);

        Assert.True(grandchild.IsGlobalActive);

        child.IsActive = false;
        Assert.False(grandchild.IsGlobalActive);

        child.IsActive = true;
        root.IsActive = false;
        Assert.False(grandchild.IsGlobalActive);
    }

    public static void AwakeTreeInitializesTreeAndLateAdditions()
    {
        TrackingGameObject root = new("Root");
        TrackingGameObject child = new("Child");
        TrackingComponent component = new();
        root.AddChild(child);
        child.AddComponent(component);

        root.AkiGamesAwakeTree();

        Assert.Equal(1, root.AwakeCount);
        Assert.Equal(1, child.AwakeCount);
        Assert.Equal(1, component.AwakeCount);

        Game1.RaiseUpdate(new GameTime(TimeSpan.FromMilliseconds(1), TimeSpan.FromMilliseconds(1)));
        Assert.Equal(1, component.StartCount);
        Assert.Equal(1, component.UpdateCount);

        TrackingGameObject lateChild = new("LateChild");
        TrackingComponent lateComponent = new();
        root.AddChild(lateChild);
        root.AddComponent(lateComponent);

        Assert.Equal(1, lateChild.AwakeCount, "Children added after Awake should awaken immediately.");
        Assert.Equal(1, lateComponent.AwakeCount, "Components added after Awake should awaken immediately.");

        component.Dispose();
        lateComponent.Dispose();
        root.Dispose();
    }

    public static void CopyRemapsGameObjectAndComponentReferences()
    {
        GameObject root = new("Root");
        GameObject child = new("Child");
        TrackingComponent childComponent = new();
        ReferenceComponent referenceComponent = new()
        {
            TargetObject = child,
            TargetComponent = childComponent,
            RuntimeOnly = 123
        };

        child.AddComponent(childComponent);
        root.AddChild(child);
        root.AddComponent(referenceComponent);

        GameObject copy = root.Copy();
        ReferenceComponent copiedReference = copy.GetComponent<ReferenceComponent>();
        GameObject copiedChild = copy.Children.Single();
        TrackingComponent copiedChildComponent = copiedChild.GetComponent<TrackingComponent>();

        Assert.NotSame(root, copy);
        Assert.NotSame(child, copiedChild);
        Assert.NotSame(childComponent, copiedChildComponent);
        Assert.Same(copiedChild, copiedReference.TargetObject);
        Assert.Same(copiedChildComponent, copiedReference.TargetComponent);
        Assert.Equal(99, copiedReference.RuntimeOnly, "[DontSerialize] fields should keep constructor defaults when copied.");
        Assert.Equal(0, copy.ObjectID, "Copies should get a fresh ObjectID unless explicitly preserved.");
    }
}
