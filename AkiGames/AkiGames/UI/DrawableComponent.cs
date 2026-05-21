using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;

namespace AkiGames.UI
{
    public abstract class DrawableComponent : GameComponent
    {
        public const int PopupZIndex = 1000;
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
                    if (zIndex >= PopupZIndex && potentialMask.zIndex < PopupZIndex)
                    {
                        currentParent = currentParent.Parent;
                        continue;
                    }

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

        private readonly record struct DrawEntry(DrawableComponent Component, int ZIndex, int TopLevelOrder, int Sequence);

        private static List<DrawEntry> layerDrawComponents = [];
        private static int _nextSequence;

        public void AddToLayer()
        {
            layerDrawComponents.Add(new DrawEntry(
                this,
                zIndex,
                gameObject?.GetTopLevelDrawOrder() ?? int.MinValue,
                _nextSequence++
            ));
        }

        public static void MoveSubtreeToPopupLayer(GameObject root)
        {
            if (root == null) return;

            foreach (DrawableComponent drawable in root.Components.OfType<DrawableComponent>())
            {
                if (drawable.zIndex < PopupZIndex)
                    drawable.zIndex = PopupZIndex;
            }

            foreach (GameObject child in root.Children)
                MoveSubtreeToPopupLayer(child);
        }

        public static void DrawLayers(SpriteBatch spriteBatch)
        {
            // zIndex is global; window/top-level order only breaks ties inside the same zIndex.
            IEnumerable<DrawableComponent> componentsToDraw = layerDrawComponents
                .OrderBy(entry => entry.ZIndex)
                .ThenBy(entry => entry.TopLevelOrder)
                .ThenBy(entry => entry.Sequence)
                .Select(entry => entry.Component);

            foreach (var component in componentsToDraw)
            {
                component.Draw(spriteBatch);
            }
            layerDrawComponents = [];
            _nextSequence = 0;
        }
        public abstract void Draw(SpriteBatch spriteBatch);
    }
}
