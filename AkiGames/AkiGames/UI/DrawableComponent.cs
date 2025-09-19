using Microsoft.Xna.Framework.Graphics;

namespace AkiGames.UI
{
    public abstract class DrawableComponent : GameComponent
    {
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
    }
}