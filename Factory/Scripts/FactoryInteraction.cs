using System;
using Microsoft.Xna.Framework;

namespace AkiGames.Scripts
{
    internal sealed class FactoryInteraction
    {
        public const float RockWorkSeconds = 0.95f;
        public const float HighGrassWorkSeconds = 0.3f;
        public const float TreeWorkSeconds = 1.45f;
        public const float StoneWallWorkSeconds = 1.2f;
        public const float FurnaceWorkSeconds = 1.15f;
        public const float SolidFuelDrillWorkSeconds = 1.2f;
        public const float WoodWallWorkSeconds = 0.85f;
        public const float DoorWorkSeconds = 0.7f;
        public const float SnareWorkSeconds = 0.55f;
        public const float LadderWorkSeconds = 0.45f;
        public const float BoatWorkSeconds = 0.55f;
        public const float FloorWorkSeconds = 0.35f;

        public Point ActiveTile { get; private set; } = new(-1, -1);
        public float Progress { get; private set; }
        public string StatusText { get; private set; } = "WASD move. LMB place or use. Hold RMB to dig.";

        public void Clear()
        {
            ActiveTile = new Point(-1, -1);
            Progress = 0;
        }

        public void SetStatus(string statusText)
        {
            StatusText = statusText ?? "";
        }

        public void Update(
            FactoryWorld world,
            FactoryInventory inventory,
            Point? targetTile,
            float interactionRadius,
            float dt
        )
        {
            if (targetTile == null)
            {
                Clear();
                return;
            }

            Point tilePosition = targetTile.Value;
            FactoryTile tile = world.GetTile(tilePosition);
            if (tile == null)
            {
                Clear();
                return;
            }

            if (Vector2.Distance(world.Player.Position, FactoryWorld.TileCenter(tilePosition)) > interactionRadius)
            {
                Clear();
                StatusText = "Target is too far away.";
                return;
            }

            if (ActiveTile != tilePosition)
            {
                ActiveTile = tilePosition;
                Progress = 0;
            }

            if (TryWorkBoat(world, tilePosition, inventory, dt)) return;
            if (TryWorkObject(world, tilePosition, tile, inventory, dt)) return;
            if (TryWorkFloor(tile, inventory, dt)) return;
            if (TryWorkOre(tile, inventory, dt)) return;

            StatusText = DescribeTile(world, tilePosition, tile);
            Progress = 0;
        }

        public float GetRequiredWorkSeconds(FactoryTile tile)
        {
            if (tile == null) return 0;

            return tile.ObjectType switch
            {
                FactoryObjectType.Rock => RockWorkSeconds,
                FactoryObjectType.HighGrass => HighGrassWorkSeconds,
                FactoryObjectType.TreeSapling => 0.55f,
                FactoryObjectType.TreeYoung => 0.95f,
                FactoryObjectType.Tree => TreeWorkSeconds,
                FactoryObjectType.Furnace => FurnaceWorkSeconds,
                FactoryObjectType.SolidFuelDrill => SolidFuelDrillWorkSeconds,
                FactoryObjectType.StoneWall => StoneWallWorkSeconds,
                FactoryObjectType.WoodWall => WoodWallWorkSeconds,
                FactoryObjectType.WoodDoor => DoorWorkSeconds,
                FactoryObjectType.Snare => SnareWorkSeconds,
                FactoryObjectType.Ladder => LadderWorkSeconds,
                _ when tile.Floor == FactoryFloor.Wood => FloorWorkSeconds,
                _ when tile.HasOre => FactoryRules.GetOreWorkSeconds(tile.Ground),
                _ => 0
            };
        }

        private bool TryWorkBoat(FactoryWorld world, Point tilePosition, FactoryInventory inventory, float dt)
        {
            if (!world.IsBoatAt(tilePosition))
                return false;

            if (world.Player.IsOnBoat && world.Player.Level == FactoryLevel.Surface && world.PlayerTile == tilePosition)
            {
                StatusText = "Press Shift to leave the boat first.";
                Progress = 0;
                return true;
            }

            if (!inventory.CanAdd(FactoryResource.Boat, 1))
            {
                StatusText = "Inventory is full.";
                Progress = 0;
                return true;
            }

            StatusText = "Pulling boat in...";
            Progress += dt;
            if (Progress < BoatWorkSeconds) return true;

            inventory.TryAdd(FactoryResource.Boat, 1);
            world.RemoveBoat(tilePosition);
            Clear();
            StatusText = "Boat recovered.";
            return true;
        }

