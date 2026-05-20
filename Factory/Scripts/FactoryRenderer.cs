using System;
using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AkiGames.Core;
using AkiGames.Events;

namespace AkiGames.Scripts
{
    internal sealed class FactoryRenderer
    {
        private Texture2D _pixel;
        private readonly Dictionary<string, Texture2D> _contentTextures = [];

        public void Draw(
            SpriteBatch spriteBatch,
            FactoryWorld world,
            FactoryCamera camera,
            FactoryInteraction interaction,
            Rectangle worldBounds,
            float interactionRadius
        )
        {
            EnsureTextures(spriteBatch.GraphicsDevice);

            Vector2 cameraOffset = camera.GetCameraOffset(worldBounds, world.Player.Position);
            DrawWorld(spriteBatch, world, camera, worldBounds, cameraOffset);
            DrawInteractionOverlay(spriteBatch, world, camera, interaction, worldBounds, cameraOffset, interactionRadius);
            DrawPlayer(spriteBatch, world.Player.Position, camera.TileSize, cameraOffset);
            DrawVitals(spriteBatch, world.Player, worldBounds);
            DrawMinimap(spriteBatch, world, worldBounds);
        }

        public void DrawCursorItem(SpriteBatch spriteBatch, FactoryResource resource, Point mousePosition)
        {
            EnsureTextures(spriteBatch.GraphicsDevice);

            Rectangle frame = new(mousePosition.X + 10, mousePosition.Y + 8, 28, 28);
            spriteBatch.Draw(_pixel, frame, new Color(20, 21, 23, 215));
            DrawBorder(spriteBatch, frame, 2, new Color(173, 162, 122));
            DrawResourceGlyph(spriteBatch, resource, new Rectangle(frame.X + 5, frame.Y + 5, 18, 18));
        }

        public Texture2D Pixel(SpriteBatch spriteBatch)
        {
            EnsureTextures(spriteBatch.GraphicsDevice);
            return _pixel;
        }

