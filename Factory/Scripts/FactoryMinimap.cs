using System;
using Microsoft.Xna.Framework;

namespace AkiGames.Scripts
{
    internal sealed class FactoryMinimap
    {
        private readonly bool[,] _surfaceDiscovered;
        private readonly bool[,] _caveDiscovered;
        private int _surfaceCount;
        private int _caveCount;

        public FactoryMinimap(int width, int height)
        {
            _surfaceDiscovered = new bool[width, height];
            _caveDiscovered = new bool[width, height];
        }

        public void Discover(Vector2 position, FactoryLevel level, int radius, int width, int height)
        {
            Point center = new((int)Math.Floor(position.X), (int)Math.Floor(position.Y));
            bool[,] map = GetMap(level);

            for (int x = center.X - radius; x <= center.X + radius; x++)
            {
                for (int y = center.Y - radius; y <= center.Y + radius; y++)
                {
                    if (x < 0 || y < 0 || x >= width || y >= height)
                        continue;

                    int dx = x - center.X;
                    int dy = y - center.Y;
                    if (dx * dx + dy * dy > radius * radius)
                        continue;

                    if (map[x, y]) continue;

                    map[x, y] = true;
                    if (level == FactoryLevel.Surface)
                        _surfaceCount++;
                    else
                        _caveCount++;
                }
            }
        }

        public bool HasDiscovery(FactoryLevel level) =>
            level == FactoryLevel.Surface ? _surfaceCount > 0 : _caveCount > 0;

        public bool IsDiscovered(FactoryLevel level, int x, int y)
        {
            bool[,] map = GetMap(level);
            return x >= 0 && y >= 0 && x < map.GetLength(0) && y < map.GetLength(1) && map[x, y];
        }

        public byte[] Export(FactoryLevel level)
        {
            bool[,] map = GetMap(level);
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            byte[] bytes = new byte[(width * height + 7) / 8];
            int index = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++, index++)
                {
                    if (!map[x, y]) continue;
                    bytes[index / 8] |= (byte)(1 << (index % 8));
                }
            }

            return bytes;
        }

        public void Import(FactoryLevel level, byte[] bytes)
        {
            bool[,] map = GetMap(level);
            int width = map.GetLength(0);
            int height = map.GetLength(1);
            int count = 0;
            int index = 0;

            for (int y = 0; y < height; y++)
            {
                for (int x = 0; x < width; x++, index++)
                {
                    bool value = bytes != null &&
                        index / 8 < bytes.Length &&
                        (bytes[index / 8] & (1 << (index % 8))) != 0;
                    map[x, y] = value;
                    if (value) count++;
                }
            }

            if (level == FactoryLevel.Surface)
                _surfaceCount = count;
            else
                _caveCount = count;
        }

        private bool[,] GetMap(FactoryLevel level) =>
            level == FactoryLevel.Surface ? _surfaceDiscovered : _caveDiscovered;
    }
}
