using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AkiGames.UI
{
    public abstract class DrawableComponent : GameComponent
    {
        public int zIndex = 0;
        private static readonly RasterizerState ScissorRasterizerState = new()
        {
            CullMode = CullMode.None,
            ScissorTestEnable = true
        };

        protected Rectangle? SetupMaskClip(SpriteBatch spriteBatch)
        {
            Rectangle? maskBounds = FindParentMaskBounds();
            if (maskBounds == null) return null;

            Rectangle previousScissor = spriteBatch.GraphicsDevice.ScissorRectangle;
            Rectangle clipBounds = Rectangle.Intersect(
                maskBounds.Value,
                spriteBatch.GraphicsDevice.Viewport.Bounds
            );

            spriteBatch.End();
            spriteBatch.GraphicsDevice.ScissorRectangle = clipBounds;
            spriteBatch.Begin(
                SpriteSortMode.Immediate,
                BlendState.AlphaBlend,
                SamplerState.LinearClamp,
                DepthStencilState.None,
                ScissorRasterizerState
            );

            return previousScissor;
        }

        private Rectangle? FindParentMaskBounds()
        {
            Rectangle? maskBounds = null;
            GameObject currentParent = gameObject.Parent;
            while (currentParent != null)
            {
                Image potentialMask = currentParent.GetComponent<Image>();
                if (potentialMask != null && potentialMask.Enabled && potentialMask.IsMask)
                {
                    maskBounds = maskBounds.HasValue ?
                        Rectangle.Intersect(maskBounds.Value, potentialMask.uiTransform.Bounds) :
                        potentialMask.uiTransform.Bounds;
                }

                currentParent = currentParent.Parent;
            }

            return maskBounds;
        }

        protected static void RestoreSpriteBatch(SpriteBatch spriteBatch, Rectangle previousScissor)
        {
            spriteBatch.End();
            spriteBatch.GraphicsDevice.ScissorRectangle = previousScissor;
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