        public void DrawBorder(SpriteBatch spriteBatch, Rectangle rect, int thickness, Color color)
        {
            EnsureTextures(spriteBatch.GraphicsDevice);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, rect.Width, thickness), color);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Bottom - thickness, rect.Width, thickness), color);
            spriteBatch.Draw(_pixel, new Rectangle(rect.X, rect.Y, thickness, rect.Height), color);
            spriteBatch.Draw(_pixel, new Rectangle(rect.Right - thickness, rect.Y, thickness, rect.Height), color);
        }

        private void DrawWorld(
            SpriteBatch spriteBatch,
            FactoryWorld world,
            FactoryCamera camera,
            Rectangle worldBounds,
            Vector2 cameraOffset
        )
        {
            spriteBatch.Draw(_pixel, worldBounds, new Color(19, 24, 27));

            int firstX = Math.Max(0, (int)Math.Floor((worldBounds.Left - cameraOffset.X) / camera.TileSize) - 1);
            int firstY = Math.Max(0, (int)Math.Floor((worldBounds.Top - cameraOffset.Y) / camera.TileSize) - 1);
            int lastX = Math.Min(world.Width - 1, (int)Math.Ceiling((worldBounds.Right - cameraOffset.X) / camera.TileSize) + 1);
            int lastY = Math.Min(world.Height - 1, (int)Math.Ceiling((worldBounds.Bottom - cameraOffset.Y) / camera.TileSize) + 1);

            world.EnsureViewLoaded(world.Player.Level, firstX, firstY, lastX, lastY);

            for (int x = firstX; x <= lastX; x++)
            {
                for (int y = firstY; y <= lastY; y++)
                {
                    FactoryTile tile = world.GetTile(new Point(x, y));
                    if (tile == null) continue;
                    Rectangle tileRect = camera.GetTileRect(x, y, cameraOffset);
                    DrawGround(spriteBatch, world, x, y, tileRect, tile, world.Player.Level);
                }
            }

            for (int x = firstX; x <= lastX; x++)
            {
                for (int y = firstY; y <= lastY; y++)
                {
                    FactoryTile tile = world.GetTile(new Point(x, y));
                    if (tile == null) continue;
                    Rectangle tileRect = camera.GetTileRect(x, y, cameraOffset);
                    DrawFloor(spriteBatch, tileRect, tile.Floor);
                }
            }

            for (int x = firstX; x <= lastX; x++)
            {
                for (int y = firstY; y <= lastY; y++)
                {
                    FactoryTile tile = world.GetTile(new Point(x, y));
                    if (tile == null) continue;
                    Rectangle tileRect = camera.GetTileRect(x, y, cameraOffset);
                    DrawObject(spriteBatch, tileRect, tile);
                }
            }

            if (world.Player.Level == FactoryLevel.Surface)
            {
                for (int i = 0; i < world.Boats.Count; i++)
                    DrawBoatEntity(spriteBatch, world.Boats[i], camera.TileSize, cameraOffset);
            }

            for (int i = 0; i < world.ActiveCreatures.Count; i++)
                DrawCreature(spriteBatch, world.ActiveCreatures[i], camera.TileSize, cameraOffset);
        }

        private void DrawGround(SpriteBatch spriteBatch, FactoryWorld world, int x, int y, Rectangle tileRect, FactoryTile tile, FactoryLevel level)
        {
            if (level == FactoryLevel.Surface && tile.Ground == FactoryGround.Grass)
                spriteBatch.Draw(_pixel, tileRect, world.GetSurfaceGrassColor(new Point(x, y)));

            DrawContentTexture(spriteBatch, FactoryTextureCatalog.GetGroundTexture(tile.Ground), tileRect);
            spriteBatch.Draw(_pixel, new Rectangle(tileRect.X, tileRect.Y, tileRect.Width, 1), new Color(0, 0, 0, 28));
            spriteBatch.Draw(_pixel, new Rectangle(tileRect.X, tileRect.Y, 1, tileRect.Height), new Color(0, 0, 0, 28));

            if (level == FactoryLevel.Cave && tile.IsHole)
                spriteBatch.Draw(_pixel, tileRect, Color.FromNonPremultiplied(239, 240, 240, 140));

            if (tile.IsHole && level == FactoryLevel.Surface)
            {
                DrawInnerRect(spriteBatch, tileRect, Math.Max(5, tileRect.Width / 7), new Color(8, 10, 11));
                DrawBorder(spriteBatch, tileRect, 2, new Color(66, 61, 57));
            }
        }

        private void DrawFloor(SpriteBatch spriteBatch, Rectangle tileRect, FactoryFloor floor)
        {
            if (floor != FactoryFloor.Wood) return;

            DrawContentTexture(spriteBatch, FactoryTextureCatalog.GetFloorTexture(floor), tileRect);
        }

        private void DrawObject(SpriteBatch spriteBatch, Rectangle tileRect, FactoryTile tile)
        {
            DrawContentTexture(spriteBatch, FactoryTextureCatalog.GetObjectTexture(tile), tileRect);
        }

        private void DrawCreature(SpriteBatch spriteBatch, FactoryCreature creature, int tileSize, Vector2 cameraOffset)
        {
            int size = creature.CreatureType == FactoryCreatureType.Rabbit
                ? Math.Max(12, tileSize / 2)
                : Math.Max(10, tileSize / 3);
            Rectangle rect = new(
                (int)(creature.Position.X * tileSize + cameraOffset.X - size / 2f),
                (int)(creature.Position.Y * tileSize + cameraOffset.Y - size / 2f),
                size,
                size
            );

            DrawContentTexture(spriteBatch, FactoryTextureCatalog.GetCreatureTexture(creature.CreatureType), rect);
        }

        private void DrawBoatEntity(SpriteBatch spriteBatch, FactoryBoatEntity boat, int tileSize, Vector2 cameraOffset)
        {
            Rectangle rect = new(
                (int)(boat.Position.X * tileSize + cameraOffset.X - tileSize * 0.36f),
                (int)(boat.Position.Y * tileSize + cameraOffset.Y - tileSize * 0.26f),
                Math.Max(14, (int)(tileSize * 0.72f)),
                Math.Max(10, (int)(tileSize * 0.42f))
            );

            DrawContentTexture(spriteBatch, FactoryTextureCatalog.BoatTexture, rect);
        }

        private void DrawInteractionOverlay(
            SpriteBatch spriteBatch,
            FactoryWorld world,
            FactoryCamera camera,
            FactoryInteraction interaction,
            Rectangle worldBounds,
            Vector2 cameraOffset,
            float interactionRadius
        )
        {
            Point mousePosition = Input.mousePosition;
            Point hoverTile = camera.ScreenToTile(mousePosition, worldBounds, world.Player.Position);

            if (worldBounds.Contains(mousePosition) && world.InBounds(hoverTile.X, hoverTile.Y))
            {
                Rectangle hoverRect = camera.GetTileRect(hoverTile.X, hoverTile.Y, cameraOffset);
                Color hoverColor = Vector2.Distance(world.Player.Position, FactoryWorld.TileCenter(hoverTile)) <= interactionRadius
                    ? new Color(255, 255, 255, 90)
                    : new Color(220, 60, 60, 95);
                DrawBorder(spriteBatch, hoverRect, 3, hoverColor);
            }

            if (!world.InBounds(interaction.ActiveTile.X, interaction.ActiveTile.Y)) return;

            Rectangle activeRect = camera.GetTileRect(interaction.ActiveTile.X, interaction.ActiveTile.Y, cameraOffset);
            DrawBorder(spriteBatch, activeRect, 4, new Color(250, 231, 138, 190));

            FactoryTile tile = world.GetTile(interaction.ActiveTile);
            float required = interaction.GetRequiredWorkSeconds(tile);
            if (required <= 0) return;

            float progress = MathHelper.Clamp(interaction.Progress / required, 0, 1);
            Rectangle bar = new(activeRect.X, activeRect.Y - 8, activeRect.Width, 5);
            spriteBatch.Draw(_pixel, bar, new Color(0, 0, 0, 160));
            spriteBatch.Draw(_pixel, new Rectangle(bar.X, bar.Y, (int)(bar.Width * progress), bar.Height), new Color(245, 205, 92));
        }

        private void DrawVitals(SpriteBatch spriteBatch, FactoryPlayer player, Rectangle worldBounds)
        {
            int pipWidth = 11;
            int pipHeight = 14;
            int spacing = 4;
            int y = worldBounds.Bottom - 34;
            int foodX = worldBounds.Left + 18;
            int healthX = foodX + (FactoryPlayer.MaxFoodPoints * (pipWidth + spacing)) + 28;

            DrawPointBar(spriteBatch, foodX, y, player.FoodPoints, FactoryPlayer.MaxFoodPoints, pipWidth, pipHeight, spacing, new Color(126, 89, 43));
            DrawPointBar(spriteBatch, healthX, y, player.HealthPoints, FactoryPlayer.MaxHealthPoints, pipWidth, pipHeight, spacing, new Color(187, 78, 78));
        }

        private void DrawPointBar(
            SpriteBatch spriteBatch,
            int startX,
            int y,
            int current,
            int max,
            int pipWidth,
            int pipHeight,
            int spacing,
            Color fill
        )
        {
            for (int i = 0; i < max; i++)
            {
                Rectangle pip = new(startX + i * (pipWidth + spacing), y, pipWidth, pipHeight);
                spriteBatch.Draw(_pixel, pip, i < current ? fill : new Color(42, 39, 37));
                DrawBorder(spriteBatch, pip, 1, new Color(18, 18, 18, 160));
            }
        }

        private void DrawMinimap(SpriteBatch spriteBatch, FactoryWorld world, Rectangle worldBounds)
        {
            if (!world.Minimap.HasDiscovery(world.Player.Level)) return;

            Rectangle outer = new(worldBounds.Right - 186, worldBounds.Top + 18, 168, 168);
            Rectangle inner = new(outer.X + 10, outer.Y + 10, outer.Width - 20, outer.Height - 20);
            int cellSize = 4;
            int viewRadius = Math.Min(24, (inner.Width / cellSize - 1) / 2);
            Point playerTile = world.PlayerTile;
            int startX = inner.X + inner.Width / 2 - cellSize / 2;
            int startY = inner.Y + inner.Height / 2 - cellSize / 2;

            spriteBatch.Draw(_pixel, outer, new Color(14, 16, 18, 185));
            DrawBorder(spriteBatch, outer, 2, new Color(98, 104, 93, 220));

            for (int offsetX = -viewRadius; offsetX <= viewRadius; offsetX++)
            {
                for (int offsetY = -viewRadius; offsetY <= viewRadius; offsetY++)
                {
                    int x = playerTile.X + offsetX;
                    int y = playerTile.Y + offsetY;
                    if (!world.Minimap.IsDiscovered(world.Player.Level, x, y)) continue;

                    FactoryTile tile = world.GetTile(world.Player.Level, new Point(x, y));
                    if (tile == null) continue;
                    Color color = world.GetMinimapGroundColor(world.Player.Level, new Point(x, y), tile) * 0.9f;
                    if (tile.Floor == FactoryFloor.Wood)
                        color = new Color(146, 103, 61);
                    if (world.Player.Level == FactoryLevel.Cave && tile.IsHole)
                        color = new Color(192, 164, 78);
                    if (tile.ObjectType == FactoryObjectType.Ladder)
                        color = new Color(214, 167, 92);

                    spriteBatch.Draw(
                        _pixel,
                        new Rectangle(
                            startX + offsetX * cellSize,
                            startY + offsetY * cellSize,
                            cellSize,
                            cellSize),
                        color
                    );
                }
            }

            Rectangle playerDot = new(
                startX,
                startY,
                Math.Max(2, cellSize),
                Math.Max(2, cellSize)
            );
            spriteBatch.Draw(_pixel, playerDot, new Color(239, 221, 154));
        }

        private void DrawPlayer(SpriteBatch spriteBatch, Vector2 playerPosition, int tileSize, Vector2 cameraOffset)
        {
            int size = Math.Max(22, (int)(tileSize * 0.66f));
            Rectangle playerRect = new(
                (int)(playerPosition.X * tileSize + cameraOffset.X - size / 2f),
                (int)(playerPosition.Y * tileSize + cameraOffset.Y - size / 2f),
                size,
                size
            );

            DrawContentTexture(spriteBatch, FactoryTextureCatalog.PlayerTexture, playerRect);
        }

        private void DrawResourceGlyph(SpriteBatch spriteBatch, FactoryResource resource, Rectangle rect)
        {
            DrawContentTexture(spriteBatch, FactoryTextureCatalog.GetResourceTexture(resource), rect);
        }

        private void DrawInnerRect(SpriteBatch spriteBatch, Rectangle rect, int inset, Color color)
        {
            Rectangle inner = new(
                rect.X + inset,
                rect.Y + inset,
                Math.Max(2, rect.Width - inset * 2),
                Math.Max(2, rect.Height - inset * 2)
            );
            spriteBatch.Draw(_pixel, inner, color);
        }

        private void DrawContentTexture(SpriteBatch spriteBatch, string assetPath, Rectangle destination)
        {
            Texture2D texture = GetContentTexture(assetPath);
            if (texture == null)
                return;

            spriteBatch.Draw(texture, destination, Color.White);
        }

        private Texture2D GetContentTexture(string assetPath)
        {
            if (string.IsNullOrWhiteSpace(assetPath))
                return null;

            if (_contentTextures.TryGetValue(assetPath, out Texture2D texture))
                return texture;

            texture = Game1.LoadGameTexture(assetPath);
            _contentTextures[assetPath] = texture;
            return texture;
        }

        private void EnsureTextures(GraphicsDevice device)
        {
            if (_pixel == null)
            {
                _pixel = new Texture2D(device, 1, 1);
                _pixel.SetData([Color.White]);
            }
        }
    }
}
