namespace AkiGames.Scripts
{
    internal static class FactoryTextureCatalog
    {
        public const string GrassGroundTexture = "Content/Textures/Ground/grass.png";
        public const string StoneGroundTexture = "Content/Textures/Ground/stone.png";
        public const string CaveGroundTexture = "Content/Textures/Ground/cave.png";
        public const string WaterGroundTexture = "Content/Textures/Ground/water.png";
        public const string SandGroundTexture = "Content/Textures/Ground/sand.png";
        public const string StoneOreGroundTexture = "Content/Textures/Ground/stone_ore.png";
        public const string IronGroundTexture = "Content/Textures/Ground/iron_ore.png";
        public const string CopperGroundTexture = "Content/Textures/Ground/copper_ore.png";
        public const string CoalGroundTexture = "Content/Textures/Ground/coal_ore.png";

        public const string PlayerTexture = "Content/Textures/Entities/player.png";
        public const string RabbitTexture = "Content/Textures/Entities/rabbit.png";
        public const string FishTexture = "Content/Textures/Entities/fish.png";

        public const string RockTexture = "Content/Textures/Objects/rock.png";
        public const string HighGrassTexture = "Content/Textures/Objects/high_grass.png";
        public const string TreeSaplingTexture = "Content/Textures/Objects/tree_sapling.png";
        public const string TreeYoungTexture = "Content/Textures/Objects/tree_young.png";
        public const string TreeTexture = "Content/Textures/Objects/tree.png";
        public const string LadderTexture = "Content/Textures/Objects/ladder.png";
        public const string BoatTexture = "Content/Textures/Objects/boat.png";
        public const string FurnaceTexture = "Content/Textures/Objects/furnace.png";
        public const string SolidFuelDrillTexture = "Content/Textures/Objects/solid_fuel_drill.png";
        public const string StoneWallTexture = "Content/Textures/Objects/stone_wall.png";
        public const string WoodWallTexture = "Content/Textures/Objects/wood_wall.png";
        public const string WoodDoorClosedTexture = "Content/Textures/Objects/wood_door_closed.png";
        public const string WoodDoorOpenTexture = "Content/Textures/Objects/wood_door_open.png";
        public const string SnareTexture = "Content/Textures/Objects/snare.png";
        public const string SnareCaughtTexture = "Content/Textures/Objects/snare_caught.png";
        public const string WoodFloorTexture = "Content/Textures/Objects/wood_floor.png";

        public const string StoneItemTexture = "Content/Textures/Items/stone.png";
        public const string WoodItemTexture = "Content/Textures/Items/wood.png";
        public const string StickItemTexture = "Content/Textures/Items/stick.png";
        public const string RopeItemTexture = "Content/Textures/Items/rope.png";
        public const string SandItemTexture = "Content/Textures/Items/sand.png";
        public const string IronItemTexture = "Content/Textures/Items/iron.png";
        public const string CopperItemTexture = "Content/Textures/Items/copper.png";
        public const string IronOreItemTexture = "Content/Textures/Items/iron_ore.png";
        public const string CopperOreItemTexture = "Content/Textures/Items/copper_ore.png";
        public const string CoalOreItemTexture = "Content/Textures/Items/coal_ore.png";
        public const string FishingRodItemTexture = "Content/Textures/Items/fishing_rod.png";
        public const string RabbitMeatItemTexture = "Content/Textures/Items/rabbit_meat.png";

        public static string GetGroundTexture(FactoryGround ground) =>
            ground switch
            {
                FactoryGround.Grass => GrassGroundTexture,
                FactoryGround.Stone => StoneGroundTexture,
                FactoryGround.Cave => CaveGroundTexture,
                FactoryGround.Water => WaterGroundTexture,
                FactoryGround.Sand => SandGroundTexture,
                FactoryGround.StoneOre => StoneOreGroundTexture,
                FactoryGround.Iron => IronGroundTexture,
                FactoryGround.Copper => CopperGroundTexture,
                FactoryGround.Coal => CoalGroundTexture,
                _ => null
            };

        public static string GetFloorTexture(FactoryFloor floor) =>
            floor == FactoryFloor.Wood ? WoodFloorTexture : null;

        public static string GetObjectTexture(FactoryTile tile)
        {
            if (tile == null) return null;

            return tile.ObjectType switch
            {
                FactoryObjectType.Rock => RockTexture,
                FactoryObjectType.HighGrass => HighGrassTexture,
                FactoryObjectType.TreeSapling => TreeSaplingTexture,
                FactoryObjectType.TreeYoung => TreeYoungTexture,
                FactoryObjectType.Tree => TreeTexture,
                FactoryObjectType.Ladder => LadderTexture,
                FactoryObjectType.Boat => BoatTexture,
                FactoryObjectType.Furnace => FurnaceTexture,
                FactoryObjectType.SolidFuelDrill => SolidFuelDrillTexture,
                FactoryObjectType.StoneWall => StoneWallTexture,
                FactoryObjectType.WoodWall => WoodWallTexture,
                FactoryObjectType.WoodDoor => tile.DoorOpen ? WoodDoorOpenTexture : WoodDoorClosedTexture,
                FactoryObjectType.Snare => tile.SnareHasCatch ? SnareCaughtTexture : SnareTexture,
                _ => null
            };
        }

        public static string GetCreatureTexture(FactoryCreatureType creatureType) =>
            creatureType switch
            {
                FactoryCreatureType.Rabbit => RabbitTexture,
                FactoryCreatureType.Fish => FishTexture,
                _ => null
            };

        public static string GetResourceTexture(FactoryResource resource) =>
            resource switch
            {
                FactoryResource.Stone => StoneItemTexture,
                FactoryResource.Wood => WoodItemTexture,
                FactoryResource.Stick => StickItemTexture,
                FactoryResource.HighGrass => HighGrassTexture,
                FactoryResource.Rope => RopeItemTexture,
                FactoryResource.Sand => SandItemTexture,
                FactoryResource.Iron => IronItemTexture,
                FactoryResource.Copper => CopperItemTexture,
                FactoryResource.IronOre => IronOreItemTexture,
                FactoryResource.CopperOre => CopperOreItemTexture,
                FactoryResource.CoalOre => CoalOreItemTexture,
                FactoryResource.Ladder => LadderTexture,
                FactoryResource.Boat => BoatTexture,
                FactoryResource.Furnace => FurnaceTexture,
                FactoryResource.SolidFuelDrill => SolidFuelDrillTexture,
                FactoryResource.WoodFlooring => WoodFloorTexture,
                FactoryResource.WoodWall => WoodWallTexture,
                FactoryResource.StoneWall => StoneWallTexture,
                FactoryResource.WoodDoor => WoodDoorClosedTexture,
                FactoryResource.FishingRod => FishingRodItemTexture,
                FactoryResource.Snare => SnareTexture,
                FactoryResource.RabbitMeat => RabbitMeatItemTexture,
                _ => null
            };
    }
}
