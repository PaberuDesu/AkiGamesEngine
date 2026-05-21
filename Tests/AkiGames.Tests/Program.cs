using AkiGames.Tests.Core;
using AkiGames.Tests.Explorer;
using AkiGames.Tests.Serialization;
using AkiGames.Tests.Smoke;
using AkiGames.Tests.Support;

TestCase[] tests =
[
    new("GameObject wires children and components", GameObjectTests.AddChildAndComponentWireReferences),
    new("GameObject reports global active state through ancestors", GameObjectTests.GlobalActiveReflectsAncestorState),
    new("Awake tree initializes existing and newly-added nodes", GameObjectTests.AwakeTreeInitializesTreeAndLateAdditions),
    new("Copy deep-clones object trees and remaps references", GameObjectTests.CopyRemapsGameObjectAndComponentReferences),
    new("Serialization round-trips components and references", JsonProjectSerializerTests.RoundTripPreservesSerializableMembersAndReferences),
    new("Prefab links load sparse overrides from content files", JsonProjectSerializerTests.PrefabLinkAppliesSparseOverrides),
    new("Prefab links can clear inherited children", JsonProjectSerializerTests.PrefabLinkCanClearInheritedChildren),
    new("MGCB registry registers and removes content files", ContentMgcbRegistryTests.RegisterAndRemoveFiles),
    new("MGCB registry renames folder references", ContentMgcbRegistryTests.RenameFolderUpdatesReferences),
    new("Content file utility recognizes supported images", ContentMgcbRegistryTests.ContentFileUtilityRecognizesImages),
    new("Smoke: project content roots have startup scenes", ContentSmokeTests.ProjectContentRootsHaveStartupScenes),
    new("Smoke: all .aki files deserialize", ContentSmokeTests.AllAkiFilesDeserialize),
    new("Smoke: prefab links resolve to files", ContentSmokeTests.PrefabLinksResolveToExistingFiles),
    new("Smoke: .aki files are registered in MGCB", ContentSmokeTests.AkiFilesAreRegisteredInMgcb)
];

int failed = TestRunner.Run(tests);
Environment.ExitCode = failed == 0 ? 0 : 1;