        private bool TryWorkObject(FactoryWorld world, Point tilePosition, FactoryTile tile, FactoryInventory inventory, float dt)
        {
            switch (tile.ObjectType)
            {
                case FactoryObjectType.Rock:
                    return TryCollectObject(tile, inventory, FactoryResource.Stone, tile.ObjectResourceAmount, RockWorkSeconds, "Digging rock", dt);

                case FactoryObjectType.HighGrass:
                    return TryCollectObject(tile, inventory, FactoryResource.HighGrass, 1, HighGrassWorkSeconds, "Cutting high grass", dt);

                case FactoryObjectType.Tree:
                    return TryCollectTree(tile, inventory, dt, 1f, 1f, "Cutting tree");

                case FactoryObjectType.TreeSapling:
                    return TryCollectTree(tile, inventory, dt, 0.25f, 0f, "Cutting sapling");

                case FactoryObjectType.TreeYoung:
                    return TryCollectTree(tile, inventory, dt, 0.5f, 0.4f, "Cutting young tree");

                case FactoryObjectType.StoneWall:
                    return TryCollectObject(tile, inventory, FactoryResource.Stone, 20, StoneWallWorkSeconds, "Digging stone wall", dt);

                case FactoryObjectType.Furnace:
                    return TryRecoverFurnace(world, tilePosition, tile, inventory, dt);

                case FactoryObjectType.SolidFuelDrill:
                    return TryRecoverSolidFuelDrill(world, tilePosition, inventory, dt);

                case FactoryObjectType.WoodWall:
                    return TryRecoverPlacedItem(tile, inventory, FactoryResource.WoodWall, WoodWallWorkSeconds, "Removing wood wall", dt);

                case FactoryObjectType.WoodDoor:
                    return TryRecoverPlacedItem(tile, inventory, FactoryResource.WoodDoor, DoorWorkSeconds, "Removing wood door", dt);

                case FactoryObjectType.Snare:
                    return TryRecoverSnare(tile, inventory, dt);

                case FactoryObjectType.Ladder:
                    if (!inventory.CanAdd(FactoryResource.Ladder, 1))
                    {
                        StatusText = "Inventory is full.";
                        Progress = 0;
                        return true;
                    }

                    StatusText = "Pulling up ladder...";
                    Progress += dt;
                    if (Progress >= LadderWorkSeconds)
                    {
                        inventory.TryAdd(FactoryResource.Ladder, 1);
                        world.RemoveLadder(tilePosition);
                        Clear();
                        StatusText = "Ladder recovered.";
                    }
                    return true;
            }

            return false;
        }

        private bool TryRecoverFurnace(FactoryWorld world, Point tilePosition, FactoryTile tile, FactoryInventory inventory, float dt)
        {
            if (!inventory.CanAdd(FactoryResource.Furnace, 1))
            {
                StatusText = "Inventory is full.";
                Progress = 0;
                return true;
            }

            FactoryFurnaceState furnace = world.GetFurnace(world.Player.Level, tilePosition);
            if (furnace != null &&
                (!furnace.FuelSlot.IsEmpty || !furnace.InputSlot.IsEmpty))
            {
                StatusText = "Take items out of the furnace first.";
                Progress = 0;
                return true;
            }

            StatusText = "Removing furnace...";
            Progress += dt;
            if (Progress < FurnaceWorkSeconds) return true;

            inventory.TryAdd(FactoryResource.Furnace, 1);
            world.RemoveFurnace(world.Player.Level, tilePosition);
            Clear();
            StatusText = "Furnace recovered.";
            return true;
        }

