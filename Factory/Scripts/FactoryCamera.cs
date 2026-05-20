using System;
using Microsoft.Xna.Framework;

namespace AkiGames.Scripts
{
    internal sealed class FactoryCamera
    {
        private const float MinZoom = 0.55f;
        private const float MaxZoom = 2.4f;
        private const float ZoomStep = 1.12f;

        private readonly int _baseTileSize;
        private float _zoom = 1f;

        public int TileSize => Math.Max(12, (int)Math.Round(_baseTileSize * _zoom));

        public FactoryCamera(int tileSize)
        {
            _baseTileSize = Math.Max(20, tileSize);
        }

        public void AdjustZoom(int scrollDelta)
        {
            if (scrollDelta == 0) return;

            int steps = Math.Max(1, Math.Abs(scrollDelta) / 120);
            float factor = (float)Math.Pow(ZoomStep, steps);
            if (scrollDelta < 0)
                factor = 1f / factor;

            _zoom = MathHelper.Clamp(_zoom * factor, MinZoom, MaxZoom);
        }

        public Vector2 GetCameraOffset(Rectangle worldBounds, Vector2 playerPosition) =>
            new(
                worldBounds.X + worldBounds.Width / 2f - playerPosition.X * TileSize,
                worldBounds.Y + worldBounds.Height / 2f - playerPosition.Y * TileSize
            );

        public Rectangle GetTileRect(int x, int y, Vector2 cameraOffset) =>
            new(
                (int)(x * TileSize + cameraOffset.X),
                (int)(y * TileSize + cameraOffset.Y),
                TileSize,
                TileSize
            );

        public Point ScreenToTile(Point screenPosition, Rectangle worldBounds, Vector2 playerPosition)
        {
            Vector2 cameraOffset = GetCameraOffset(worldBounds, playerPosition);
            Vector2 worldPosition = (screenPosition.ToVector2() - cameraOffset) / TileSize;
            return new Point((int)Math.Floor(worldPosition.X), (int)Math.Floor(worldPosition.Y));
        }
    }
}
