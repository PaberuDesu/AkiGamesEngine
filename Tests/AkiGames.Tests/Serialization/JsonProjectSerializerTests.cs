using System.Text.Json;
using AkiGames.Core;
using AkiGames.Core.Serialization;
using AkiGames.Tests.Support;
using Microsoft.Xna.Framework;

namespace AkiGames.Tests.Serialization;

internal static class JsonProjectSerializerTests
{
    public static void RoundTripPreservesSerializableMembersAndReferences()
    {
        JsonProjectSerializer.ClearTypeCache();

        GameObject root = new("Root") { ObjectID = 1 };
        GameObject target = new("Target") { ObjectID = 2 };
        TrackingComponent targetComponent = new();
        SerializableComponent component = new()
        {
            Number = 42,
            Text = "hello",
            Mode = SampleMode.Second,
            Tint = new Color(1, 2, 3, 4),
            Offset = new Vector2(12.5f, -7.25f),
            ObjectReference = target,
            ComponentReference = targetComponent,
            RuntimeOnly = "do not serialize",
            PropertyValue = "property"
        };

        target.AddComponent(targetComponent);
        root.AddChild(target);
        root.AddComponent(component);

        string json = JsonProjectSerializer.SerializeToJson(root);

        Assert.Contains("\"type\": \"SerializableComponent\"", json);
        Assert.Contains("\"ObjectReference\": 2", json);
        Assert.Contains("\"ComponentReference\": 2", json);
        Assert.DoesNotContain("RuntimeOnly", json);

        JsonElement element = JsonSerializer.Deserialize<JsonElement>(json);
        GameObject loaded = JsonProjectSerializer.LoadFromJson(element);
        SerializableComponent loadedComponent = loaded.GetComponent<SerializableComponent>();
        GameObject loadedTarget = loaded.Children.Single();

        Assert.Equal("Root", loaded.ObjectName);
        Assert.Equal(42, loadedComponent.Number);
        Assert.Equal("hello", loadedComponent.Text);
        Assert.Equal(SampleMode.Second, loadedComponent.Mode);
        Assert.Equal(new Color(1, 2, 3, 4), loadedComponent.Tint);
        Assert.Equal(new Vector2(12.5f, -7.25f), loadedComponent.Offset);
        Assert.Equal("property", loadedComponent.PropertyValue);
        Assert.Same(loadedTarget, loadedComponent.ObjectReference);
        Assert.Same(loadedTarget.GetComponent<TrackingComponent>(), loadedComponent.ComponentReference);
        Assert.Equal("runtime", loadedComponent.RuntimeOnly);
    }

    public static void PrefabLinkAppliesSparseOverrides()
    {
        JsonProjectSerializer.ClearTypeCache();

        using TemporaryDirectory temp = new("prefab-link");
        string contentRoot = System.IO.Path.Combine(temp.Path, "Content");
        string prefabsRoot = System.IO.Path.Combine(contentRoot, "Prefabs");
        Directory.CreateDirectory(prefabsRoot);

        File.WriteAllText(
            System.IO.Path.Combine(prefabsRoot, "Button.aki"),
            """
            {
              "ObjectName": "Button",
              "ObjectID": 10,
              "IsActive": true,
              "IsMouseTargetable": true,
              "Components": [
                { "type": "SerializableComponent", "Number": 5, "Text": "base" }
              ],
              "Children": [
                {
                  "ObjectName": "Label",
                  "ObjectID": 11,
                  "IsActive": true,
                  "IsMouseTargetable": true,
                  "Components": [
                    { "type": "SerializableComponent", "Text": "label" }
                  ],
                  "Children": []
                }
              ]
            }
            """
        );

        Game1.GameContentRoot = contentRoot;
        Game1.EditorContentRoot = null;
        Game1.Prefabs.Clear();

        JsonElement element = JsonSerializer.Deserialize<JsonElement>(
            """
            {
              "Link": "Content/Prefabs/Button",
              "ObjectName": "LinkedButton",
              "IsActive": false,
              "Components": [
                { "type": "SerializableComponent", "Number": 9 }
              ],
              "Children": [
                {
                  "ObjectName": "Label",
                  "IsMouseTargetable": false,
                  "Components": [
                    { "type": "SerializableComponent", "Text": "override label" }
                  ]
                },
                {
                  "ObjectName": "Extra",
                  "IsActive": true,
                  "IsMouseTargetable": true,
                  "Components": [],
                  "Children": []
                }
              ]
            }
            """
        );

        GameObject linked = JsonProjectSerializer.LoadFromJson(element);
        SerializableComponent component = linked.GetComponent<SerializableComponent>();
        GameObject label = linked.Children.Single(child => child.ObjectName == "Label");
        GameObject extra = linked.Children.Single(child => child.ObjectName == "Extra");

        Assert.Equal("LinkedButton", linked.ObjectName);
        Assert.False(linked.IsActive);
        Assert.Equal(9, component.Number);
        Assert.Equal("base", component.Text, "Sparse component overrides should keep omitted prefab values.");
        Assert.False(label.IsMouseTargetable);
        Assert.Equal("override label", label.GetComponent<SerializableComponent>().Text);
        Assert.Equal("Extra", extra.ObjectName);
    }

    public static void PrefabLinkCanClearInheritedChildren()
    {
        JsonProjectSerializer.ClearTypeCache();

        using TemporaryDirectory temp = new("prefab-link-clear");
        string contentRoot = System.IO.Path.Combine(temp.Path, "Content");
        string prefabsRoot = System.IO.Path.Combine(contentRoot, "Prefabs");
        Directory.CreateDirectory(prefabsRoot);

        File.WriteAllText(
            System.IO.Path.Combine(prefabsRoot, "Panel.aki"),
            """
            {
              "ObjectName": "Panel",
              "Components": [],
              "Children": [
                { "ObjectName": "InheritedChild", "Components": [], "Children": [] }
              ]
            }
            """
        );

        Game1.GameContentRoot = contentRoot;
        Game1.EditorContentRoot = null;
        Game1.Prefabs.Clear();

        JsonElement element = JsonSerializer.Deserialize<JsonElement>(
            """
            {
              "Link": "Panel",
              "Children": []
            }
            """
        );

        GameObject linked = JsonProjectSerializer.LoadFromJson(element);

        Assert.Equal("Panel", linked.ObjectName);
        Assert.Equal(0, linked.Children.Count);
    }
}
