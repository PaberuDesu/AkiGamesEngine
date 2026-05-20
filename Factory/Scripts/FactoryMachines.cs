namespace AkiGames.Scripts
{
    internal interface IFactoryStorageMachine
    {
        FactoryInventorySlot FuelSlot { get; }
        FactoryInventorySlot PrimarySlot { get; }
        string TitleText { get; }
        string FuelLabelText { get; }
        string PrimaryLabelText { get; }
        string HintText { get; }
    }

    internal sealed class FactoryFurnaceState : IFactoryStorageMachine
    {
        public const float SmeltSeconds = 1.2f;

        public FactoryInventorySlot FuelSlot { get; } = new();
        public FactoryInventorySlot InputSlot { get; } = new();
        public float WorkProgress { get; set; }

        public FactoryInventorySlot PrimarySlot => InputSlot;
        public string TitleText => "Furnace";
        public string FuelLabelText => "Fuel";
        public string PrimaryLabelText => "Input";
        public string HintText => "Coal powers the furnace. Smelt iron ore and copper ore into metal.";
    }

    internal sealed class FactoryDrillState : IFactoryStorageMachine
    {
        public FactoryInventorySlot FuelSlot { get; } = new();
        public FactoryInventorySlot OutputSlot { get; } = new();
        public float WorkProgress { get; set; }
        public int FuelOreChargesRemaining { get; set; }

        public FactoryInventorySlot PrimarySlot => OutputSlot;
        public string TitleText => "Solid fuel drill";
        public string FuelLabelText => "Fuel";
        public string PrimaryLabelText => "Storage";
        public string HintText => "Coal powers the drill. One coal runs five mined ore. Coal output feeds fuel first.";
    }
}
