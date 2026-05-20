using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;

namespace AkiGames.Scripts
{
    internal readonly struct FactoryChunkKey : IEquatable<FactoryChunkKey>
    {
        public FactoryChunkKey(FactoryLevel level, int chunkX, int chunkY)
        {
            Level = level;
            ChunkX = chunkX;
            ChunkY = chunkY;
        }

        public FactoryLevel Level { get; }
        public int ChunkX { get; }
        public int ChunkY { get; }

        public bool Equals(FactoryChunkKey other) =>
            Level == other.Level && ChunkX == other.ChunkX && ChunkY == other.ChunkY;

        public override bool Equals(object obj) =>
            obj is FactoryChunkKey other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine((int)Level, ChunkX, ChunkY);
    }

    internal readonly struct FactoryPlacedObjectKey : IEquatable<FactoryPlacedObjectKey>
    {
        public FactoryPlacedObjectKey(FactoryLevel level, Point tile)
        {
            Level = level;
            Tile = tile;
        }

        public FactoryLevel Level { get; }
        public Point Tile { get; }

        public bool Equals(FactoryPlacedObjectKey other) =>
            Level == other.Level && Tile == other.Tile;

        public override bool Equals(object obj) =>
            obj is FactoryPlacedObjectKey other && Equals(other);

        public override int GetHashCode() =>
            HashCode.Combine((int)Level, Tile.X, Tile.Y);
    }

    internal sealed class FactoryChunk
    {
        public const int Size = 64;

        public FactoryChunk(FactoryLevel level, int chunkX, int chunkY)
        {
            Level = level;
            ChunkX = chunkX;
            ChunkY = chunkY;
            Tiles = new FactoryTile[Size, Size];
            Creatures = [];
        }

        public FactoryLevel Level { get; }
        public int ChunkX { get; }
        public int ChunkY { get; }
        public FactoryTile[,] Tiles { get; }
        public List<FactoryCreature> Creatures { get; }
    }

    internal sealed class FactoryBoatEntity
    {
        public FactoryBoatEntity(Vector2 position)
        {
            Position = position;
        }

        public Vector2 Position { get; set; }
        public Point Tile => new((int)Math.Floor(Position.X), (int)Math.Floor(Position.Y));
    }

    internal readonly struct FactorySurfaceBiomeData
    {
        public FactorySurfaceBiomeData(
            FactoryBiome baseBiome,
            FactoryBiome biome,
            float lakeScore,
            float mountainScore,
            float forestScore,
            float fieldScore,
            float riverBand)
        {
            BaseBiome = baseBiome;
            Biome = biome;
            LakeScore = lakeScore;
            MountainScore = mountainScore;
            ForestScore = forestScore;
            FieldScore = fieldScore;
            RiverBand = riverBand;
        }

        public FactoryBiome BaseBiome { get; }
        public FactoryBiome Biome { get; }
        public float LakeScore { get; }
        public float MountainScore { get; }
        public float ForestScore { get; }
        public float FieldScore { get; }
        public float RiverBand { get; }
    }

    internal readonly struct FactoryOreDepositSample
    {
        public FactoryOreDepositSample(FactoryGround ground, int amount, float weight)
        {
            Ground = ground;
            Amount = amount;
            Weight = weight;
        }

        public FactoryGround Ground { get; }
        public int Amount { get; }
        public float Weight { get; }
        public bool IsValid => Amount > 0;
    }

    internal sealed class FactoryWorld
    {
        private const int DiscoveryRadius = 9;
        private const int ActiveChunkRadius = 2;
        private const int SpawnSafeRadius = 5;
        private const float TreeSpawnTickSeconds = 120f;
        private const float HighGrassSpawnTickSeconds = 18f;
        private const float TreeStageSeconds = 110f;
        private const float FoodTickSeconds = 120f;

        private readonly Dictionary<FactoryChunkKey, FactoryChunk> _chunks = [];
        private readonly Dictionary<FactoryPlacedObjectKey, FactoryFurnaceState> _furnaces = [];
        private readonly Dictionary<FactoryPlacedObjectKey, FactoryDrillState> _drills = [];
        private readonly List<FactoryBoatEntity> _boats = [];
        private readonly List<FactoryCreature> _visibleCreatures = [];
        private int _seed;

        private float _treeSpawnTimer;
        private float _highGrassSpawnTimer;
        private float _foodTickTimer;
        private FactoryBoatEntity _mountedBoat;
        private bool _hasVisibleChunkWindow;
        private FactoryLevel _visibleChunkLevel;
        private int _visibleMinChunkX;
        private int _visibleMaxChunkX;
        private int _visibleMinChunkY;
        private int _visibleMaxChunkY;

        public int Width { get; }
        public int Height { get; }
        public FactoryPlayer Player { get; }
        public FactoryMinimap Minimap { get; }
        public IReadOnlyList<FactoryCreature> ActiveCreatures => _visibleCreatures;
        public IReadOnlyList<FactoryBoatEntity> Boats => _boats;
        public int Seed => _seed;

        public Point PlayerTile =>
            new((int)Math.Floor(Player.Position.X), (int)Math.Floor(Player.Position.Y));

        public FactoryWorld(int width, int height, int seed)
        {
            Width = Math.Max(256, width);
            Height = Math.Max(256, height);
            _seed = seed;

            Player = new FactoryPlayer(new Vector2(Width / 2f + 0.5f, Height / 2f + 0.5f));
            Minimap = new FactoryMinimap(Width, Height);

            ClearStartingArea(PlayerTile.X, PlayerTile.Y, SpawnSafeRadius);
            RemoveCreaturesNear(Player.Position, 10f);
            Minimap.Discover(Player.Position, Player.Level, DiscoveryRadius, Width, Height);
        }

