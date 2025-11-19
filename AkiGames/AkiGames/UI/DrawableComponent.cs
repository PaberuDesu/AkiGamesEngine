using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework.Graphics;

namespace AkiGames.UI
{
    public abstract class DrawableComponent : GameComponent
    {
        public int zIndex = 0;
        protected Image FindParentMask()
        {
            GameObject currentParent = gameObject.Parent;
            while (currentParent != null)
            {
                Image potentialMask = currentParent.GetComponent<Image>();
                if (potentialMask != null && potentialMask.IsMask)
                {
                    return potentialMask;
                }
                currentParent = currentParent.Parent;
            }
            return null;
        }

        protected static void SetupStencilTest(SpriteBatch spriteBatch, int maskId)
        {
            spriteBatch.End();

            DepthStencilState stencilState = new()
            {
                StencilEnable = true,
                StencilFunction = CompareFunction.Equal,
                StencilPass = StencilOperation.Keep,
                ReferenceStencil = maskId,
                DepthBufferEnable = false
            };

            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                stencilState,
                RasterizerState.CullNone
            );
        }
        
        protected static void RestoreSpriteBatch(SpriteBatch spriteBatch)
        {
            spriteBatch.End();
            spriteBatch.Begin(
                SpriteSortMode.Deferred,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                RasterizerState.CullNone
            );
        }

        private static Dictionary<int,List<DrawableComponent>> layerDrawComponents = [];
        public void AddToLayer()
        {
            if (!layerDrawComponents.TryGetValue(zIndex, out List<DrawableComponent> list))
            {
                list = [];
                layerDrawComponents[zIndex] = list;
            }

            list.Add(this);
        }

        public static void DrawLayers(SpriteBatch spriteBatch)
        {
            IEnumerable<DrawableComponent> componentsToDraw = layerDrawComponents
                .OrderBy(kvp => kvp.Key).SelectMany(kvp => kvp.Value);

            foreach (var component in componentsToDraw)
            {
                component.Draw(spriteBatch);
            }
            layerDrawComponents = [];
        }
        public abstract void Draw(SpriteBatch spriteBatch);
    }
}