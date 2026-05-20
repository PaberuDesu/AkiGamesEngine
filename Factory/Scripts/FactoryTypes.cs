using System;

namespace AkiGames.Scripts
{
    internal enum FactoryGround
    {
        Grass,
        Stone,
        Cave,
        Water,
        Sand,
        StoneOre,
        Iron,
        Copper,
        Coal
    }

    internal enum FactoryBiome
    {
        Field,
        Forest,
        Mountain,
        Lake,
        River
    }

    internal enum FactoryFloor
    {
        Empty,
        Wood
    }

    internal enum FactoryObjectType
    {
        Empty,
        Rock,
        HighGrass,
        TreeSapling,
        TreeYoung,
        Tree,
        Ladder,
        Boat,
        Furnace,
        StoneWall,
        WoodWall,
        WoodDoor,
        Snare,
        SolidFuelDrill
    }

    internal enum FactoryResource
    {
        Stone,
        Wood,
        Stick,
        HighGrass,
        Rope,
        Sand,
        IronOre,
        CopperOre,
        CoalOre,
        Ladder,
        Boat,
        Furnace,
        WoodFlooring,
        WoodWall,
        StoneWall,
        WoodDoor,
        FishingRod,
        Snare,
        RabbitMeat,
        Iron,
        Copper,
        SolidFuelDrill
    }

    internal enum FactoryLevel
    {
        Surface,
        Cave
    }

    internal enum FactoryCreatureType
    {
        Rabbit,
        Fish
    }

    internal static class FactoryRules
    {
        public const int MaxStackSize = 255;
        public const int LadderWoodCost = 7;
        public const int BoatWoodCost = 15;
        public const int FurnaceStoneCost = 15;
        public const int WoodFlooringWoodCost = 10;
        public const int WoodWallWoodCost = 20;
        public const int StoneWallStoneCost = 20;
        public const int WoodDoorWoodCost = 8;
        public const int RopeHighGrassCost = 3;
        public const int FishingRodRopeCost = 5;
        public const int FishingRodStickCost = 3;
        public const int SnareRopeCost = 3;
        public const int SnareStickCost = 1;
        public const int SolidFuelDrillIronCost = 5;
        public const int SolidFuelDrillCopperCost = 3;
        public const int DrillOrePerFuel = 5;

        public static bool IsOre(FactoryGround ground) =>
            ground is FactoryGround.Sand or FactoryGround.StoneOre or FactoryGround.Iron or FactoryGround.Copper or FactoryGround.Coal;

        public static bool IsBlocking(FactoryObjectType objectType) =>
            objectType is FactoryObjectType.Rock or FactoryObjectType.TreeSapling or
                FactoryObjectType.TreeYoung or FactoryObjectType.Tree or
                FactoryObjectType.Furnace or FactoryObjectType.SolidFuelDrill or
                FactoryObjectType.StoneWall or FactoryObjectType.WoodWall;

        public static bool IsFuelResource(FactoryResource resource) =>
            resource == FactoryResource.CoalOre;

        public static bool TryGetSmeltResult(FactoryResource resource, out FactoryResource result)
        {
            switch (resource)
            {
                case FactoryResource.IronOre:
                    result = FactoryResource.Iron;
                    return true;
                case FactoryResource.CopperOre:
                    result = FactoryResource.Copper;
                    return true;
                default:
                    result = default;
                    return false;
            }
        }

        public static FactoryResource ResourceForOre(FactoryGround ground) =>
            ground switch
            {
                FactoryGround.Sand => FactoryResource.Sand,
                FactoryGround.Iron => FactoryResource.IronOre,
                FactoryGround.Copper => FactoryResource.CopperOre,
                FactoryGround.Coal => FactoryResource.CoalOre,
                _ => FactoryResource.Stone
            };

        public static FactoryGround DepletedGroundForOre(FactoryGround ground) =>
            ground == FactoryGround.Sand ? FactoryGround.Grass : FactoryGround.Stone;

        public static string GroundName(FactoryGround ground) =>
            ground switch
            {
                FactoryGround.Grass => "Grass",
                FactoryGround.Stone => "Stone",
                FactoryGround.Cave => "Hole",
                FactoryGround.Water => "Water",
                FactoryGround.Sand => "Sand",
                FactoryGround.StoneOre => "Stone ore",
                FactoryGround.Iron => "Iron",
                FactoryGround.Copper => "Copper",
                FactoryGround.Coal => "Coal",
                _ => "Unknown"
            };

        public static string FloorName(FactoryFloor floor) =>
            floor switch
            {
                FactoryFloor.Wood => "Wood flooring",
                _ => ""
            };

        public static string ObjectName(FactoryObjectType objectType) =>
            objectType switch
            {
                FactoryObjectType.Rock => "Rock",
                FactoryObjectType.HighGrass => "High grass",
                FactoryObjectType.TreeSapling => "Sapling",
                FactoryObjectType.TreeYoung => "Young tree",
                FactoryObjectType.Tree => "Tree",
                FactoryObjectType.Ladder => "Ladder",
                FactoryObjectType.Boat => "Boat",
                FactoryObjectType.Furnace => "Furnace",
                FactoryObjectType.SolidFuelDrill => "Solid fuel drill",
                FactoryObjectType.StoneWall => "Stone wall",
                FactoryObjectType.WoodWall => "Wood wall",
                FactoryObjectType.WoodDoor => "Wood door",
                FactoryObjectType.Snare => "Snare",
                _ => ""
            };