        public void EnsureViewLoaded(FactoryLevel level, int minTileX, int minTileY, int maxTileX, int maxTileY)
        {
            minTileX = Math.Clamp(minTileX, 0, Width - 1);
            minTileY = Math.Clamp(minTileY, 0, Height - 1);
            maxTileX = Math.Clamp(maxTileX, 0, Width - 1);
            maxTileY = Math.Clamp(maxTileY, 0, Height - 1);

            int minChunkX = GetChunkX(minTileX);
            int maxChunkX = GetChunkX(maxTileX);
            int minChunkY = GetChunkY(minTileY);
            int maxChunkY = GetChunkY(maxTileY);

            bool changed =
                !_hasVisibleChunkWindow ||
                _visibleChunkLevel != level ||
                _visibleMinChunkX != minChunkX ||
                _visibleMaxChunkX != maxChunkX ||
                _visibleMinChunkY != minChunkY ||
                _visibleMaxChunkY != maxChunkY;

            for (int chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
            {
                for (int chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
                    GetOrCreateChunk(level, chunkX, chunkY);
            }

            _hasVisibleChunkWindow = true;
            _visibleChunkLevel = level;
            _visibleMinChunkX = minChunkX;
            _visibleMaxChunkX = maxChunkX;
            _visibleMinChunkY = minChunkY;
            _visibleMaxChunkY = maxChunkY;

            if (changed)
                RebuildVisibleCreatureList();
        }

        public FactoryTile GetTile(Point tile) =>
            GetTile(Player.Level, tile);

        public FactoryTile GetTile(FactoryLevel level, Point tile)
        {
            if (!InBounds(tile.X, tile.Y)) return null;

            FactoryChunk chunk = GetOrCreateChunk(level, GetChunkX(tile.X), GetChunkY(tile.Y));
            return chunk.Tiles[GetLocalX(tile.X), GetLocalY(tile.Y)];
        }

        public FactoryBiome GetSurfaceBiome(Point tile) =>
            SampleSurfaceBiome(tile.X, tile.Y).Biome;

        public string GetDebugHudText()
        {
            Point tile = PlayerTile;
            FactorySurfaceBiomeData biome = SampleSurfaceBiome(tile.X, tile.Y);
            string levelText = Player.Level == FactoryLevel.Surface ? "Surface" : "Cave";
            string biomeText = Player.Level == FactoryLevel.Surface
                ? biome.Biome == FactoryBiome.River
                    ? $"River ({FactoryRules.BiomeName(biome.BaseBiome)})"
                    : FactoryRules.BiomeName(biome.Biome)
                : $"{FactoryRules.BiomeName(biome.BaseBiome)} cave";

            return $"{levelText} | {biomeText} | {tile.X}, {tile.Y}";
        }

        public Color GetSurfaceGrassColor(Point tile)
        {
            FactorySurfaceBiomeData sample = SampleSurfaceBiome(tile.X, tile.Y);
            float forestWeight = sample.ForestScore;
            float fieldWeight = sample.FieldScore;
            float mountainWeight = sample.MountainScore;
            float lakeWeight = sample.LakeScore;
            float total = Math.Max(0.001f, forestWeight + fieldWeight + mountainWeight + lakeWeight);

            Vector3 forestColor = new(58f, 132f, 68f);
            Vector3 fieldColor = new(112f, 155f, 78f);
            Vector3 mountainColor = new(92f, 122f, 71f);
            Vector3 lakeColor = new(96f, 148f, 90f);
            Vector3 blended =
                forestColor * (forestWeight / total) +
                fieldColor * (fieldWeight / total) +
                mountainColor * (mountainWeight / total) +
                lakeColor * (lakeWeight / total);

            float variation = (Next01(tile.X, tile.Y, 1823) - 0.5f) * 18f;
            blended += new Vector3(variation * 0.25f, variation, variation * 0.18f);

            return new Color(
                (int)Math.Clamp(blended.X, 28f, 180f),
                (int)Math.Clamp(blended.Y, 60f, 190f),
                (int)Math.Clamp(blended.Z, 28f, 120f));
        }

        public Color GetMinimapGroundColor(FactoryLevel level, Point tile, FactoryTile current)
        {
            if (current == null)
                return Color.Magenta;

            if (level == FactoryLevel.Surface && current.Ground == FactoryGround.Grass)
                return GetSurfaceGrassColor(tile);

            return current.Ground switch
            {
                FactoryGround.Stone => new Color(103, 107, 103),
                FactoryGround.Cave => new Color(34, 35, 38),
                FactoryGround.Water => new Color(45, 103, 151),
                FactoryGround.Sand => new Color(186, 169, 116),
                FactoryGround.StoneOre => new Color(104, 108, 105),
                FactoryGround.Iron => new Color(91, 103, 103),
                FactoryGround.Copper => new Color(134, 90, 56),
                FactoryGround.Coal => new Color(47, 48, 46),
                _ => Color.Magenta
            };
        }

        public bool InBounds(int x, int y) =>
            x >= 0 && y >= 0 && x < Width && y < Height;

        public bool CanStandAt(Vector2 position, bool allowWater = false, Point? ignoreBlockingTile = null)
        {
            int x = (int)Math.Floor(position.X);
            int y = (int)Math.Floor(position.Y);
            return CanStandAt(Player.Level, new Point(x, y), allowWater, ignoreBlockingTile);
        }

        public bool IsHoleTile(FactoryLevel level, Point tile) =>
            GetTile(level, tile)?.IsHole == true;

        public FactoryCreature GetCreatureAt(FactoryLevel level, Point tile, bool blockingOnly = false)
        {
            if (level != FactoryLevel.Surface || !InBounds(tile.X, tile.Y))
                return null;

            FactoryChunk chunk = TryGetLoadedChunk(FactoryLevel.Surface, GetChunkX(tile.X), GetChunkY(tile.Y));
            if (chunk == null)
                return null;

            for (int i = 0; i < chunk.Creatures.Count; i++)
            {
                FactoryCreature creature = chunk.Creatures[i];
                if (creature.Tile != tile) continue;
                if (blockingOnly && !creature.BlocksMovement) continue;
                return creature;
            }

            return null;
        }

        public bool CanCreatureStandAt(FactoryCreature creature, Point tile)
        {
            if (!InBounds(tile.X, tile.Y)) return false;

            FactoryTile surfaceTile = GetTile(FactoryLevel.Surface, tile);
            if (surfaceTile == null) return false;

            if (creature.CreatureType == FactoryCreatureType.Fish)
                return surfaceTile.Ground == FactoryGround.Water && GetBoatAt(tile) == null;

            if (surfaceTile.IsHole || surfaceTile.Ground == FactoryGround.Water)
                return false;

            if (TileObjectBlocksCreatureMovement(surfaceTile))
                return false;

            if (Player.Level == FactoryLevel.Surface && PlayerTile == tile)
                return false;

            for (int chunkY = Math.Max(0, GetChunkY(tile.Y) - 1); chunkY <= Math.Min(GetChunkY(tile.Y) + 1, GetChunkY(Height - 1)); chunkY++)
            {
                for (int chunkX = Math.Max(0, GetChunkX(tile.X) - 1); chunkX <= Math.Min(GetChunkX(tile.X) + 1, GetChunkX(Width - 1)); chunkX++)
                {
                    FactoryChunk chunk = TryGetLoadedChunk(FactoryLevel.Surface, chunkX, chunkY);
                    if (chunk == null) continue;

                    for (int i = 0; i < chunk.Creatures.Count; i++)
                    {
                        FactoryCreature other = chunk.Creatures[i];
                        if (ReferenceEquals(other, creature) || !other.BlocksMovement) continue;
                        if (other.Tile == tile) return false;
                    }
                }
            }

            return true;
        }

        public bool IsLadderAt(Point tile)
        {
            FactoryTile surface = GetTile(FactoryLevel.Surface, tile);
            FactoryTile cave = GetTile(FactoryLevel.Cave, tile);
            return surface?.ObjectType == FactoryObjectType.Ladder &&
                   cave?.ObjectType == FactoryObjectType.Ladder;
        }

        public FactoryBoatEntity GetBoatAt(Point tile)
        {
            for (int i = 0; i < _boats.Count; i++)
            {
                if (_boats[i].Tile == tile)
                    return _boats[i];
            }

            return null;
        }

        public bool IsBoatAt(Point tile) =>
            GetBoatAt(tile) != null;

        public bool CanPlaceLadder(Point tile) =>
            Player.Level == FactoryLevel.Surface &&
            GetTile(FactoryLevel.Surface, tile)?.IsHole == true &&
            !IsLadderAt(tile);

        public bool CanPlaceBoat(Point tile)
        {
            FactoryTile tileData = GetTile(FactoryLevel.Surface, tile);
            return Player.Level == FactoryLevel.Surface &&
                tileData != null &&
                !tileData.IsHole &&
                tileData.Ground == FactoryGround.Water &&
                tileData.ObjectType == FactoryObjectType.Empty &&
                GetBoatAt(tile) == null;
        }

        public bool CanPlaceWoodFloor(Point tile)
        {
            FactoryTile tileData = GetTile(tile);
            return tileData != null &&
                CanPlaceDryStructure(Player.Level, tile, tileData) &&
                tileData.Floor == FactoryFloor.Empty;
        }

        public bool CanPlaceWoodWall(Point tile)
        {
            FactoryTile tileData = GetTile(tile);
            return tileData != null && CanPlaceDryStructure(Player.Level, tile, tileData);
        }

        public bool CanPlaceStoneWall(Point tile)
        {
            FactoryTile tileData = GetTile(tile);
            return tileData != null && CanPlaceDryStructure(Player.Level, tile, tileData);
        }

        public bool CanPlaceWoodDoor(Point tile)
        {
            FactoryTile tileData = GetTile(tile);
            return tileData != null && CanPlaceDryStructure(Player.Level, tile, tileData);
        }

        public bool CanPlaceFurnace(Point tile)
        {
            FactoryTile tileData = GetTile(tile);
            return tileData != null && CanPlaceDryStructure(Player.Level, tile, tileData);
        }

        public bool CanPlaceSolidFuelDrill(Point tile)
        {
            FactoryTile tileData = GetTile(tile);
            return tileData != null &&
                tileData.Floor == FactoryFloor.Empty &&
                tileData.HasOre &&
                CanPlaceDryStructure(Player.Level, tile, tileData);
        }

        public bool CanPlaceSnare(Point tile)
        {
            FactoryTile tileData = GetTile(FactoryLevel.Surface, tile);
            return Player.Level == FactoryLevel.Surface &&
                tileData != null &&
                CanPlaceDryStructure(FactoryLevel.Surface, tile, tileData);
        }

        public void PlaceLadder(Point tile)
        {
            FactoryTile surfaceTile = GetTile(FactoryLevel.Surface, tile);
            FactoryTile caveTile = GetTile(FactoryLevel.Cave, tile);
            if (surfaceTile == null || caveTile == null) return;

            surfaceTile.ClearObject();
            caveTile.ClearObject();
            surfaceTile.ObjectType = FactoryObjectType.Ladder;
            caveTile.ObjectType = FactoryObjectType.Ladder;
            surfaceTile.IsPlayerPlacedObject = true;
            caveTile.IsPlayerPlacedObject = true;
        }

        public void RemoveLadder(Point tile)
        {
            GetTile(FactoryLevel.Surface, tile)?.ClearObject();
            GetTile(FactoryLevel.Cave, tile)?.ClearObject();
        }

        public void PlaceBoat(Point tile)
        {
            if (GetBoatAt(tile) != null) return;
            _boats.Add(new FactoryBoatEntity(TileCenter(tile)));
        }

        public bool RemoveBoat(Point tile)
        {
            FactoryBoatEntity boat = GetBoatAt(tile);
            if (boat == null || ReferenceEquals(boat, _mountedBoat))
                return false;

            _boats.Remove(boat);
            return true;
        }

        public void PlaceWoodFloor(Point tile)
        {
            FactoryTile tileData = GetTile(tile);
            if (tileData == null) return;
            tileData.Floor = FactoryFloor.Wood;
        }

        public void PlaceWoodWall(Point tile) =>
            SetPlacedObject(Player.Level, tile, FactoryObjectType.WoodWall);

        public void PlaceStoneWall(Point tile) =>
            SetPlacedObject(Player.Level, tile, FactoryObjectType.StoneWall, 20);

        public void PlaceWoodDoor(Point tile)
        {
            SetPlacedObject(Player.Level, tile, FactoryObjectType.WoodDoor);
            FactoryTile tileData = GetTile(tile);
            if (tileData != null)
                tileData.DoorOpen = false;
        }

        public void PlaceFurnace(Point tile)
        {
            SetPlacedObject(Player.Level, tile, FactoryObjectType.Furnace);
            _furnaces[new FactoryPlacedObjectKey(Player.Level, tile)] = new FactoryFurnaceState();
        }

        public void PlaceSolidFuelDrill(Point tile)
        {
            SetPlacedObject(Player.Level, tile, FactoryObjectType.SolidFuelDrill);
            _drills[new FactoryPlacedObjectKey(Player.Level, tile)] = new FactoryDrillState();
        }

        public void RemoveFurnace(FactoryLevel level, Point tile)
        {
            GetTile(level, tile)?.ClearObject();
            _furnaces.Remove(new FactoryPlacedObjectKey(level, tile));
        }

        public void RemoveSolidFuelDrill(FactoryLevel level, Point tile)
        {
            GetTile(level, tile)?.ClearObject();
            _drills.Remove(new FactoryPlacedObjectKey(level, tile));
        }

        public void PlaceSnare(Point tile) =>
            SetPlacedObject(FactoryLevel.Surface, tile, FactoryObjectType.Snare);

        public FactoryFurnaceState GetFurnace(FactoryLevel level, Point tile)
        {
            _furnaces.TryGetValue(new FactoryPlacedObjectKey(level, tile), out FactoryFurnaceState furnace);
            return furnace;
        }

        public FactoryDrillState GetSolidFuelDrill(FactoryLevel level, Point tile)
        {
            _drills.TryGetValue(new FactoryPlacedObjectKey(level, tile), out FactoryDrillState drill);
            return drill;
        }

        public IFactoryStorageMachine GetStorageMachine(FactoryLevel level, Point tile)
        {
            FactoryTile tileData = GetTile(level, tile);
            return tileData?.ObjectType switch
            {
                FactoryObjectType.Furnace => GetFurnace(level, tile),
                FactoryObjectType.SolidFuelDrill => GetSolidFuelDrill(level, tile),
                _ => null
            };
        }

        public bool TryToggleDoor(Point tile, out bool isOpen)
        {
            FactoryTile tileData = GetTile(tile);
            if (tileData?.ObjectType != FactoryObjectType.WoodDoor)
            {
                isOpen = false;
                return false;
            }

            bool nextOpen = !tileData.DoorOpen;
            if (!nextOpen)
            {
                if (PlayerTile == tile)
                {
                    isOpen = tileData.DoorOpen;
                    return false;
                }

                if (GetCreatureAt(Player.Level, tile, blockingOnly: true) != null)
                {
                    isOpen = tileData.DoorOpen;
                    return false;
                }
            }

            tileData.DoorOpen = nextOpen;
            isOpen = nextOpen;
            return true;
        }

        public bool TryBoardBoat(Point tile)
        {
            if (Player.Level != FactoryLevel.Surface || Player.IsOnBoat)
                return false;

            FactoryBoatEntity boat = GetBoatAt(tile);
            if (boat == null) return false;

            _mountedBoat = boat;
            Player.SetBoatMounted(true);
            Player.SetPosition(boat.Position);
            return true;
        }

        public bool TryLeaveBoat()
        {
            if (!Player.IsOnBoat || Player.Level != FactoryLevel.Surface || _mountedBoat == null)
                return false;

            Point boatTile = _mountedBoat.Tile;
            if (!FindNearestStandableTile(FactoryLevel.Surface, boatTile, 3, out Point destination))
                return false;

            Player.SetBoatMounted(false);
            Player.SetPosition(TileCenter(destination));
            _mountedBoat.Position = TileCenter(boatTile);
            _mountedBoat = null;
            return true;
        }

        public bool TryUseLadder(Point tile)
        {
            if (!IsLadderAt(tile)) return false;

            if (Player.Level == FactoryLevel.Surface)
            {
                Player.SetBoatMounted(false);
                _mountedBoat = null;
                Player.MoveToLevel(FactoryLevel.Cave);
                Player.SetPosition(TileCenter(tile));
                return true;
            }

            if (!FindNearestStandableTile(FactoryLevel.Surface, tile, 4, out Point destination))
                return false;

            Player.SetBoatMounted(false);
            _mountedBoat = null;
            Player.MoveToLevel(FactoryLevel.Surface);
            Player.SetPosition(TileCenter(destination));
            return true;
        }

        public void MovePlayer(Vector2 direction, float speed, float dt)
        {
            Point oldTile = PlayerTile;
            Player.Move(direction, speed, dt, this);

            if (Player.IsOnBoat && Player.Level == FactoryLevel.Surface && _mountedBoat != null)
                _mountedBoat.Position = Player.Position;

            if (oldTile != PlayerTile)
                RebuildVisibleCreatureList();
        }

        public void Update(float dt, FactoryInventory inventory)
        {
            UpdateNeeds(dt);
            UpdateVegetation(dt);
            UpdateMachines(dt, inventory);
            UpdateCreatures(dt);
            Minimap.Discover(Player.Position, Player.Level, DiscoveryRadius, Width, Height);
        }

        public string DescribeHoverTile(Point tile, float interactionRadius)
        {
            FactoryTile current = GetTile(tile);
            if (current == null) return "";

            List<string> parts =
            [
                Player.Level == FactoryLevel.Surface ? "Surface" : "Cave",
                current.IsHole ? "Hole" : FactoryRules.GroundName(current.Ground)
            ];

            FactoryCreature creature = GetCreatureAt(Player.Level, tile);
            if (creature != null)
                parts.Add(FactoryRules.CreatureName(creature.CreatureType));

            if (current.Floor != FactoryFloor.Empty)
                parts.Add(FactoryRules.FloorName(current.Floor));

            if (current.HasOre)
                parts.Add($"{current.OreRemaining} ore left");

            if (current.ObjectType != FactoryObjectType.Empty)
            {
                string objectName = FactoryRules.ObjectName(current.ObjectType);
                if (current.ObjectType == FactoryObjectType.WoodDoor)
                    objectName = current.DoorOpen ? "Open wood door" : "Closed wood door";
                else if (current.ObjectType == FactoryObjectType.Snare && current.SnareHasCatch)
                    objectName = "Snare with rabbit";

                if (current.ObjectResourceAmount > 0)
                    objectName += $" ({current.ObjectResourceAmount})";
                parts.Add(objectName);

                if (current.ObjectType == FactoryObjectType.Furnace)
                {
                    FactoryFurnaceState furnace = GetFurnace(Player.Level, tile);
                    if (furnace?.FuelSlot?.IsEmpty == false)
                        parts.Add($"fuel {furnace.FuelSlot.Count}");
                    if (furnace?.InputSlot?.IsEmpty == false)
                        parts.Add($"{furnace.InputSlot.Count} {FactoryRules.ResourceCostName(furnace.InputSlot.Resource)} queued");
                }
                else if (current.ObjectType == FactoryObjectType.SolidFuelDrill)
                {
                    FactoryDrillState drill = GetSolidFuelDrill(Player.Level, tile);
                    if (drill?.FuelSlot?.IsEmpty == false)
                        parts.Add($"fuel {drill.FuelSlot.Count}");
                    if (drill?.OutputSlot?.IsEmpty == false)
                        parts.Add($"{drill.OutputSlot.Count} stored");
                    if (drill != null && drill.FuelOreChargesRemaining > 0)
                        parts.Add($"{drill.FuelOreChargesRemaining}/5 fuel");
                }
            }

            FactoryBoatEntity boat = Player.Level == FactoryLevel.Surface ? GetBoatAt(tile) : null;
            if (boat != null)
                parts.Add("Boat");

            bool blocked = (Player.Level == FactoryLevel.Surface && current.IsHole) ||
                (current.Ground == FactoryGround.Water && !Player.IsOnBoat) ||
                TileObjectBlocksPlayerMovement(current);
            if (creature?.BlocksMovement == true)
                blocked = true;
            if (blocked)
                parts.Add("blocked");

            if (IsLadderAt(tile))
                parts.Add(Player.Level == FactoryLevel.Surface ? "LMB: go down" : "LMB: go up");
            else if (current.ObjectType == FactoryObjectType.WoodDoor)
                parts.Add(current.DoorOpen ? "LMB: close" : "LMB: open");
            else if (current.ObjectType == FactoryObjectType.Furnace)
                parts.Add("LMB: open furnace");
            else if (current.ObjectType == FactoryObjectType.SolidFuelDrill)
                parts.Add("LMB: open drill");
            else if (CanPlaceLadder(tile))
                parts.Add("LMB with ladder: place");
            else if (boat != null && !Player.IsOnBoat)
                parts.Add("LMB: board boat");
            else if (CanPlaceBoat(tile))
                parts.Add("LMB with boat: place");
            else if (CanPlaceSolidFuelDrill(tile))
                parts.Add("LMB with drill: place");

            if (Vector2.Distance(Player.Position, TileCenter(tile)) > interactionRadius)
                parts.Add("out of reach");

            return string.Join(" | ", parts);
        }

        public FactoryWorldSaveData ExportState()
        {
            FactoryWorldSaveData data = new()
            {
                Seed = _seed,
                PlayerX = Player.Position.X,
                PlayerY = Player.Position.Y,
                Level = Player.Level,
                IsOnBoat = Player.IsOnBoat,
                HealthPoints = Player.HealthPoints,
                FoodPoints = Player.FoodPoints,
                FoodTickSecondsRemaining = Math.Max(0f, FoodTickSeconds - _foodTickTimer),
                SurfaceChunks = ExportChunks(FactoryLevel.Surface),
                CaveChunks = ExportChunks(FactoryLevel.Cave),
                Boats = _boats.Select(boat => new FactoryBoatSaveData
                {
                    X = boat.Position.X,
                    Y = boat.Position.Y
                }).ToArray(),
                Furnaces = _furnaces.Select(pair => new FactoryFurnaceSaveData
                {
                    Level = pair.Key.Level,
                    X = pair.Key.Tile.X,
                    Y = pair.Key.Tile.Y,
                    Fuel = FactoryInventorySlotSaveData.FromSlot(pair.Value.FuelSlot),
                    Input = FactoryInventorySlotSaveData.FromSlot(pair.Value.InputSlot),
                    WorkProgress = pair.Value.WorkProgress
                }).ToArray(),
                Drills = _drills.Select(pair => new FactoryDrillSaveData
                {
                    Level = pair.Key.Level,
                    X = pair.Key.Tile.X,
                    Y = pair.Key.Tile.Y,
                    Fuel = FactoryInventorySlotSaveData.FromSlot(pair.Value.FuelSlot),
                    Output = FactoryInventorySlotSaveData.FromSlot(pair.Value.OutputSlot),
                    WorkProgress = pair.Value.WorkProgress,
                    FuelOreChargesRemaining = pair.Value.FuelOreChargesRemaining
                }).ToArray(),
                SurfaceDiscovery = Convert.ToBase64String(Minimap.Export(FactoryLevel.Surface)),
                CaveDiscovery = Convert.ToBase64String(Minimap.Export(FactoryLevel.Cave))
            };

            return data;
        }

        public void LoadState(FactoryWorldSaveData data)
        {
            if (data == null) return;

            _seed = data.Seed;
            _chunks.Clear();
            _furnaces.Clear();
            _drills.Clear();
            _boats.Clear();
            _mountedBoat = null;
            _hasVisibleChunkWindow = false;
            _visibleCreatures.Clear();

            ImportChunks(FactoryLevel.Surface, data.SurfaceChunks);
            ImportChunks(FactoryLevel.Cave, data.CaveChunks);

            if (data.Boats != null)
            {
                for (int i = 0; i < data.Boats.Length; i++)
                    _boats.Add(new FactoryBoatEntity(new Vector2(data.Boats[i].X, data.Boats[i].Y)));
            }

            if (data.Furnaces != null)
            {
                for (int i = 0; i < data.Furnaces.Length; i++)
                {
                    FactoryFurnaceSaveData furnaceSave = data.Furnaces[i];
                    FactoryPlacedObjectKey key = new(furnaceSave.Level, new Point(furnaceSave.X, furnaceSave.Y));
                    FactoryFurnaceState furnace = new();
                    furnaceSave.Fuel?.ApplyTo(furnace.FuelSlot);
                    furnaceSave.Input?.ApplyTo(furnace.InputSlot);
                    furnace.WorkProgress = furnaceSave.WorkProgress;
                    _furnaces[key] = furnace;
                }
            }

            if (data.Drills != null)
            {
                for (int i = 0; i < data.Drills.Length; i++)
                {
                    FactoryDrillSaveData drillSave = data.Drills[i];
                    FactoryPlacedObjectKey key = new(drillSave.Level, new Point(drillSave.X, drillSave.Y));
                    FactoryDrillState drill = new();
                    drillSave.Fuel?.ApplyTo(drill.FuelSlot);
                    drillSave.Output?.ApplyTo(drill.OutputSlot);
                    drill.WorkProgress = drillSave.WorkProgress;
                    drill.FuelOreChargesRemaining = Math.Max(0, drillSave.FuelOreChargesRemaining);
                    _drills[key] = drill;
                }
            }

            if (!string.IsNullOrEmpty(data.SurfaceDiscovery))
                Minimap.Import(FactoryLevel.Surface, Convert.FromBase64String(data.SurfaceDiscovery));
            if (!string.IsNullOrEmpty(data.CaveDiscovery))
                Minimap.Import(FactoryLevel.Cave, Convert.FromBase64String(data.CaveDiscovery));

            Player.LoadState(
                new Vector2(data.PlayerX, data.PlayerY),
                data.Level,
                data.IsOnBoat,
                data.HealthPoints,
                data.FoodPoints
            );

            if (data.FoodTickSecondsRemaining > 0f)
                _foodTickTimer = MathHelper.Clamp(FoodTickSeconds - data.FoodTickSecondsRemaining, 0f, FoodTickSeconds - 0.001f);
            else
                _foodTickTimer = 0f;

            if (Player.IsOnBoat)
            {
                _mountedBoat = FindNearestBoat(Player.Position);
                _mountedBoat ??= CreateBoatAtPlayer();
            }

            RebuildVisibleCreatureList();
        }

        public static Vector2 TileCenter(Point tile) =>
            new(tile.X + 0.5f, tile.Y + 0.5f);

        private FactoryBoatEntity CreateBoatAtPlayer()
        {
            FactoryBoatEntity boat = new(Player.Position);
            _boats.Add(boat);
            return boat;
        }

        private FactoryBoatEntity FindNearestBoat(Vector2 position)
        {
            FactoryBoatEntity nearest = null;
            float nearestDistance = float.MaxValue;

            for (int i = 0; i < _boats.Count; i++)
            {
                float distance = Vector2.DistanceSquared(_boats[i].Position, position);
                if (distance >= nearestDistance) continue;

                nearestDistance = distance;
                nearest = _boats[i];
            }

            return nearest;
        }

        private bool CanStandAt(FactoryLevel level, Point tile, bool allowWater = false, Point? ignoreBlockingTile = null)
        {
            if (!InBounds(tile.X, tile.Y)) return false;

            FactoryTile current = GetTile(level, tile);
            if (current == null) return false;
            if (level == FactoryLevel.Surface && current.IsHole) return false;

            if (allowWater)
            {
                if (level != FactoryLevel.Surface || current.Ground != FactoryGround.Water)
                    return false;
            }
            else if (current.Ground == FactoryGround.Water)
            {
                return false;
            }

            if (TileObjectBlocksPlayerMovement(current))
            {
                bool ignored = ignoreBlockingTile.HasValue && ignoreBlockingTile.Value == tile;
                if (!ignored) return false;
            }

            if (level == FactoryLevel.Surface && GetBoatAt(tile) != null && !Player.IsOnBoat)
                return false;

            if (GetCreatureAt(level, tile, blockingOnly: true) != null)
                return false;

            return true;
        }

        private bool CanPlaceDryStructure(FactoryLevel level, Point tile, FactoryTile tileData) =>
            tileData.ObjectType == FactoryObjectType.Empty &&
            !tileData.IsHole &&
            tileData.Ground != FactoryGround.Water &&
            GetCreatureAt(level, tile, blockingOnly: true) == null &&
            (level != FactoryLevel.Surface || GetBoatAt(tile) == null);

        private static bool TileObjectBlocksPlayerMovement(FactoryTile tile) =>
            tile != null &&
            (tile.ObjectType is FactoryObjectType.Furnace or FactoryObjectType.SolidFuelDrill or
             FactoryObjectType.StoneWall or FactoryObjectType.WoodWall ||
             (tile.ObjectType == FactoryObjectType.WoodDoor && !tile.DoorOpen));

        private static bool TileObjectBlocksCreatureMovement(FactoryTile tile) =>
            tile != null &&
            (FactoryRules.IsBlocking(tile.ObjectType) ||
             (tile.ObjectType == FactoryObjectType.WoodDoor && !tile.DoorOpen));

        private void SetPlacedObject(FactoryLevel level, Point tile, FactoryObjectType objectType, int amount = 0)
        {
            FactoryTile tileData = GetTile(level, tile);
            if (tileData == null) return;

            tileData.ClearObject();
            tileData.ObjectType = objectType;
            tileData.IsPlayerPlacedObject = true;
            tileData.ObjectResourceAmount = amount;
        }

        private bool FindNearestStandableTile(FactoryLevel level, Point origin, int radius, out Point destination)
        {
            for (int distance = 1; distance <= radius; distance++)
            {
                Point[] preferred =
                [
                    new Point(origin.X, origin.Y - distance),
                    new Point(origin.X + distance, origin.Y),
                    new Point(origin.X, origin.Y + distance),
                    new Point(origin.X - distance, origin.Y)
                ];

                for (int i = 0; i < preferred.Length; i++)
                {
                    if (CanStandAt(level, preferred[i]))
                    {
                        destination = preferred[i];
                        return true;
                    }
                }
            }

            for (int distance = 1; distance <= radius; distance++)
            {
                for (int x = origin.X - distance; x <= origin.X + distance; x++)
                {
                    for (int y = origin.Y - distance; y <= origin.Y + distance; y++)
                    {
                        if (Math.Abs(x - origin.X) != distance && Math.Abs(y - origin.Y) != distance)
                            continue;

                        Point candidate = new(x, y);
                        if (CanStandAt(level, candidate))
                        {
                            destination = candidate;
                            return true;
                        }
                    }
                }
            }

            destination = origin;
            return false;
        }

        private void UpdateVegetation(float dt)
        {
            _treeSpawnTimer += dt;
            if (_treeSpawnTimer >= TreeSpawnTickSeconds)
            {
                _treeSpawnTimer -= TreeSpawnTickSeconds;
                TrySpawnTreeSapling();
            }

            _highGrassSpawnTimer += dt;
            if (_highGrassSpawnTimer >= HighGrassSpawnTickSeconds)
            {
                _highGrassSpawnTimer -= HighGrassSpawnTickSeconds;
                TrySpawnHighGrass();
            }

            foreach (FactoryChunk chunk in EnumerateActiveChunks(FactoryLevel.Surface))
            {
                for (int x = 0; x < FactoryChunk.Size; x++)
                {
                    for (int y = 0; y < FactoryChunk.Size; y++)
                        AdvanceTreeGrowth(chunk.Tiles[x, y], dt);
                }
            }
        }

        private void UpdateNeeds(float dt)
        {
            _foodTickTimer += dt;
            while (_foodTickTimer >= FoodTickSeconds)
            {
                _foodTickTimer -= FoodTickSeconds;
                Player.ReduceFood(1);
            }
        }

        private void UpdateMachines(float dt, FactoryInventory inventory)
        {
            foreach (KeyValuePair<FactoryPlacedObjectKey, FactoryFurnaceState> pair in _furnaces)
                UpdateFurnaceMachine(pair.Key, pair.Value, dt, inventory);

            foreach (KeyValuePair<FactoryPlacedObjectKey, FactoryDrillState> pair in _drills)
                UpdateDrillMachine(pair.Key, pair.Value, dt);
        }

        private void UpdateFurnaceMachine(FactoryPlacedObjectKey key, FactoryFurnaceState furnace, float dt, FactoryInventory inventory)
        {
            if (!ShouldSimulateMachineTile(key.Level, key.Tile))
                return;

            FactoryTile tile = GetTile(key.Level, key.Tile);
            if (tile?.ObjectType != FactoryObjectType.Furnace)
                return;

            if (furnace == null ||
                furnace.FuelSlot.IsEmpty ||
                !FactoryRules.IsFuelResource(furnace.FuelSlot.Resource) ||
                furnace.InputSlot.IsEmpty ||
                !FactoryRules.TryGetSmeltResult(furnace.InputSlot.Resource, out FactoryResource output))
            {
                furnace.WorkProgress = 0f;
                return;
            }

            if (!inventory.CanAdd(output, 1))
                return;

            furnace.WorkProgress += dt;
            while (furnace.WorkProgress >= FactoryFurnaceState.SmeltSeconds)
            {
                if (furnace.FuelSlot.IsEmpty ||
                    !FactoryRules.IsFuelResource(furnace.FuelSlot.Resource) ||
                    furnace.InputSlot.IsEmpty ||
                    !FactoryRules.TryGetSmeltResult(furnace.InputSlot.Resource, out output) ||
                    !inventory.CanAdd(output, 1))
                    break;

                furnace.WorkProgress -= FactoryFurnaceState.SmeltSeconds;
                furnace.FuelSlot.Remove(1);
                furnace.InputSlot.Remove(1);
                inventory.TryAdd(output, 1);
            }

            if (furnace.InputSlot.IsEmpty)
                furnace.WorkProgress = 0f;
        }

        private void UpdateDrillMachine(FactoryPlacedObjectKey key, FactoryDrillState drill, float dt)
        {
            if (!ShouldSimulateMachineTile(key.Level, key.Tile))
                return;

            FactoryTile tile = GetTile(key.Level, key.Tile);
            if (tile?.ObjectType != FactoryObjectType.SolidFuelDrill)
                return;

            if (!tile.HasOre)
            {
                drill.WorkProgress = 0f;
                return;
            }

            FactoryResource reward = FactoryRules.ResourceForOre(tile.Ground);
            if (!CanDrillStoreProduct(drill, reward) || !EnsureDrillFuelCharge(drill))
                return;

            float workSeconds = FactoryRules.GetOreWorkSeconds(tile.Ground);
            drill.WorkProgress += dt;

            while (drill.WorkProgress >= workSeconds && tile.HasOre)
            {
                reward = FactoryRules.ResourceForOre(tile.Ground);
                if (!CanDrillStoreProduct(drill, reward) || !EnsureDrillFuelCharge(drill))
                    break;

                drill.WorkProgress -= workSeconds;
                drill.FuelOreChargesRemaining = Math.Max(0, drill.FuelOreChargesRemaining - 1);
                tile.OreRemaining--;
                StoreDrillProduct(drill, reward);

                if (tile.OreRemaining <= 0)
                {
                    tile.Ground = FactoryRules.DepletedGroundForOre(tile.Ground);
                    tile.OreRemaining = 0;
                    drill.WorkProgress = 0f;
                    break;
                }

                workSeconds = FactoryRules.GetOreWorkSeconds(tile.Ground);
            }
        }

        private bool ShouldSimulateMachineTile(FactoryLevel level, Point tile)
        {
            if (level != Player.Level)
                return false;

            int chunkX = GetChunkX(tile.X);
            int chunkY = GetChunkY(tile.Y);
            if (_hasVisibleChunkWindow)
            {
                return _visibleChunkLevel == level &&
                    chunkX >= _visibleMinChunkX &&
                    chunkX <= _visibleMaxChunkX &&
                    chunkY >= _visibleMinChunkY &&
                    chunkY <= _visibleMaxChunkY;
            }

            Point center = PlayerTile;
            int centerChunkX = GetChunkX(center.X);
            int centerChunkY = GetChunkY(center.Y);
            return Math.Abs(chunkX - centerChunkX) <= ActiveChunkRadius &&
                Math.Abs(chunkY - centerChunkY) <= ActiveChunkRadius;
        }

        private static bool EnsureDrillFuelCharge(FactoryDrillState drill)
        {
            if (drill.FuelOreChargesRemaining > 0)
                return true;

            if (drill.FuelSlot.IsEmpty || !FactoryRules.IsFuelResource(drill.FuelSlot.Resource))
                return false;

            drill.FuelSlot.Remove(1);
            drill.FuelOreChargesRemaining = FactoryRules.DrillOrePerFuel;
            return true;
        }

        private static bool CanDrillStoreProduct(FactoryDrillState drill, FactoryResource reward)
        {
            if (reward == FactoryResource.CoalOre && drill.FuelSlot.CanAccept(reward, 1))
                return true;

            return drill.OutputSlot.CanAccept(reward, 1);
        }

        private static void StoreDrillProduct(FactoryDrillState drill, FactoryResource reward)
        {
            int remaining = 1;
            if (reward == FactoryResource.CoalOre)
                remaining -= drill.FuelSlot.AddUpTo(reward, remaining);

            if (remaining > 0)
                drill.OutputSlot.AddUpTo(reward, remaining);
        }

        private void UpdateCreatures(float dt)
        {
            foreach (FactoryChunk chunk in EnumerateActiveChunks(FactoryLevel.Surface))
            {
                for (int i = chunk.Creatures.Count - 1; i >= 0; i--)
                {
                    FactoryCreature creature = chunk.Creatures[i];
                    Point oldTile = creature.Tile;
                    creature.Update(dt, this);

                    if (creature.CreatureType == FactoryCreatureType.Rabbit)
                    {
                        FactoryTile tile = GetTile(FactoryLevel.Surface, creature.Tile);
                        if (tile?.ObjectType == FactoryObjectType.Snare && !tile.SnareHasCatch)
                        {
                            tile.SnareHasCatch = true;
                            chunk.Creatures.RemoveAt(i);
                            continue;
                        }
                    }

                    Point newTile = creature.Tile;
                    if (GetChunkX(oldTile.X) == GetChunkX(newTile.X) && GetChunkY(oldTile.Y) == GetChunkY(newTile.Y))
                        continue;

                    FactoryChunk destinationChunk = GetOrCreateChunk(FactoryLevel.Surface, GetChunkX(newTile.X), GetChunkY(newTile.Y));
                    destinationChunk.Creatures.Add(creature);
                    chunk.Creatures.RemoveAt(i);
                }
            }

            RebuildVisibleCreatureList();
        }

        private void RebuildVisibleCreatureList()
        {
            _visibleCreatures.Clear();
            if (Player.Level != FactoryLevel.Surface) return;

            foreach (FactoryChunk chunk in EnumerateActiveChunks(FactoryLevel.Surface))
                _visibleCreatures.AddRange(chunk.Creatures);
        }

        private IEnumerable<FactoryChunk> EnumerateActiveChunks(FactoryLevel level)
        {
            if (_hasVisibleChunkWindow && _visibleChunkLevel == level)
            {
                for (int chunkY = _visibleMinChunkY; chunkY <= _visibleMaxChunkY; chunkY++)
                {
                    for (int chunkX = _visibleMinChunkX; chunkX <= _visibleMaxChunkX; chunkX++)
                    {
                        FactoryChunk loadedChunk = TryGetLoadedChunk(level, chunkX, chunkY);
                        if (loadedChunk != null)
                            yield return loadedChunk;
                    }
                }

                yield break;
            }

            Point center = PlayerTile;
            int minChunkX = Math.Max(0, GetChunkX(center.X) - ActiveChunkRadius);
            int maxChunkX = Math.Min(GetChunkX(Width - 1), GetChunkX(center.X) + ActiveChunkRadius);
            int minChunkY = Math.Max(0, GetChunkY(center.Y) - ActiveChunkRadius);
            int maxChunkY = Math.Min(GetChunkY(Height - 1), GetChunkY(center.Y) + ActiveChunkRadius);

            for (int chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
            {
                for (int chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
                {
                    FactoryChunk loadedChunk = TryGetLoadedChunk(level, chunkX, chunkY);
                    if (loadedChunk != null)
                        yield return loadedChunk;
                }
            }
        }

        private FactoryChunk TryGetLoadedChunk(FactoryLevel level, int chunkX, int chunkY)
        {
            _chunks.TryGetValue(new FactoryChunkKey(level, chunkX, chunkY), out FactoryChunk chunk);
            return chunk;
        }

        private FactoryChunk GetOrCreateChunk(FactoryLevel level, int chunkX, int chunkY)
        {
            FactoryChunkKey key = new(level, chunkX, chunkY);
            if (_chunks.TryGetValue(key, out FactoryChunk chunk))
                return chunk;

            chunk = new FactoryChunk(level, chunkX, chunkY);
            GenerateChunk(chunk);
            _chunks[key] = chunk;
            return chunk;
        }

        private void GenerateChunk(FactoryChunk chunk)
        {
            int startX = chunk.ChunkX * FactoryChunk.Size;
            int startY = chunk.ChunkY * FactoryChunk.Size;

            for (int localX = 0; localX < FactoryChunk.Size; localX++)
            {
                for (int localY = 0; localY < FactoryChunk.Size; localY++)
                {
                    int x = startX + localX;
                    int y = startY + localY;
                    chunk.Tiles[localX, localY] = GenerateTile(chunk.Level, x, y);
                }
            }

            if (chunk.Level == FactoryLevel.Surface)
                GenerateChunkCreatures(chunk);
        }

        private FactoryTile GenerateTile(FactoryLevel level, int x, int y)
        {
            FactoryTile tile = new();
            FactorySurfaceBiomeData biome = SampleSurfaceBiome(x, y);
            bool hole = IsSurfaceHole(x, y, biome);

            if (level == FactoryLevel.Surface)
            {
                tile.IsHole = hole;
                if (hole)
                {
                    tile.Ground = FactoryGround.Cave;
                }
                else
                {
                    tile.Ground = GenerateSurfaceGround(x, y, biome, out int oreAmount);
                    tile.OreRemaining = oreAmount;
                }

                ApplySurfaceObject(tile, x, y, biome);
            }
            else
            {
                tile.IsHole = hole;
                if (hole)
                {
                    tile.Ground = FactoryGround.Stone;
                }
                else
                {
                    tile.Ground = GenerateCaveGround(x, y, biome, out int oreAmount);
                    tile.OreRemaining = oreAmount;
                }

                ApplyCaveObject(tile, x, y, biome);
            }

            return tile;
        }

        private FactoryGround GenerateSurfaceGround(int x, int y, FactorySurfaceBiomeData biome, out int oreAmount)
        {
            oreAmount = 0;
            Random random = CreateRandom(x, y, 61);

            if (IsSurfaceWater(x, y, biome))
                return FactoryGround.Water;

            if (IsSurfaceSand(x, y, biome))
            {
                oreAmount = FactoryRules.RollOreValue(random);
                return FactoryGround.Sand;
            }

            if (TrySampleSurfaceOreDeposit(x, y, biome, out FactoryGround depositGround, out oreAmount))
                return depositGround;

            if (biome.Biome == FactoryBiome.Mountain || biome.MountainScore > 0.72f)
            {
                if (Fbm(x, y, 53, 44, 2) > 0.06f)
                    return FactoryGround.Stone;
            }

            if (biome.MountainScore > 0.66f && Next01(x, y, 59) > 0.38f)
                return FactoryGround.Stone;

            return FactoryGround.Grass;
        }

        private FactoryGround GenerateCaveGround(int x, int y, FactorySurfaceBiomeData biome, out int oreAmount)
        {
            oreAmount = 0;
            if (TrySampleCaveOreDeposit(x, y, biome, out FactoryGround depositGround, out oreAmount))
                return depositGround;

            return FactoryGround.Stone;
        }

        private bool TrySampleSurfaceOreDeposit(int x, int y, FactorySurfaceBiomeData biome, out FactoryGround ground, out int oreAmount)
        {
            ground = FactoryGround.Grass;
            oreAmount = 0;

            float mountainAffinity = biome.Biome == FactoryBiome.Mountain
                ? 1f
                : MathHelper.Clamp((biome.MountainScore - 0.66f) / 0.24f, 0f, 1f);
            if (mountainAffinity <= 0f)
                return false;

            const int cellSize = 18;
            int cellX = FloorDiv(x, cellSize);
            int cellY = FloorDiv(y, cellSize);
            FactoryOreDepositSample best = default;

            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                    ConsiderSurfaceOreDepositCandidate(x, y, cellX + offsetX, cellY + offsetY, mountainAffinity, ref best);
            }

            if (!best.IsValid)
                return false;

            ground = best.Ground;
            oreAmount = best.Amount;
            return true;
        }

        private void ConsiderSurfaceOreDepositCandidate(int x, int y, int cellX, int cellY, float mountainAffinity, ref FactoryOreDepositSample best)
        {
            const int cellSize = 18;
            float presence = Next01(cellX, cellY, 601);
            float threshold = 0.86f - mountainAffinity * 0.08f;
            if (presence < threshold)
                return;

            float typeRoll = MathHelper.Clamp(
                Next01(cellX, cellY, 607) + mountainAffinity * 0.05f,
                0f,
                0.9999f);
            FactoryGround ground =
                typeRoll > 0.975f ? FactoryGround.Iron :
                typeRoll > 0.955f ? FactoryGround.Copper :
                typeRoll > 0.93f ? FactoryGround.Coal :
                FactoryGround.StoneOre;

            float sizeRoll = Next01(cellX, cellY, 613);
            float sizeNorm = ground == FactoryGround.StoneOre
                ? MathF.Pow(sizeRoll, 1.8f)
                : MathF.Pow(sizeRoll, 2.4f);
            float radius = ground == FactoryGround.StoneOre
                ? MathHelper.Lerp(2.1f, 5.6f, sizeNorm)
                : MathHelper.Lerp(1.9f, 4.4f, sizeNorm);
            float centerX = cellX * cellSize + 1f + Next01(cellX, cellY, 619) * (cellSize - 2f);
            float centerY = cellY * cellSize + 1f + Next01(cellX, cellY, 631) * (cellSize - 2f);
            float dx = x + 0.5f - centerX;
            float dy = y + 0.5f - centerY;
            float shape = 0.82f + Next01(x + cellX * 13, y + cellY * 17, 643) * 0.32f;
            float effectiveRadius = radius * shape;
            float distance = MathF.Sqrt(dx * dx + dy * dy);
            if (distance > effectiveRadius)
                return;

            float closeness = 1f - distance / effectiveRadius;
            float richnessFactor = 0.86f + presence * 0.24f;
            int peakAmount = ground == FactoryGround.StoneOre
                ? (int)MathF.Round(80f + MathF.Pow(sizeNorm, 2.15f) * 2400f * richnessFactor)
                : (int)MathF.Round(36f + MathF.Pow(sizeNorm, 2.25f) * 1100f * richnessFactor);
            int amount = Math.Max(1, (int)MathF.Round(peakAmount * MathF.Pow(closeness, 1.85f)));
            float weight = closeness * (0.6f + sizeNorm * 0.85f);

            if (!best.IsValid || weight > best.Weight || (MathF.Abs(weight - best.Weight) < 0.001f && amount > best.Amount))
                best = new FactoryOreDepositSample(ground, amount, weight);
        }

        private bool TrySampleCaveOreDeposit(int x, int y, FactorySurfaceBiomeData biome, out FactoryGround ground, out int oreAmount)
        {
            ground = FactoryGround.Stone;
            oreAmount = 0;

            float mountainBonus = biome.Biome == FactoryBiome.Mountain ? 0.1f : 0f;
            float lakeBonus = biome.BaseBiome == FactoryBiome.Lake ? 0.07f : 0f;
            const int cellSize = 24;
            int cellX = FloorDiv(x, cellSize);
            int cellY = FloorDiv(y, cellSize);
            FactoryOreDepositSample best = default;

            for (int offsetX = -1; offsetX <= 1; offsetX++)
            {
                for (int offsetY = -1; offsetY <= 1; offsetY++)
                    ConsiderCaveOreDepositCandidate(x, y, cellX + offsetX, cellY + offsetY, mountainBonus, lakeBonus, ref best);
            }

            if (!best.IsValid)
                return false;

            ground = best.Ground;
            oreAmount = best.Amount;
            return true;
        }

        private void ConsiderCaveOreDepositCandidate(int x, int y, int cellX, int cellY, float mountainBonus, float lakeBonus, ref FactoryOreDepositSample best)
        {
            const int cellSize = 24;
            float presence = Next01(cellX, cellY, 701);
            float threshold = 0.74f - mountainBonus * 0.32f - lakeBonus * 0.22f;
            if (presence < threshold)
                return;

            float typeRoll = MathHelper.Clamp(
                Next01(cellX, cellY, 719) + lakeBonus * 0.22f - mountainBonus * 0.08f,
                0f,
                0.9999f);
            FactoryGround ground =
                typeRoll > 0.82f ? FactoryGround.Iron :
                typeRoll > 0.6f ? FactoryGround.Copper :
                typeRoll > 0.35f ? FactoryGround.Coal :
                FactoryGround.StoneOre;

            float sizeRoll = Next01(cellX, cellY, 733);
            float radiusNorm = MathF.Pow(sizeRoll, 2.15f);
            float richnessNorm = MathF.Pow(sizeRoll, 3.6f);
            float radius = ground == FactoryGround.StoneOre
                ? MathHelper.Lerp(3.4f, 14.2f, radiusNorm)
                : MathHelper.Lerp(2.8f, 11.4f, radiusNorm);
            radius *= 1f + mountainBonus * 0.28f + lakeBonus * 0.12f;

            float centerX = cellX * cellSize + 1f + Next01(cellX, cellY, 751) * (cellSize - 2f);
            float centerY = cellY * cellSize + 1f + Next01(cellX, cellY, 761) * (cellSize - 2f);
            float dx = x + 0.5f - centerX;
            float dy = y + 0.5f - centerY;
            float shape = 0.84f + Next01(x + cellX * 19, y + cellY * 23, 773) * 0.22f;
            float effectiveRadius = radius * shape;
            float distance = MathF.Sqrt(dx * dx + dy * dy);
            if (distance > effectiveRadius)
                return;

            float closeness = 1f - distance / effectiveRadius;
            float richnessFactor = MathHelper.Clamp(0.82f + presence * 0.28f + mountainBonus * 0.52f + lakeBonus * 0.1f, 0.6f, 1.18f);
            int peakAmount = ground switch
            {
                FactoryGround.StoneOre => (int)MathF.Round(120f + richnessNorm * 19880f * richnessFactor),
                FactoryGround.Iron => (int)MathF.Round(90f + richnessNorm * 14800f * richnessFactor),
                FactoryGround.Copper => (int)MathF.Round(80f + richnessNorm * 12400f * richnessFactor),
                FactoryGround.Coal => (int)MathF.Round(95f + richnessNorm * 16000f * richnessFactor),
                _ => 0
            };
            peakAmount = Math.Min(20000, peakAmount);
            int amount = Math.Max(1, (int)MathF.Round(peakAmount * MathF.Pow(closeness, 2.1f)));
            float weight = closeness * (0.68f + radiusNorm * 0.72f);

            if (!best.IsValid || weight > best.Weight || (MathF.Abs(weight - best.Weight) < 0.001f && amount > best.Amount))
                best = new FactoryOreDepositSample(ground, amount, weight);
        }

        private void ApplySurfaceObject(FactoryTile tile, int x, int y, FactorySurfaceBiomeData biome)
        {
            if (tile.IsHole || tile.Ground == FactoryGround.Water)
                return;

            Random random = CreateRandom(x, y, 211);

            if (biome.Biome == FactoryBiome.Mountain)
            {
                float wallHeapNoise = Fbm(x, y, 223, 14, 2);
                if (wallHeapNoise > 0.9f && tile.Ground != FactoryGround.Sand)
                {
                    tile.ObjectType = FactoryObjectType.StoneWall;
                    tile.ObjectResourceAmount = 20;
                    return;
                }

                if (wallHeapNoise > 0.77f)
                {
                    tile.ObjectType = FactoryObjectType.Rock;
                    tile.ObjectResourceAmount = FactoryRules.RollObjectResourceAmount(random);
                    return;
                }
            }

            if (tile.Ground == FactoryGround.Stone && Next01(x, y, 229) > 0.84f)
            {
                tile.ObjectType = FactoryObjectType.Rock;
                tile.ObjectResourceAmount = FactoryRules.RollObjectResourceAmount(random);
                return;
            }

            float treeNoise = Fbm(x, y, 239, 44, 2);
            if (biome.Biome == FactoryBiome.Forest)
            {
                if (tile.Ground != FactoryGround.Grass)
                    return;

                float treePriority = GetForestTreePriority(x, y);

                if (treePriority > 0.64f && IsForestCandidatePeak(x, y, treePriority, 1.75f, 0.58f))
                {
                    tile.ObjectType = FactoryObjectType.Tree;
                    tile.ObjectResourceAmount = FactoryRules.RollObjectResourceAmount(random);
                    return;
                }

                if (treePriority > 0.5f && IsForestCandidatePeak(x, y, treePriority, 1.08f, 0.46f))
                {
                    tile.ObjectType = FactoryObjectType.TreeYoung;
                    tile.ObjectResourceAmount = FactoryRules.RollObjectResourceAmount(random);
                    tile.TreeGrowthSeconds = TreeStageSeconds * (float)(0.25 + random.NextDouble() * 0.5);
                    return;
                }

                if (Next01(x, y, 251) > 0.82f)
                    tile.ObjectType = FactoryObjectType.HighGrass;

                return;
            }

            if (biome.Biome == FactoryBiome.Field)
            {
                if (tile.Ground == FactoryGround.Grass && Next01(x, y, 257) > 0.7f)
                {
                    tile.ObjectType = FactoryObjectType.HighGrass;
                    return;
                }

                if (tile.Ground == FactoryGround.Grass && treeNoise > 0.86f)
                {
                    tile.ObjectType = FactoryObjectType.TreeYoung;
                    tile.ObjectResourceAmount = FactoryRules.RollObjectResourceAmount(random);
                    tile.TreeGrowthSeconds = TreeStageSeconds * (float)(0.35 + random.NextDouble() * 0.4);
                }

                return;
            }

            if (biome.Biome == FactoryBiome.River)
            {
                if (tile.Ground == FactoryGround.Grass && Next01(x, y, 263) > 0.88f)
                    tile.ObjectType = FactoryObjectType.HighGrass;
                return;
            }
        }

        private void ApplyCaveObject(FactoryTile tile, int x, int y, FactorySurfaceBiomeData biome)
        {
            if (tile.IsHole || IsNearSurfaceHole(x, y, 1))
                return;

            bool hasOre = tile.HasOre;
            if (biome.BaseBiome == FactoryBiome.Lake && hasOre && Fbm(x, y, 295, 72, 2) > 0.34f)
            {
                tile.ObjectType = FactoryObjectType.StoneWall;
                tile.ObjectResourceAmount = 20;
                return;
            }

            float wallNoise = Fbm(x, y, 307, 36, 2);
            float wallThreshold = biome.BaseBiome switch
            {
                FactoryBiome.Lake => 0.24f,
                FactoryBiome.Mountain => 0.41f,
                FactoryBiome.Forest => 0.5f,
                FactoryBiome.Field => 0.54f,
                _ => 0.52f
            };

            if (wallNoise > wallThreshold)
            {
                tile.ObjectType = FactoryObjectType.StoneWall;
                tile.ObjectResourceAmount = 20;
                return;
            }

            if (tile.Ground == FactoryGround.Stone && Next01(x, y, 311) > 0.9f)
            {
                tile.ObjectType = FactoryObjectType.Rock;
                tile.ObjectResourceAmount = FactoryRules.RollObjectResourceAmount(CreateRandom(x, y, 313));
            }
        }

        private void GenerateChunkCreatures(FactoryChunk chunk)
        {
            Random random = CreateRandom(chunk.ChunkX, chunk.ChunkY, 401);

            for (int i = 0; i < 6; i++)
                TrySpawnChunkCreature(chunk, FactoryCreatureType.Rabbit, random);

            for (int i = 0; i < 8; i++)
                TrySpawnChunkCreature(chunk, FactoryCreatureType.Fish, random);
        }

        private void TrySpawnChunkCreature(FactoryChunk chunk, FactoryCreatureType creatureType, Random random)
        {
            for (int attempt = 0; attempt < 24; attempt++)
            {
                int localX = random.Next(FactoryChunk.Size);
                int localY = random.Next(FactoryChunk.Size);
                FactoryTile tile = chunk.Tiles[localX, localY];
                Point tilePoint = new(chunk.ChunkX * FactoryChunk.Size + localX, chunk.ChunkY * FactoryChunk.Size + localY);

                if (!InBounds(tilePoint.X, tilePoint.Y)) continue;
                if (Vector2.Distance(TileCenter(tilePoint), Player.Position) < 9f) continue;
                if (chunk.Creatures.Exists(creature => creature.Tile == tilePoint)) continue;

                if (creatureType == FactoryCreatureType.Rabbit)
                {
                    if (tile.IsHole || tile.Ground == FactoryGround.Water || TileObjectBlocksCreatureMovement(tile))
                        continue;

                    FactoryBiome biome = GetSurfaceBiome(tilePoint);
                    if (biome != FactoryBiome.Forest && !(biome == FactoryBiome.Field && Next01(tilePoint.X, tilePoint.Y, 409) > 0.75f))
                        continue;
                }
                else if (tile.Ground != FactoryGround.Water)
                {
                    continue;
                }

                chunk.Creatures.Add(new FactoryCreature(
                    creatureType,
                    TileCenter(tilePoint),
                    new Random(random.Next())
                ));
                return;
            }
        }

        private FactorySurfaceBiomeData SampleSurfaceBiome(int x, int y)
        {
            int warpX = (int)MathF.Round((Fbm(x, y, 1301, 118, 2) - 0.5f) * 58f);
            int warpY = (int)MathF.Round((Fbm(x, y, 1327, 118, 2) - 0.5f) * 58f);
            int sx = x + warpX;
            int sy = y + warpY;

            float lakeScore = (Fbm(sx, sy, 1409, 124, 3) + 0.04f) *
                (0.5f + Fbm(sx, sy, 1417, 54, 2) * 0.46f) +
                Fbm(sx, sy, 1421, 30, 2) * 0.08f;
            float mountainScore = (Fbm(sx, sy, 1423, 118, 3) + 0.03f) *
                (0.54f + Fbm(sx, sy, 1431, 58, 2) * 0.48f) +
                Fbm(sx, sy, 1439, 32, 2) * 0.08f;
            float forestScore = (Fbm(sx, sy, 1451, 110, 3) + 0.09f) *
                (0.58f + Fbm(sx, sy, 1463, 50, 2) * 0.52f) +
                Fbm(sx, sy, 1471, 28, 2) * 0.1f;
            float fieldScore = (Fbm(sx, sy, 1481, 102, 3) + 0.06f) *
                (0.62f + Fbm(sx, sy, 1493, 46, 2) * 0.42f) +
                Fbm(sx, sy, 1501, 26, 2) * 0.08f;

            FactoryBiome baseBiome = FactoryBiome.Field;
            float bestScore = fieldScore;
            if (forestScore > bestScore)
            {
                bestScore = forestScore;
                baseBiome = FactoryBiome.Forest;
            }

            if (mountainScore > bestScore)
            {
                bestScore = mountainScore;
                baseBiome = FactoryBiome.Mountain;
            }

            if (lakeScore > bestScore)
                baseBiome = FactoryBiome.Lake;

            FactoryBiome biome = baseBiome;
            float riverBand = 1f;

            if (baseBiome != FactoryBiome.Lake)
            {
                float riverScale = baseBiome switch
                {
                    FactoryBiome.Mountain => 62f,
                    FactoryBiome.Forest => 78f,
                    _ => 94f
                };
                float warpStrength = baseBiome switch
                {
                    FactoryBiome.Mountain => 20f,
                    FactoryBiome.Forest => 14f,
                    _ => 10f
                };
                float riverWarp = (Fbm(x, y, 1511, 120, 2) - 0.5f) * warpStrength;
                float riverCurve = MathF.Sin((x + riverWarp) / riverScale) +
                    MathF.Cos((y - riverWarp) / (riverScale * 0.8f));
                riverBand = MathF.Abs(riverCurve + (Fbm(x, y, 1543, 84, 2) - 0.5f) * 0.9f);

                float riverThreshold = baseBiome switch
                {
                    FactoryBiome.Mountain => 0.2f,
                    FactoryBiome.Forest => 0.12f,
                    _ => 0.1f
                };

                if (riverBand < riverThreshold)
                    biome = FactoryBiome.River;
            }

            return new FactorySurfaceBiomeData(
                baseBiome,
                biome,
                lakeScore,
                mountainScore,
                forestScore,
                fieldScore,
                riverBand);
        }

        private bool IsSurfaceWater(int x, int y, FactorySurfaceBiomeData biome)
        {
            if (biome.Biome == FactoryBiome.River)
            {
                float waterThreshold = biome.BaseBiome == FactoryBiome.Mountain ? 0.13f : 0.09f;
                return biome.RiverBand < waterThreshold;
            }

            if (biome.Biome != FactoryBiome.Lake)
                return false;

            float rival = Math.Max(biome.MountainScore, Math.Max(biome.ForestScore, biome.FieldScore));
            float depth = biome.LakeScore - rival;
            return depth > -0.01f || Fbm(x, y, 1597, 40, 2) > 0.57f;
        }

        private bool IsSurfaceSand(int x, int y, FactorySurfaceBiomeData biome)
        {
            if (IsSurfaceWater(x, y, biome))
                return false;

            int waterRadius = biome.Biome == FactoryBiome.Lake ? 2 : 1;
            return IsAdjacentToSurfaceWater(x, y, waterRadius);
        }

        private void ClearStartingArea(int centerX, int centerY, int radius)
        {
            FactorySurfaceBiomeData startBiome = SampleSurfaceBiome(centerX, centerY);
            bool lakeStart = startBiome.Biome == FactoryBiome.Lake || startBiome.BaseBiome == FactoryBiome.Lake;
            float safeRadius = lakeStart ? radius + 0.45f : Math.Max(2.6f, radius - 1.65f);
            float transitionRadius = lakeStart ? radius + 0.85f : safeRadius + 0.9f;
            float caveSafeRadius = Math.Max(2.4f, safeRadius - 0.65f);
            Point? starterTreeTile = null;
            float starterTreeScore = float.MaxValue;

            for (int x = Math.Max(0, centerX - radius); x <= Math.Min(Width - 1, centerX + radius); x++)
            {
                for (int y = Math.Max(0, centerY - radius); y <= Math.Min(Height - 1, centerY + radius); y++)
                {
                    int dx = x - centerX;
                    int dy = y - centerY;
                    float distance = MathF.Sqrt(dx * dx + dy * dy);
                    if (distance > radius + 0.9f)
                        continue;

                    Point tilePoint = new(x, y);
                    FactoryTile surface = GetTile(FactoryLevel.Surface, tilePoint);
                    FactoryTile cave = GetTile(FactoryLevel.Cave, tilePoint);

                    if (distance <= caveSafeRadius)
                    {
                        cave.Ground = FactoryGround.Stone;
                        cave.Floor = FactoryFloor.Empty;
                        cave.IsHole = false;
                        cave.OreRemaining = 0;
                        cave.ClearObject();
                    }

                    if (lakeStart)
                    {
                        surface.Floor = FactoryFloor.Empty;
                        surface.IsHole = false;
                        surface.OreRemaining = 0;
                        surface.ClearObject();
                        surface.Ground = distance <= safeRadius - 1.35f
                            ? FactoryGround.Grass
                            : FactoryGround.Sand;

                        if (distance >= Math.Max(2f, safeRadius - 2.3f) && distance <= safeRadius - 0.2f)
                        {
                            float score = MathF.Abs(distance - (safeRadius - 1.1f));
                            if (score < starterTreeScore)
                            {
                                starterTreeScore = score;
                                starterTreeTile = tilePoint;
                            }
                        }

                        continue;
                    }

                    if (distance <= safeRadius)
                    {
                        surface.Ground = GetStartGroundForBiome(startBiome);
                        surface.Floor = FactoryFloor.Empty;
                        surface.IsHole = false;
                        surface.OreRemaining = 0;
                        surface.ClearObject();
                    }
                    else if (distance <= transitionRadius)
                    {
                        if (surface.IsHole)
                            surface.IsHole = false;

                        if (surface.Ground == FactoryGround.Water)
                        {
                            surface.Ground = startBiome.Biome == FactoryBiome.Mountain
                                ? FactoryGround.Stone
                                : FactoryGround.Grass;
                            surface.OreRemaining = 0;
                        }

                        if (TileObjectBlocksCreatureMovement(surface))
                            surface.ClearObject();
                    }
                }
            }

            if (lakeStart)
                PlaceStarterTreeOnIsland(starterTreeTile ?? new Point(centerX, centerY), centerX, centerY);
        }

        private FactoryGround GetStartGroundForBiome(FactorySurfaceBiomeData biome) =>
            biome.Biome == FactoryBiome.Mountain || biome.MountainScore > 0.68f
                ? FactoryGround.Stone
                : FactoryGround.Grass;

        private void PlaceStarterTreeOnIsland(Point preferredTile, int centerX, int centerY)
        {
            Point[] candidates =
            [
                preferredTile,
                new Point(centerX, Math.Max(0, centerY - 2)),
                new Point(Math.Min(Width - 1, centerX + 2), centerY),
                new Point(centerX, Math.Min(Height - 1, centerY + 2)),
                new Point(Math.Max(0, centerX - 2), centerY),
                new Point(Math.Min(Width - 1, centerX + 1), Math.Max(0, centerY - 2)),
                new Point(Math.Max(0, centerX - 1), Math.Min(Height - 1, centerY + 2))
            ];

            for (int i = 0; i < candidates.Length; i++)
            {
                Point tilePoint = candidates[i];
                if (!InBounds(tilePoint.X, tilePoint.Y))
                    continue;

                FactoryTile tile = GetTile(FactoryLevel.Surface, tilePoint);
                if (tile == null || tile.IsHole || tile.Ground != FactoryGround.Grass)
                    continue;

                tile.Ground = FactoryGround.Grass;
                tile.Floor = FactoryFloor.Empty;
                tile.OreRemaining = 0;
                tile.ClearObject();
                tile.ObjectType = FactoryObjectType.Tree;
                tile.ObjectResourceAmount = Math.Max(
                    FactoryRules.BoatWoodCost,
                    FactoryRules.RollObjectResourceAmount(CreateRandom(tilePoint.X, tilePoint.Y, 1889)));
                return;
            }
        }

        private void RemoveCreaturesNear(Vector2 position, float radius)
        {
            Point center = new((int)Math.Floor(position.X), (int)Math.Floor(position.Y));
            int minChunkX = Math.Max(0, GetChunkX(center.X) - 1);
            int maxChunkX = Math.Min(GetChunkX(Width - 1), GetChunkX(center.X) + 1);
            int minChunkY = Math.Max(0, GetChunkY(center.Y) - 1);
            int maxChunkY = Math.Min(GetChunkY(Height - 1), GetChunkY(center.Y) + 1);

            for (int chunkY = minChunkY; chunkY <= maxChunkY; chunkY++)
            {
                for (int chunkX = minChunkX; chunkX <= maxChunkX; chunkX++)
                {
                    FactoryChunk chunk = TryGetLoadedChunk(FactoryLevel.Surface, chunkX, chunkY);
                    if (chunk == null) continue;
                    chunk.Creatures.RemoveAll(creature => Vector2.Distance(creature.Position, position) < radius);
                }
            }
        }

        private void TrySpawnTreeSapling()
        {
            foreach (FactoryChunk chunk in EnumerateActiveChunks(FactoryLevel.Surface))
            {
                Random random = CreateRandom(chunk.ChunkX, chunk.ChunkY, PlayerTile.X ^ PlayerTile.Y ^ (int)_treeSpawnTimer);
                for (int attempt = 0; attempt < 8; attempt++)
                {
                    int localX = random.Next(FactoryChunk.Size);
                    int localY = random.Next(FactoryChunk.Size);
                    Point tilePoint = new(chunk.ChunkX * FactoryChunk.Size + localX, chunk.ChunkY * FactoryChunk.Size + localY);
                    FactoryTile tile = chunk.Tiles[localX, localY];

                    if (tile.IsHole || tile.Floor != FactoryFloor.Empty) continue;
                    if (tile.Ground != FactoryGround.Grass) continue;
                    if (tile.ObjectType != FactoryObjectType.Empty) continue;
                    if (Vector2.Distance(Player.Position, TileCenter(tilePoint)) < 8f) continue;

                    FactorySurfaceBiomeData biome = SampleSurfaceBiome(tilePoint.X, tilePoint.Y);
                    if (HasNearbyTreeTooClose(tilePoint.X, tilePoint.Y, 2))
                        continue;

                    float treeInfluence = GetNearbyTreeInfluence(tilePoint.X, tilePoint.Y, 7);
                    if (treeInfluence <= 0.01f) continue;

                    float biomeMultiplier = biome.Biome switch
                    {
                        FactoryBiome.Forest => 1.15f,
                        FactoryBiome.Field => 0.72f,
                        FactoryBiome.River => 0.56f,
                        FactoryBiome.Mountain => 0.32f,
                        FactoryBiome.Lake => 0.18f,
                        _ => 0.5f
                    };

                    double spawnChance = Math.Min(treeInfluence * biomeMultiplier, 0.045f);
                    if (random.NextDouble() > spawnChance) continue;

                    tile.ObjectType = FactoryObjectType.TreeSapling;
                    tile.ObjectResourceAmount = FactoryRules.RollObjectResourceAmount(random);
                    tile.TreeGrowthSeconds = 0;
                    return;
                }
            }
        }

        private void TrySpawnHighGrass()
        {
            foreach (FactoryChunk chunk in EnumerateActiveChunks(FactoryLevel.Surface))
            {
                Random random = CreateRandom(chunk.ChunkX, chunk.ChunkY, PlayerTile.X + PlayerTile.Y + (int)_highGrassSpawnTimer);
                for (int attempt = 0; attempt < 10; attempt++)
                {
                    int localX = random.Next(FactoryChunk.Size);
                    int localY = random.Next(FactoryChunk.Size);
                    Point tilePoint = new(chunk.ChunkX * FactoryChunk.Size + localX, chunk.ChunkY * FactoryChunk.Size + localY);
                    FactoryTile tile = chunk.Tiles[localX, localY];

                    if (tile.IsHole || tile.Floor != FactoryFloor.Empty) continue;
                    if (tile.Ground != FactoryGround.Grass) continue;
                    if (tile.ObjectType != FactoryObjectType.Empty) continue;
                    if (Vector2.Distance(Player.Position, TileCenter(tilePoint)) < 6f) continue;

                    FactorySurfaceBiomeData biome = SampleSurfaceBiome(tilePoint.X, tilePoint.Y);
                    double spawnChance = biome.Biome switch
                    {
                        FactoryBiome.Field => 0.34,
                        FactoryBiome.Forest => 0.16,
                        FactoryBiome.River => 0.24,
                        _ => 0.08
                    };
                    if (random.NextDouble() > spawnChance) continue;

                    tile.ObjectType = FactoryObjectType.HighGrass;
                    return;
                }
            }
        }

        private float GetNearbyTreeInfluence(int centerX, int centerY, int radius)
        {
            float influence = 0f;
            int radiusSquared = radius * radius;

            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (!InBounds(x, y)) continue;

                    int dx = x - centerX;
                    int dy = y - centerY;
                    if (dx == 0 && dy == 0) continue;
                    if (dx * dx + dy * dy > radiusSquared) continue;

                    FactoryTile tile = TryGetLoadedTile(FactoryLevel.Surface, new Point(x, y));
                    if (tile == null) continue;

                    FactoryObjectType objectType = tile.ObjectType;
                    if (objectType is FactoryObjectType.Tree or FactoryObjectType.TreeYoung)
                    {
                        float distance = MathF.Sqrt(dx * dx + dy * dy);
                        influence += distance switch
                        {
                            <= 1.5f => 0.008f,
                            <= 2.5f => 0.02f,
                            <= 3.6f => 0.07f,
                            <= 4.6f => 0.07f,
                            <= 5.6f => 0.038f,
                            <= 6.6f => 0.018f,
                            <= 7.5f => 0.01f,
                            _ => 0f
                        };
                    }
                }
            }

            return influence;
        }

        private bool HasNearbyTreeTooClose(int centerX, int centerY, int radius)
        {
            int radiusSquared = radius * radius;

            for (int x = centerX - radius; x <= centerX + radius; x++)
            {
                for (int y = centerY - radius; y <= centerY + radius; y++)
                {
                    if (!InBounds(x, y)) continue;

                    int dx = x - centerX;
                    int dy = y - centerY;
                    if (dx == 0 && dy == 0) continue;
                    if (dx * dx + dy * dy > radiusSquared) continue;

                    FactoryTile tile = TryGetLoadedTile(FactoryLevel.Surface, new Point(x, y));
                    if (tile == null) continue;

                    if (tile.ObjectType is FactoryObjectType.Tree or FactoryObjectType.TreeYoung or FactoryObjectType.TreeSapling)
                        return true;
                }
            }

            return false;
        }

        private float GetForestTreePriority(int x, int y)
        {
            float canopy = Fbm(x, y, 239, 28, 3);
            float scatter = Next01(x, y, 271);
            float detail = Fbm(x, y, 283, 14, 2);
            return canopy * 0.44f + scatter * 0.38f + detail * 0.18f;
        }

        private bool IsForestCandidatePeak(int x, int y, float currentPriority, float radius, float minNeighborPriority)
        {
            Vector2 currentPoint = GetForestCandidatePoint(x, y);
            int searchRadius = (int)MathF.Ceiling(radius);
            float radiusSquared = radius * radius;
            float currentTieBreaker = Next01(x, y, 317);

            for (int offsetX = -searchRadius; offsetX <= searchRadius; offsetX++)
            {
                for (int offsetY = -searchRadius; offsetY <= searchRadius; offsetY++)
                {
                    if (offsetX == 0 && offsetY == 0)
                        continue;

                    int nx = x + offsetX;
                    int ny = y + offsetY;
                    if (!InBounds(nx, ny))
                        continue;

                    float neighborPriority = GetForestTreePriority(nx, ny);
                    if (neighborPriority < minNeighborPriority)
                        continue;

                    Vector2 neighborPoint = GetForestCandidatePoint(nx, ny);
                    if (Vector2.DistanceSquared(currentPoint, neighborPoint) > radiusSquared)
                        continue;

                    if (neighborPriority > currentPriority + 0.0001f)
                        return false;

                    if (MathF.Abs(neighborPriority - currentPriority) <= 0.0001f &&
                        Next01(nx, ny, 317) > currentTieBreaker)
                        return false;
                }
            }

            return true;
        }

        private Vector2 GetForestCandidatePoint(int x, int y) =>
            new(
                x + 0.16f + Next01(x, y, 293) * 0.68f,
                y + 0.16f + Next01(x, y, 307) * 0.68f
            );

        private static void AdvanceTreeGrowth(FactoryTile tile, float dt)
        {
            if (tile.ObjectType is not (FactoryObjectType.TreeSapling or FactoryObjectType.TreeYoung)) return;

            tile.TreeGrowthSeconds += dt;
            if (tile.TreeGrowthSeconds < TreeStageSeconds) return;

            tile.TreeGrowthSeconds = 0;
            tile.ObjectType = tile.ObjectType == FactoryObjectType.TreeSapling
                ? FactoryObjectType.TreeYoung
                : FactoryObjectType.Tree;
        }

        private FactoryChunkSaveData[] ExportChunks(FactoryLevel level)
        {
            return _chunks
                .Where(pair => pair.Key.Level == level)
                .OrderBy(pair => pair.Key.ChunkY)
                .ThenBy(pair => pair.Key.ChunkX)
                .Select(pair =>
                {
                    FactoryChunk chunk = pair.Value;
                    FactoryTileSaveData[] tiles = new FactoryTileSaveData[FactoryChunk.Size * FactoryChunk.Size];
                    int index = 0;
                    for (int y = 0; y < FactoryChunk.Size; y++)
                    {
                        for (int x = 0; x < FactoryChunk.Size; x++, index++)
                            tiles[index] = FactoryTileSaveData.FromTile(chunk.Tiles[x, y]);
                    }

                    return new FactoryChunkSaveData
                    {
                        ChunkX = chunk.ChunkX,
                        ChunkY = chunk.ChunkY,
                        Tiles = tiles,
                        Creatures = chunk.Creatures.Select(creature => new FactoryCreatureSaveData
                        {
                            Type = creature.CreatureType,
                            X = creature.Position.X,
                            Y = creature.Position.Y
                        }).ToArray()
                    };
                })
                .ToArray();
        }

        private void ImportChunks(FactoryLevel level, FactoryChunkSaveData[] saves)
        {
            if (saves == null) return;

            for (int i = 0; i < saves.Length; i++)
            {
                FactoryChunkSaveData save = saves[i];
                FactoryChunk chunk = new(level, save.ChunkX, save.ChunkY);
                int index = 0;
                for (int y = 0; y < FactoryChunk.Size; y++)
                {
                    for (int x = 0; x < FactoryChunk.Size; x++, index++)
                        chunk.Tiles[x, y] = save.Tiles[index].ToTile();
                }

                if (save.Creatures != null)
                {
                    for (int c = 0; c < save.Creatures.Length; c++)
                    {
                        FactoryCreatureSaveData creatureSave = save.Creatures[c];
                        chunk.Creatures.Add(new FactoryCreature(
                            creatureSave.Type,
                            new Vector2(creatureSave.X, creatureSave.Y),
                            CreateRandom(save.ChunkX, save.ChunkY, c + 777)
                        ));
                    }
                }

                _chunks[new FactoryChunkKey(level, save.ChunkX, save.ChunkY)] = chunk;
            }
        }

        private bool IsSurfaceHole(int x, int y, FactorySurfaceBiomeData biome)
        {
            if (biome.Biome is FactoryBiome.Lake or FactoryBiome.River || biome.BaseBiome == FactoryBiome.Lake)
                return false;

            float mountainAffinity = biome.Biome == FactoryBiome.Mountain
                ? 1f
                : Math.Max(0f, biome.MountainScore - Math.Max(biome.ForestScore, biome.FieldScore) * 0.35f);
            if (mountainAffinity < 0.5f)
                return false;

            int cellX = x / 2;
            int cellY = y / 2;
            float holeNoise = Fbm(cellX, cellY, 503, 10, 3);
            float shapeNoise = Fbm(cellX, cellY, 521, 6, 2);
            float breakupNoise = Fbm(cellX, cellY, 547, 4, 2);
            float threshold = mountainAffinity > 0.86f
                ? 0.64f
                : mountainAffinity > 0.72f
                    ? 0.72f
                    : 0.8f;

            if (shapeNoise < 0.5f || breakupNoise < 0.46f || holeNoise <= threshold)
                return false;

            if (IsAdjacentToSurfaceWater(x, y, 1))
                return false;

            return true;
        }

        private bool IsNearSurfaceHole(int x, int y, int radius)
        {
            for (int rx = x - radius; rx <= x + radius; rx++)
            {
                for (int ry = y - radius; ry <= y + radius; ry++)
                {
                    if (!InBounds(rx, ry)) continue;
                    if (IsSurfaceHole(rx, ry, SampleSurfaceBiome(rx, ry))) return true;
                }
            }

            return false;
        }

        private bool IsAdjacentToSurfaceWater(int x, int y, int radius)
        {
            for (int rx = x - radius; rx <= x + radius; rx++)
            {
                for (int ry = y - radius; ry <= y + radius; ry++)
                {
                    if ((rx == x && ry == y) || !InBounds(rx, ry))
                        continue;

                    if (IsSurfaceWater(rx, ry, SampleSurfaceBiome(rx, ry)))
                        return true;
                }
            }

            return false;
        }

        private float Fbm(int x, int y, int salt, int scale, int octaves)
        {
            float total = 0f;
            float amplitude = 1f;
            float amplitudeTotal = 0f;
            int currentScale = scale;

            for (int octave = 0; octave < octaves; octave++)
            {
                total += ValueNoise(x, y, Math.Max(2, currentScale), _seed + salt + octave * 997) * amplitude;
                amplitudeTotal += amplitude;
                amplitude *= 0.5f;
                currentScale /= 2;
            }

            return amplitudeTotal <= 0f ? 0f : total / amplitudeTotal;
        }

        private float ValueNoise(int x, int y, int scale, int seed)
        {
            int cellX = FloorDiv(x, scale);
            int cellY = FloorDiv(y, scale);
            float fx = (x - cellX * scale) / (float)scale;
            float fy = (y - cellY * scale) / (float)scale;

            float v00 = Next01(cellX, cellY, seed);
            float v10 = Next01(cellX + 1, cellY, seed);
            float v01 = Next01(cellX, cellY + 1, seed);
            float v11 = Next01(cellX + 1, cellY + 1, seed);

            float sx = SmoothStep(fx);
            float sy = SmoothStep(fy);
            float top = MathHelper.Lerp(v00, v10, sx);
            float bottom = MathHelper.Lerp(v01, v11, sx);
            return MathHelper.Lerp(top, bottom, sy);
        }

        private static float SmoothStep(float value) =>
            value * value * (3f - 2f * value);

        private float Next01(int x, int y, int salt)
        {
            uint hash = (uint)(x * 374761393) ^ (uint)(y * 668265263) ^ (uint)(_seed + salt * 1447);
            hash ^= hash >> 13;
            hash *= 1274126177u;
            hash ^= hash >> 16;
            return (hash & 0x00FFFFFF) / 16777215f;
        }

        private Random CreateRandom(int x, int y, int salt)
        {
            int seed = unchecked((int)(
                (uint)(x * 73856093) ^
                (uint)(y * 19349663) ^
                (uint)(_seed + salt * 83492791)));
            return new Random(seed);
        }

        private static int FloorDiv(int value, int divisor)
        {
            int result = value / divisor;
            if (value < 0 && value % divisor != 0)
                result--;
            return result;
        }

        private static int GetChunkX(int x) =>
            x / FactoryChunk.Size;

        private static int GetChunkY(int y) =>
            y / FactoryChunk.Size;

        private static int GetLocalX(int x) =>
            x % FactoryChunk.Size;

        private static int GetLocalY(int y) =>
            y % FactoryChunk.Size;

        private FactoryTile TryGetLoadedTile(FactoryLevel level, Point tile)
        {
            if (!InBounds(tile.X, tile.Y)) return null;

            FactoryChunk chunk = TryGetLoadedChunk(level, GetChunkX(tile.X), GetChunkY(tile.Y));
            return chunk?.Tiles[GetLocalX(tile.X), GetLocalY(tile.Y)];
        }
    }
}
