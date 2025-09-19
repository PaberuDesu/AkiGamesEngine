using Microsoft.Xna.Framework;
using AkiGames.Events;

namespace AkiGames.Scripts.Window
{
    public class ResizeHandleController : WindowTransformer
    {
        public ResizeDirection resizeDirection;
        public enum ResizeDirection
        {
            Left,
            Right,
            Bottom,
            BottomLeft,
            BottomRight
        }
        
        protected override Rectangle Constrain(Rectangle windowBounds)
        {
            Rectangle windowScopeBounds = WindowScopeBounds;
            if (windowBounds.Left < windowScopeBounds.Left)
            {
                windowBounds.Width = windowBounds.Right - windowScopeBounds.Left;
                windowBounds.X = windowScopeBounds.Left;
            }
            if (windowBounds.Right > windowScopeBounds.Right)
            {
                windowBounds.Width = windowScopeBounds.Right - windowBounds.Left;
                windowBounds.X = windowScopeBounds.Right - windowBounds.Width;
            }
            if (windowBounds.Bottom > windowScopeBounds.Bottom)
            {
                windowBounds.Height = windowScopeBounds.Bottom - windowBounds.Top;
            }

            return windowBounds;
        }
        
        public override void Drag(Vector2 cursorPosOnObj)
        {
            Rectangle handlePos = uiTransform.Bounds;
            Vector2 moveOffset = Input.mousePosition.ToVector2() - cursorPosOnObj - new Vector2(handlePos.X, handlePos.Y);
            Rectangle resizeStartBounds = gameObject.Parent.uiTransform.Bounds;
            int newX = resizeStartBounds.X;
            int newY = resizeStartBounds.Y;
            int newWidth = resizeStartBounds.Width;
            int newHeight = resizeStartBounds.Height;
            switch (resizeDirection)
            {
                case ResizeDirection.Left:
                    newX += (int)moveOffset.X;
                    newWidth -= (int)moveOffset.X;
                    break;
                case ResizeDirection.Right:
                    newWidth += (int)moveOffset.X;
                    break;
                case ResizeDirection.Bottom:
                    newHeight += (int)moveOffset.Y;
                    break;
                case ResizeDirection.BottomLeft:
                    newX += (int)moveOffset.X;
                    newWidth -= (int)moveOffset.X;
                    newHeight += (int)moveOffset.Y;
                    break;
                case ResizeDirection.BottomRight:
                    newWidth += (int)moveOffset.X;
                    newHeight += (int)moveOffset.Y;
                    break;
            }

            if (newWidth < 200)
            {
                if (resizeDirection == ResizeDirection.Left ||
                    resizeDirection == ResizeDirection.BottomLeft)
                {
                    newX = newX + newWidth - 200;
                }
                newWidth = 200;
            }
            if (newHeight < 150) newHeight = 150;

            MoveInSpace(
                new Vector2(newX, newY),
                newWidth,
                newHeight
            );
        }
    }
}