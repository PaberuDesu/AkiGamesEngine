using System;
using System.Text;

namespace AkiGames.Scripts
{
    internal readonly struct FactoryIngredient
    {
        public FactoryIngredient(FactoryResource resource, int count)
        {
            Resource = resource;
            Count = Math.Max(1, count);
        }

        public FactoryResource Resource { get; }
        public int Count { get; }
    }

    internal sealed class FactoryCraftRecipe
    {
        public FactoryCraftRecipe(string buttonName, FactoryResource result, string description, params FactoryIngredient[] ingredients)
        {
            ButtonName = buttonName;
            Result = result;
            Description = description;
            Ingredients = ingredients ?? [];
        }

        public string ButtonName { get; }
        public FactoryResource Result { get; }
        public string Description { get; }
        public FactoryIngredient[] Ingredients { get; }
        public string DisplayName => FactoryRules.ResourceName(Result);
        public string CostLabel => BuildCostLabel(multiline: false);
        public string ButtonCostLabel => BuildCostLabel(multiline: Ingredients.Length > 1);

        private string BuildCostLabel(bool multiline)
        {
            if (Ingredients.Length == 0) return "";

            StringBuilder builder = new();
            for (int i = 0; i < Ingredients.Length; i++)
            {
                if (i > 0)
                    builder.Append(multiline ? "\n" : ", ");

                FactoryIngredient ingredient = Ingredients[i];
                builder.Append(ingredient.Count);
                builder.Append(' ');
                builder.Append(FactoryRules.ResourceCostName(ingredient.Resource));
            }

            return builder.ToString();
        }
    }

    internal static class FactoryCrafting
    {
        public static FactoryCraftRecipe[] Recipes { get; } =
        [
            new FactoryCraftRecipe(
                "CraftButtonLadder",
                FactoryResource.Ladder,
                "Place on a surface hole to link the overworld and cave. Click the ladder again to travel.",
                new FactoryIngredient(FactoryResource.Wood, FactoryRules.LadderWoodCost)
            ),
            new FactoryCraftRecipe(
                "CraftButtonBoat",
                FactoryResource.Boat,
                "Place on empty water. Left-click the boat to ride it and press Shift to step back onto land.",
                new FactoryIngredient(FactoryResource.Wood, FactoryRules.BoatWoodCost)
            ),
            new FactoryCraftRecipe(
                "CraftButtonFurnace",
                FactoryResource.Furnace,
                "Place on a dry tile. Coal powers it, and smelted iron or copper goes straight into your inventory.",
                new FactoryIngredient(FactoryResource.Stone, FactoryRules.FurnaceStoneCost)
            ),
            new FactoryCraftRecipe(
                "CraftButtonSolidFuelDrill",
                FactoryResource.SolidFuelDrill,
                "Place on an ore tile. Coal powers it for five mined ore, and mined coal refills fuel before storage.",
                new FactoryIngredient(FactoryResource.Iron, FactoryRules.SolidFuelDrillIronCost),
                new FactoryIngredient(FactoryResource.Copper, FactoryRules.SolidFuelDrillCopperCost)
            ),
            new FactoryCraftRecipe(
                "CraftButtonWoodFlooring",
                FactoryResource.WoodFlooring,
                "Places a wood floor on the middle layer of an empty dry tile.",
                new FactoryIngredient(FactoryResource.Wood, FactoryRules.WoodFlooringWoodCost)
            ),
            new FactoryCraftRecipe(
                "CraftButtonWoodWall",
                FactoryResource.WoodWall,
                "Places a full blocking wood wall on an empty dry tile.",
                new FactoryIngredient(FactoryResource.Wood, FactoryRules.WoodWallWoodCost)
            ),
            new FactoryCraftRecipe(
                "CraftButtonStoneWall",
                FactoryResource.StoneWall,
                "Places a solid stone wall. Natural cave walls use the same material and reveal the ground behind them when dug.",
                new FactoryIngredient(FactoryResource.Stone, FactoryRules.StoneWallStoneCost)
            ),
            new FactoryCraftRecipe(
                "CraftButtonWoodDoor",
                FactoryResource.WoodDoor,
                "Places a wooden door on an empty dry tile. Left-click it to open or close.",
                new FactoryIngredient(FactoryResource.Wood, FactoryRules.WoodDoorWoodCost)
            ),
            new FactoryCraftRecipe(
                "CraftButtonRope",
                FactoryResource.Rope,
                "Twists high grass into rope.",
                new FactoryIngredient(FactoryResource.HighGrass, FactoryRules.RopeHighGrassCost)
            ),
            new FactoryCraftRecipe(
                "CraftButtonFishingRod",
                FactoryResource.FishingRod,
                "A simple fishing rod. It does not do anything yet, but it is ready for future work.",
                new FactoryIngredient(FactoryResource.Rope, FactoryRules.FishingRodRopeCost),
                new FactoryIngredient(FactoryResource.Stick, FactoryRules.FishingRodStickCost)
            ),
            new FactoryCraftRecipe(
                "CraftButtonSnare",
                FactoryResource.Snare,
                "Place a snare on a dry empty tile. Rabbits that step onto it get trapped and can be dug up for meat and a stick.",
                new FactoryIngredient(FactoryResource.Rope, FactoryRules.SnareRopeCost),
                new FactoryIngredient(FactoryResource.Stick, FactoryRules.SnareStickCost)
            )
        ];

        public static FactoryCraftRecipe FindByButton(string buttonName)
        {
            for (int i = 0; i < Recipes.Length; i++)
            {
                if (string.Equals(Recipes[i].ButtonName, buttonName, StringComparison.Ordinal))
                    return Recipes[i];
            }

            return null;
        }
    }
}
