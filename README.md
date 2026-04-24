# AkiGamesEngine

AkiGamesEngine is a custom game engine and editor built with MonoGame `3.8.4` on `.NET 10` for Windows. The repository combines the editor/runtime, a custom content pipeline extension for `.aki` scene files, and a sample project that follows the same architectural model as the engine itself.

The core idea behind the project is a self-similar workflow: the editor is built as a project on top of the same component-driven architecture it is meant to edit. Game objects, components, UI elements, serialized scenes, and editor tools all follow the same general structure, which makes the engine project and user projects behave consistently.

## Repository layout

- `AkiGames/AkiGames` - the main engine/editor application
- `AkiGames/AkiContentPipeline` - custom MonoGame content pipeline support for `.aki` assets
- `TicTacToe` - sample game project built on the same architecture

## Main features

- `GameObject` scene graph and `GameComponent`-based behavior model
- Custom UI framework with layout/anchor support through `UITransform`
- Event-driven input and dispatch system
- JSON-based scene and prefab serialization
- Editor-style windows for hierarchy, inspector, scene, explorer, console, and related tools

## Requirements

- Windows
- `.NET 10.0` SDK
- Internet access on first restore if NuGet packages or local dotnet tools are missing

## Build and run

From `AkiGames/AkiGames`:

```bash
dotnet build
dotnet run
```

## Notes

- The main project targets `net10.0-windows`.
- Windows Forms is enabled for DPI handling.
- MonoGame content build tooling is restored as part of the project setup.
