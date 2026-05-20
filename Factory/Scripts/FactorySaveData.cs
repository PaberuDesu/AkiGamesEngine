namespace AkiGames.Scripts
{
    internal sealed class FactorySaveData
    {
        public FactoryWorldSaveData World { get; set; }
        public FactoryInventorySlotSaveData[] InventorySlots { get; set; }
        public int SelectedHotbarSlot { get; set; }
    }

    internal sealed class FactoryWorldSaveData
    {
        public int Seed { get; set; }
        public float PlayerX { get; set; }
        public float PlayerY { get; set; }
        public FactoryLevel Level { get; set; }
        public bool IsOnBoat { get; set; }
        public int HealthPoints { get; set; }
        public int FoodPoints { get; set; }
        public float FoodTickSecondsRemaining { get; set; }
        public FactoryChunkSaveData[] SurfaceChunks { get; set; }
        public FactoryChunkSaveData[] CaveChunks { get; set; }
        public FactoryBoatSaveData[] Boats { get; set; }
        public FactoryFurnaceSaveData[] Furnaces { get; set; }
        public FactoryDrillSaveData[] Drills { get; set; }
        public string SurfaceDiscovery { get; set; }
        public string CaveDiscovery { get; set; }
    }

    internal sealed class FactoryChunkSaveData
    {
        public int ChunkX { get; set; }
        public int ChunkY { get; set; }
        public FactoryTileSaveData[] Tiles { get; set; }
        public FactoryCreatureSaveData[] Creatures { get; set; }
    }

    internal sealed class FactoryCreatureSaveData
    {
        public FactoryCreatureType Type { get; set; }
        public float X { get; set; }
        public float Y { get; set; }
    }

    internal sealed class FactoryBoatSaveData
    {
        public float X { get; set; }
        public float Y { get; set; }
    }

    internal sealed class FactoryFurnaceSaveData
    {
        public FactoryLevel Level { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public FactoryInventorySlotSaveData Fuel { get; set; }
        public FactoryInventorySlotSaveData Input { get; set; }
        public float WorkProgress { get; set; }
    }

    internal sealed class FactoryDrillSaveData
    {
        public FactoryLevel Level { get; set; }
        public int X { get; set; }
        public int Y { get; set; }
        public FactoryInventorySlotSaveData Fuel { get; set; }
        public FactoryInventorySlotSaveData Output { get; set; }
        public float WorkProgress { get; set; }
        public int FuelOreChargesRemaining { get; set; }
    }

    internal sealed class FactoryTileSaveData
    {
        public FactoryGround Ground { get; set; }
        public FactoryFloor Floor { get; set; }
        public FactoryObjectType ObjectType { get; set; }
        public int OreRemaining { get; set; }
        public int ObjectResourceAmount { get; set; }
        public float TreeGrowthSeconds { get; set; }
        public bool IsHole { get; set; }
        public bool IsPlayerPlacedObject { get; set; }
        public bool DoorOpen { get; set; }
        public bool SnareHasCatch { get; set; }

        public static FactoryTileSaveData FromTile(FactoryTile tile) =>
            new()
            {
                Ground = tile.Ground,
                Floor = tile.Floor,
                ObjectType = tile.ObjectType,
                OreRemaining = tile.OreRemaining,
                ObjectResourceAmount = tile.ObjectResourceAmount,
                TreeGrowthSeconds = tile.TreeGrowthSeconds,
                IsHole = tile.IsHole,
                IsPlayerPlacedObject = tile.IsPlayerPlacedObject,
                DoorOpen = tile.DoorOpen,
                SnareHasCatch = tile.SnareHasCatch
            };

        public FactoryTile ToTile() =>
            new()
            {
                Ground = Ground,
                Floor = Floor,
                ObjectType = ObjectType,
                OreRemaining = OreRemaining,
                ObjectResourceAmount = ObjectResourceAmount,
                TreeGrowthSeconds = TreeGrowthSeconds,
                IsHole = IsHole,
                IsPlayerPlacedObject = IsPlayerPlacedObject,
                DoorOpen = DoorOpen,
                SnareHasCatch = SnareHasCatch
            };
    }

    internal sealed class FactoryInventorySlotSaveData
    {
        public FactoryResource Resource { get; set; }
        public int Count { get; set; }

        public static FactoryInventorySlotSaveData FromSlot(FactoryInventorySlot slot) =>
            slot == null || slot.IsEmpty
                ? new FactoryInventorySlotSaveData()
                : new FactoryInventorySlotSaveData
                {
                    Resource = slot.Resource,
                    Count = slot.Count
                };

        public void ApplyTo(FactoryInventorySlot slot)
        {
            if (slot == null) return;

            if (Count <= 0)
                slot.Clear();
            else
                slot.Set(Resource, Count);
        }
    }
}
