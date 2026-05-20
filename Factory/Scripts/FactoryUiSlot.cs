namespace AkiGames.Scripts
{
    internal enum FactoryUiSlotKind
    {
        Inventory,
        FurnaceFuel,
        FurnaceInput
    }

    internal readonly struct FactoryUiSlotReference
    {
        public FactoryUiSlotReference(FactoryUiSlotKind kind, int index = -1)
        {
            Kind = kind;
            Index = index;
        }

        public FactoryUiSlotKind Kind { get; }
        public int Index { get; }
    }
}
