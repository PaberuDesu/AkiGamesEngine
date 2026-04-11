using System;
using Vector2 = AkiGames.Core.Vector2;

namespace AkiGames.UI
{
    public interface IAlignable
    {
        public (int, int) PositionAfterTurningParent(
            UITransform turningTransform, int xBefore, int yBefore)
        {
            Vector2 parentOriginPosition = turningTransform.OriginPosition;
            float angle = (float)(turningTransform.Rotation * Math.PI / 180.0);
            if (angle != 0)
            {
                float x_relative = xBefore - parentOriginPosition.X;
                float y_relative = yBefore - parentOriginPosition.Y;
                float x_relative_new = (float)(
                    x_relative * Math.Cos(angle) - y_relative * Math.Sin(angle));
                float y_relative_new = (float)(
                    x_relative * Math.Sin(angle) + y_relative * Math.Cos(angle));
                return (
                    (int)(x_relative_new + parentOriginPosition.X),
                    (int)(y_relative_new + parentOriginPosition.Y)
                );
            }
            return (xBefore, yBefore);
        }
    }
}