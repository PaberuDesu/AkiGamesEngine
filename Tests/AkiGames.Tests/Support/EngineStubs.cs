using System.Reflection;
using AkiGames.Core.GameStructures;
using AkiGames.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;

namespace AkiGames.Core
{
    public static class Game1
    {
        public static event Action<GameTime>? UpdateAction;
        public static Dictionary<string, GameObject> Prefabs { get; } = [];
        public static ContentManager? GameContent { get; set; }
        public static ContentManager? EditorContent { get; set; }
        public static string? GameContentRoot { get; set; }
        public static string? EditorContentRoot { get; set; }

        public static void RaiseUpdate(GameTime gameTime) => UpdateAction?.Invoke(gameTime);

        public static Texture2D? LoadGameTexture(string assetPath) => null;

        public static string GetGameTextureLink(Texture2D? texture) =>
            texture?.Name ?? "";
    }
}

namespace AkiGames.Core.Editor
{
    public static class ProjectScriptLoader
    {
        public static Type? ResolveComponentType(string typeName) => null;

        public static bool IsProjectScriptAssembly(Assembly assembly) => false;
    }
}

namespace AkiGames.Events
{
    public static class Input
    {
        public enum HotKey
        {
            CtrlZ,
            CtrlS
        }
    }
}

namespace AkiGames.Scripts.Window
{
    public abstract class WindowController : GameComponent
    {
    }
}

namespace AkiGames.Scripts.WindowContentTypes
{
    public static class ConsoleWindowController
    {
        public static List<string> Messages { get; } = [];

        public static void Log(object message) => Messages.Add(message?.ToString() ?? "");
    }
}

namespace AkiGames.UI
{
    public abstract class DrawableComponent : GameComponent
    {
        public int zIndex;

        public void AddToLayer()
        {
        }
    }
}
