using System;
using Microsoft.Xna.Framework;

namespace AkiGames.Scripts
{
    internal sealed class FactoryTile
    {
        public FactoryGround Ground { get; set; } = FactoryGround.Grass;
        public FactoryFloor Floor { get; set; } = FactoryFloor.Empty;
        public FactoryObjectType ObjectType { get; set; } = FactoryObjectType.Empty;
        public int OreRemaining { get; set; }
        public int ObjectResourceAmount { get; set; }
        public float TreeGrowthSeconds { get; set; }
        public bool IsHole { get; set; }
        public bool IsPlayerPlacedObject { get; set; }
        public bool DoorOpen { get; set; }
        public bool SnareHasCatch { get; set; }

        public bool BlocksMovement =>
            Ground == FactoryGround.Water ||
            FactoryRules.IsBlocking(ObjectType) ||
            (ObjectType == FactoryObjectType.WoodDoor && !DoorOpen);

        public bool HasOre =>
            FactoryRules.IsOre(Ground) && OreRemaining > 0;

        public void ClearObject()
        {
            ObjectType = FactoryObjectType.Empty;
            ObjectResourceAmount = 0;
            TreeGrowthSeconds = 0;
            IsPlayerPlacedObject = false;
            DoorOpen = false;
            SnareHasCatch = false;
        }
    }

    internal sealed class FactoryPlayer
    {
        public const int MaxHealthPoints = 10;
        public const int MaxFoodPoints = 10;

        public Vector2 Position { get; private set; }
        public FactoryLevel Level { get; private set; } = FactoryLevel.Surface;
        public bool IsOnBoat { get; private set; }
        public int HealthPoints { get; private set; } = MaxHealthPoints;
        public int FoodPoints { get; private set; } = MaxFoodPoints;

        public FactoryPlayer(Vector2 startPosition)
        {
            Position = startPosition;
        }

        public void Move(Vector2 direction, float speed, float dt, FactoryWorld world)
        {
            if (direction == Vector2.Zero) return;

            direction.Normalize();
            Vector2 delta = direction * speed * dt;
            bool allowWater = IsOnBoat;
            Point ignoreTile = world.PlayerTile;

            Vector2 nextX = new(Position.X + delta.X, Position.Y);
            if (world.CanStandAt(nextX, allowWater, IsOnBoat ? ignoreTile : null))
                Position = nextX;

            ignoreTile = world.PlayerTile;
            Vector2 nextY = new(Position.X, Position.Y + delta.Y);
            if (world.CanStandAt(nextY, allowWater, IsOnBoat ? ignoreTile : null))
                Position = nextY;
        }

        public void SetPosition(Vector2 position) =>
            Position = position;

        public void SetBoatMounted(bool mounted) =>
            IsOnBoat = mounted;

        public void MoveToLevel(FactoryLevel level)
        {
            Level = level;
            if (level != FactoryLevel.Surface)
                IsOnBoat = false;
        }

        public void LoadState(Vector2 position, FactoryLevel level, bool isOnBoat, int healthPoints, int foodPoints)
        {
            Position = position;
            Level = level;
            IsOnBoat = isOnBoat && level == FactoryLevel.Surface;
            HealthPoints = Math.Clamp(healthPoints, 0, MaxHealthPoints);
            FoodPoints = Math.Clamp(foodPoints, 0, MaxFoodPoints);
        }

        public void ReduceFood(int amount)
        {
            if (amount <= 0) return;
            FoodPoints = Math.Max(0, FoodPoints - amount);
        }

        public bool TryRestoreFood(int amount)
        {
            if (amount <= 0 || FoodPoints >= MaxFoodPoints)
                return false;

            FoodPoints = Math.Min(MaxFoodPoints, FoodPoints + amount);
            return true;
        }
    }
}