        private bool TryRecoverSolidFuelDrill(FactoryWorld world, Point tilePosition, FactoryInventory inventory, float dt)
        {
            if (!inventory.CanAdd(FactoryResource.SolidFuelDrill, 1))
            {
                StatusText = "Inventory is full.";
                Progress = 0;
                return true;
            }

            FactoryDrillState drill = world.GetSolidFuelDrill(world.Player.Level, tilePosition);
            if (drill != null &&
                (!drill.FuelSlot.IsEmpty || !drill.OutputSlot.IsEmpty))
            {
                StatusText = "Take items out of the drill first.";
                Progress = 0;
                return true;
            }

            StatusText = "Removing drill...";
            Progress += dt;
            if (Progress < SolidFuelDrillWorkSeconds) return true;

            inventory.TryAdd(FactoryResource.SolidFuelDrill, 1);
            world.RemoveSolidFuelDrill(world.Player.Level, tilePosition);
            Clear();
            StatusText = "Solid fuel drill recovered.";
            return true;
        }

        private bool TryWorkFloor(FactoryTile tile, FactoryInventory inventory, float dt)
        {
            if (tile.Floor != FactoryFloor.Wood) return false;

            if (!inventory.CanAdd(FactoryResource.WoodFlooring, 1))
            {
                StatusText = "Inventory is full.";
                Progress = 0;
                return true;
            }

            StatusText = "Lifting wood flooring...";
            Progress += dt;
            if (Progress >= FloorWorkSeconds)
            {
                inventory.TryAdd(FactoryResource.WoodFlooring, 1);
                tile.Floor = FactoryFloor.Empty;
                Clear();
                StatusText = "Wood flooring recovered.";
            }

            return true;
        }

        private bool TryCollectTree(
            FactoryTile tile,
            FactoryInventory inventory,
            float dt,
            float woodScale,
            float stickScale,
            string actionText)
        {
            int fullWoodAmount = Math.Max(1, tile.ObjectResourceAmount);
            int woodAmount = Math.Max(1, (int)Math.Ceiling(fullWoodAmount * woodScale));
            int fullStickAmount = FactoryRules.TreeStickAmount(fullWoodAmount);
            int stickAmount = stickScale <= 0f
                ? 0
                : Math.Max(1, (int)Math.Ceiling(fullStickAmount * stickScale));
            float workSeconds = tile.ObjectType switch
            {
                FactoryObjectType.TreeSapling => 0.55f,
                FactoryObjectType.TreeYoung => 0.95f,
                _ => TreeWorkSeconds
            };

            if (!inventory.CanAdd(FactoryResource.Wood, woodAmount) ||
                (stickAmount > 0 && !inventory.CanAdd(FactoryResource.Stick, stickAmount)))
            {
                StatusText = "Inventory is full.";
                Progress = 0;
                return true;
            }

            StatusText = stickAmount > 0
                ? $"{actionText} ({woodAmount} wood, {stickAmount} stick)..."
                : $"{actionText} ({woodAmount} wood)...";
            Progress += dt;
            if (Progress < workSeconds) return true;

            inventory.TryAdd(FactoryResource.Wood, woodAmount);
            if (stickAmount > 0)
                inventory.TryAdd(FactoryResource.Stick, stickAmount);
            tile.ClearObject();
            Clear();
            StatusText = stickAmount > 0
                ? $"{woodAmount} wood and {stickAmount} stick collected."
                : $"{woodAmount} wood collected.";
            return true;
        }

        private bool TryRecoverSnare(FactoryTile tile, FactoryInventory inventory, float dt)
        {
            bool hasCatch = tile.SnareHasCatch;
            if (hasCatch)
            {
                if (!inventory.CanAdd(FactoryResource.Stick, 1) || !inventory.CanAdd(FactoryResource.RabbitMeat, 1))
                {
                    StatusText = "Inventory is full.";
                    Progress = 0;
                    return true;
                }
            }
            else if (!inventory.CanAdd(FactoryResource.Snare, 1))
            {
                StatusText = "Inventory is full.";
                Progress = 0;
                return true;
            }

            StatusText = hasCatch ? "Collecting snared rabbit..." : "Lifting snare...";
            Progress += dt;
            if (Progress < SnareWorkSeconds) return true;

            if (hasCatch)
            {
                inventory.TryAdd(FactoryResource.Stick, 1);
                inventory.TryAdd(FactoryResource.RabbitMeat, 1);
                StatusText = "Rabbit meat and a stick collected.";
            }
            else
            {
                inventory.TryAdd(FactoryResource.Snare, 1);
                StatusText = "Snare recovered.";
            }

            tile.ClearObject();
            Clear();
            return true;
        }

