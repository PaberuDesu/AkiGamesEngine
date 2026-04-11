using AkiGames.Core;
using Rectangle = AkiGames.Core.Rectangle;
using AkiGames.UI;

namespace AkiGames.Scripts.Window
{
    public abstract class WindowTransformer : GameComponent
    {
        protected void MoveInSpace(Vector2 newPosition, int Width = -1, int Height = -1)
        {
            Rectangle windowBounds = WindowBounds;
            Rectangle newBounds = new(
                (int)newPosition.X,
                (int)newPosition.Y,
                Width > 0 ? Width : windowBounds.Width,
                Height > 0 ? Height : windowBounds.Height
            );
            newBounds = Constrain(newBounds);
            Rectangle windowScopeBounds = WindowScopeBounds;
            UITransform windowTransform = WindowTransform;
            windowTransform.OffsetMin = new Vector2(newBounds.X - windowScopeBounds.X, newBounds.Y - windowScopeBounds.Y);
            windowTransform.Width = newBounds.Width;
            windowTransform.Height = newBounds.Height;
            WindowObj.RefreshBounds();
        }

        protected GameObject WindowObj => gameObject.Parent;
        protected GameObject WindowScopeObj => WindowObj.Parent;
        protected UITransform WindowTransform => WindowObj.uiTransform;
        protected UITransform WindowScopeTransform => WindowScopeObj.uiTransform;
        protected Rectangle WindowBounds => WindowTransform.Bounds;
        protected Rectangle WindowScopeBounds => WindowScopeTransform.Bounds;

        protected abstract Rectangle Constrain(Rectangle newBounds);

        public override void OnMouseUp() => SnapToNearbyEdges();
        
        internal void SnapToNearbyEdges()
        {
            Rectangle windowScopeBounds = WindowScopeBounds;
            // Собираем все возможные границы для прилипания
            List<SnapPoint> snapPoints =
            [
                // Добавляем границы пространства для окон
                new SnapPoint(0, SnapType.Horizontal), // Левый край
                new SnapPoint(windowScopeBounds.Width, SnapType.Horizontal), // Правый край
                new SnapPoint(0, SnapType.Vertical), // Верхний край
                new SnapPoint(windowScopeBounds.Height, SnapType.Vertical) // Нижний край
            ];

            // Добавляем границы других окон
            foreach (GameObject otherWindow in WindowScopeObj.Children)
            {
                if (otherWindow == gameObject) continue;

                // Левая граница
                snapPoints.Add(new SnapPoint(otherWindow.uiTransform.LocalBounds.Left, SnapType.Horizontal));
                // Правая граница
                snapPoints.Add(new SnapPoint(otherWindow.uiTransform.LocalBounds.Right, SnapType.Horizontal));
                // Верхняя граница
                snapPoints.Add(new SnapPoint(otherWindow.uiTransform.LocalBounds.Top, SnapType.Vertical));
                // Нижняя граница
                snapPoints.Add(new SnapPoint(otherWindow.uiTransform.LocalBounds.Bottom, SnapType.Vertical));
            }

            // Применяем прилипание для каждой стороны окна
            UITransform windowTransform = WindowTransform;
            TrySnapSide(windowTransform.LocalBounds.Left, snapPoints, SnapType.Horizontal, (x) =>
            {
                int offset = windowTransform.LocalBounds.Left - x;
                windowTransform.OffsetMin = new Vector2(windowTransform.OffsetMin.X - offset, windowTransform.OffsetMin.Y);
                windowTransform.Width += offset;
            });

            TrySnapSide(windowTransform.LocalBounds.Right, snapPoints, SnapType.Horizontal, (x) =>
            {
                windowTransform.Width = x - windowTransform.LocalBounds.Left;
            });

            TrySnapSide(windowTransform.LocalBounds.Top, snapPoints, SnapType.Vertical, (y) =>
            {
                int offset = windowTransform.LocalBounds.Top - y;
                windowTransform.OffsetMin = new Vector2(windowTransform.OffsetMin.X, windowTransform.OffsetMin.Y - offset);
                windowTransform.Height += offset;
            });

            TrySnapSide(windowTransform.LocalBounds.Bottom, snapPoints, SnapType.Vertical, (y) =>
            {
                windowTransform.Height = y - windowTransform.LocalBounds.Top;
            });
            
            WindowObj.RefreshBounds();
        }

        private static void TrySnapSide(int value, List<SnapPoint> snapPoints, SnapType type, Action<int> adjust)
        {
            const int snapThreshold = 30;

            foreach (var point in snapPoints)
            {
                if (point.Type != type) continue;

                if (Math.Abs(value - point.Position) < snapThreshold)
                {
                    adjust(point.Position);
                    return;
                }
            }
        }

        private struct SnapPoint(int position, SnapType type)
        {
            public int Position = position;
            public SnapType Type = type;
        }

        private enum SnapType
        {
            Horizontal,
            Vertical
        }
    }
}