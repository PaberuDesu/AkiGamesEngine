using Microsoft.Xna.Framework;
using AkiGames.Events;

namespace AkiGames.Scripts.Window
{
    internal class TitleBarController : WindowTransformer
    {
        protected override Rectangle Constrain(Rectangle windowBounds)
        {
            Rectangle windowScopeBounds = WindowScopeBounds;
            if (windowBounds.Left < windowScopeBounds.Left) windowBounds.X = windowScopeBounds.Left;
            if (windowBounds.Top < windowScopeBounds.Top) windowBounds.Y = windowScopeBounds.Top;
            if (windowBounds.Right > windowScopeBounds.Right) windowBounds.X = windowScopeBounds.Right - windowBounds.Width;
            if (windowBounds.Bottom > windowScopeBounds.Bottom) windowBounds.Y = windowScopeBounds.Bottom - windowBounds.Height;

            return windowBounds;
        }

        public override void Drag(Vector2 cursorPosOnObj) =>
            MoveInSpace(Input.mousePosition.ToVector2() - cursorPosOnObj);
    }
}