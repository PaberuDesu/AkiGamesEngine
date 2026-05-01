using AkiGames.UI;

namespace AkiGames.Scripts.Scene
{
    internal class SceneText : Text
    {
        public float RenderScale = 1f;
        protected override float TextScale => RenderScale;

        public static SceneText From(Text source, float renderScale)
        {
            if (source == null) return null;

            SceneText sceneText = new()
            {
                RenderScale = renderScale
            };
            sceneText.CopyTextStateFrom(source);
            return sceneText;
        }
    }
}
