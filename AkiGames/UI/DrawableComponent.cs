using AkiGames.Core;

namespace AkiGames.UI
{
    public abstract class DrawableComponent : GameComponent
    {
        public int zIndex = 0;

        // Временная заглушка для поиска маски (требует реализации класса Image)
        protected Image? FindParentMask()
        {
            GameObject currentParent = gameObject.Parent;
            while (currentParent != null)
            {
                Image? potentialMask = currentParent.GetComponent<Image>();
                if (potentialMask != null && potentialMask.IsMask)
                {
                    return potentialMask;
                }
                currentParent = currentParent.Parent;
            }
            return null;
        }

        // Stencil-тесты для Veldrid требуют отдельной реализации – пока отключено
        protected static void SetupStencilTest(SpriteBatch spriteBatch, int maskId)
        {
            // TODO: реализовать через Pipeline и ResourceSet
            // В текущей версии SpriteBatch не поддерживает stencil
        }

        protected static void RestoreSpriteBatch(SpriteBatch spriteBatch)
        {
            // TODO: восстановление стандартного состояния
        }

        private static Dictionary<int, List<DrawableComponent>> layerDrawComponents = [];

        public void AddToLayer()
        {
            if (!layerDrawComponents.TryGetValue(zIndex, out var list))
            {
                list = [];
                layerDrawComponents[zIndex] = list;
            }
            list.Add(this);
        }

        public static void DrawLayers(SpriteBatch spriteBatch)
        {
            IEnumerable<DrawableComponent> componentsToDraw = layerDrawComponents
                .OrderBy(kvp => kvp.Key)
                .SelectMany(kvp => kvp.Value);

            foreach (var component in componentsToDraw)
            {
                component.Draw(spriteBatch);
            }
            layerDrawComponents.Clear();
        }

        public abstract void Draw(SpriteBatch spriteBatch);
    }
}