        public static string ResourceCostName(FactoryResource resource) =>
            resource switch
            {
                FactoryResource.HighGrass => "grass",
                FactoryResource.Iron => "iron",
                FactoryResource.Copper => "copper",
                FactoryResource.IronOre => "iron",
                FactoryResource.CopperOre => "copper",
                FactoryResource.CoalOre => "coal",
                _ => ResourceName(resource).ToLowerInvariant()
            };

        public static string ResourceName(FactoryResource resource) =>
            resource switch
            {
                FactoryResource.Stone => "Stone",
                FactoryResource.Wood => "Wood",
                FactoryResource.Stick => "Stick",
                FactoryResource.HighGrass => "High grass",
                FactoryResource.Rope => "Rope",
                FactoryResource.Sand => "Sand",
                FactoryResource.Iron => "Iron",
                FactoryResource.Copper => "Copper",
                FactoryResource.IronOre => "Iron ore",
                FactoryResource.CopperOre => "Copper ore",
                FactoryResource.CoalOre => "Coal",
                FactoryResource.Ladder => "Ladder",
                FactoryResource.Boat => "Boat",
                FactoryResource.Furnace => "Furnace",
                FactoryResource.SolidFuelDrill => "Solid fuel drill",
                FactoryResource.WoodFlooring => "Wood flooring",
                FactoryResource.WoodWall => "Wood wall",
                FactoryResource.StoneWall => "Stone wall",
                FactoryResource.WoodDoor => "Wood door",
                FactoryResource.FishingRod => "Fishing rod",
                FactoryResource.Snare => "Snare",
                FactoryResource.RabbitMeat => "Rabbit meat",
                _ => "Unknown"
            };

        public static string CreatureName(FactoryCreatureType creatureType) =>
            creatureType switch
            {
                FactoryCreatureType.Rabbit => "Rabbit",
                FactoryCreatureType.Fish => "Fish",
                _ => "Creature"
            };

        public static string BiomeName(FactoryBiome biome) =>
            biome switch
            {
                FactoryBiome.Field => "Field",
                FactoryBiome.Forest => "Forest",
                FactoryBiome.Mountain => "Mountain",
                FactoryBiome.Lake => "Lake",
                FactoryBiome.River => "River",
                _ => "Unknown"
            };

        public static int RollObjectResourceAmount(Random random)
        {
            double roll = random.NextDouble();
            if (roll < 0.5) return 7;
            if (roll < 0.82) return random.Next(5, 10);
            if (roll < 0.95) return random.Next(10, 15);
            return random.Next(15, 21);
        }

        public static int RollOreValue(Random random)
        {
            double roll = random.NextDouble();
            if (roll < 0.45) return 7;
            if (roll < 0.85) return random.Next(5, 11);
            return random.Next(11, 21);
        }

        public static int RollCaveWallStoneAmount(Random random)
        {
            return 20;
        }

        public static int TreeStickAmount(int woodAmount) =>
            Math.Max(1, woodAmount / 5);

        public static float GetOreWorkSeconds(FactoryGround ground) =>
            ground switch
            {
                FactoryGround.Sand => 0.48f,
                FactoryGround.StoneOre => 1.05f,
                FactoryGround.Coal => 1.3f,
                FactoryGround.Copper => 1.6f,
                FactoryGround.Iron => 1.95f,
                _ => 1.2f
            };

        public static int SeedFromText(string seedText)
        {
            if (string.IsNullOrWhiteSpace(seedText))
                return Environment.TickCount;

            if (int.TryParse(seedText.Trim(), out int parsed))
                return parsed;

            unchecked
            {
                int hash = 17;
                string normalized = seedText.Trim();
                for (int i = 0; i < normalized.Length; i++)
                    hash = hash * 31 + normalized[i];

                return hash;
            }
        }

        public static string ResourceDescription(FactoryResource resource) =>
            resource switch
            {
                FactoryResource.Stone => "Basic building material. Used for walls and furnaces.",
                FactoryResource.Wood => "Cut from mature trees. Used in many early crafts.",
                FactoryResource.Stick => "A light wooden stick. Handy for tools and traps.",
                FactoryResource.HighGrass => "Tall grass that can be twisted into rope.",
                FactoryResource.Rope => "Flexible plant rope for tools, traps, and fishing gear.",
                FactoryResource.Sand => "Loose sand dug from shorelines and water edges.",
                FactoryResource.Iron => "Refined iron ready for tougher machines.",
                FactoryResource.Copper => "Refined copper used in sturdier parts.",
                FactoryResource.IronOre => "Heavy iron-bearing ore from the cave.",
                FactoryResource.CopperOre => "Copper ore from cave veins.",
                FactoryResource.CoalOre => "Fuel for furnaces and solid fuel drills.",
                FactoryResource.Ladder => "Place on a hole to travel between surface and cave.",
                FactoryResource.Boat => "Place on water, ride with LMB, leave with Shift.",
                FactoryResource.Furnace => "Place it on dry ground to smelt iron ore and copper ore with coal.",
                FactoryResource.SolidFuelDrill => "Place it on an ore tile. Coal powers it and mined ore stays inside.",
                FactoryResource.WoodFlooring => "Placed on the middle floor layer of a dry tile.",
                FactoryResource.WoodWall => "A blocking wooden wall.",
                FactoryResource.StoneWall => "A blocking stone wall. Digging one returns 20 stone.",
                FactoryResource.WoodDoor => "Place it on dry ground and open or close it with LMB.",
                FactoryResource.FishingRod => "A simple rod. It does not do anything yet.",
                FactoryResource.Snare => "Place it on land to catch rabbits that walk into it.",
                FactoryResource.RabbitMeat => "Fresh meat from a trapped rabbit.",
                _ => "Item"
            };
    }
}