        private bool TryWorkOre(FactoryTile tile, FactoryInventory inventory, float dt)
        {
            if (!tile.HasOre) return false;

            FactoryResource reward = FactoryRules.ResourceForOre(tile.Ground);
            if (!inventory.CanAdd(reward, 1))
            {
                StatusText = "Inventory is full.";
                Progress = 0;
                return true;
            }

            string oreName = FactoryRules.GroundName(tile.Ground);
            float workSeconds = FactoryRules.GetOreWorkSeconds(tile.Ground);
            StatusText = $"Mining {oreName} ({tile.OreRemaining} left)...";
            Progress += dt;

            if (Progress < workSeconds) return true;

            Progress -= workSeconds;
            tile.OreRemaining--;
            inventory.TryAdd(reward, 1);

            if (tile.OreRemaining <= 0)
            {
                tile.Ground = FactoryRules.DepletedGroundForOre(tile.Ground);
                tile.OreRemaining = 0;
                Clear();
                StatusText = $"{oreName} deposit is empty.";
            }

            return true;
        }

        private bool TryCollectObject(
            FactoryTile tile,
            FactoryInventory inventory,
            FactoryResource reward,
            int amount,
            float workSeconds,
            string actionText,
            float dt
        )
        {
            if (!inventory.CanAdd(reward, amount))
            {
                StatusText = "Inventory is full.";
                Progress = 0;
                return true;
            }

            StatusText = $"{actionText} ({amount})...";
            Progress += dt;
            if (Progress < workSeconds) return true;

            inventory.TryAdd(reward, amount);
            tile.ClearObject();
            Clear();
            StatusText = $"{amount} {FactoryRules.ResourceName(reward).ToLowerInvariant()} collected.";
            return true;
        }

        private bool TryRecoverPlacedItem(
            FactoryTile tile,
            FactoryInventory inventory,
            FactoryResource reward,
            float workSeconds,
            string actionText,
            float dt
        )
        {
            if (!inventory.CanAdd(reward, 1))
            {
                StatusText = "Inventory is full.";
                Progress = 0;
                return true;
            }

            StatusText = $"{actionText}...";
            Progress += dt;
            if (Progress < workSeconds) return true;

            inventory.TryAdd(reward, 1);
            tile.ClearObject();
            Clear();
            StatusText = $"{FactoryRules.ResourceName(reward)} recovered.";
            return true;
        }

        private static string DescribeTile(FactoryWorld world, Point tilePosition, FactoryTile tile)
        {
            if (tile.ObjectType != FactoryObjectType.Empty)
            {
                if (tile.ObjectType == FactoryObjectType.WoodDoor)
                    return tile.DoorOpen ? "Open wood door. Left-click to close." : "Closed wood door. Left-click to open.";

                if (tile.ObjectType == FactoryObjectType.Snare)
                    return tile.SnareHasCatch ? "Snare with a rabbit." : "Snare.";

                if (tile.ObjectType == FactoryObjectType.Furnace)
                    return "Furnace. Left-click to open.";

                if (tile.ObjectType == FactoryObjectType.SolidFuelDrill)
                    return "Solid fuel drill. Left-click to open.";

                return FactoryRules.ObjectName(tile.ObjectType);
            }

            if (tile.Floor == FactoryFloor.Wood)
                return "Wood flooring.";

            if (tile.IsHole)
                return world.IsLadderAt(tilePosition)
                    ? "Ladder hole. Left-click to travel."
                    : "Hole. Place a ladder here.";

            if (tile.Ground == FactoryGround.Water)
                return world.CanPlaceBoat(tilePosition)
                    ? "Water. Place a boat here."
                    : "Water blocks movement.";

            if (tile.Ground == FactoryGround.Grass)
                return "Grass. Trees and tall grass may grow here later.";

            return FactoryRules.GroundName(tile.Ground);
        }
    }
}
