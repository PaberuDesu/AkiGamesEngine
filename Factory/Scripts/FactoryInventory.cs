using System;

namespace AkiGames.Scripts
{
    internal sealed class FactoryInventorySlot
    {
        public FactoryResource Resource { get; private set; }
        public int Count { get; private set; }

        public bool IsEmpty => Count <= 0;

        public void Set(FactoryResource resource, int count)
        {
            Resource = resource;
            Count = Math.Max(0, count);
            if (Count == 0)
                Resource = default;
        }

        public void Add(int amount) =>
            Set(Resource, Count + amount);

        public void Remove(int amount) =>
            Set(Resource, Count - amount);

        public bool CanAccept(FactoryResource resource, int amount)
        {
            if (amount <= 0)
                return true;

            return IsEmpty
                ? amount <= FactoryRules.MaxStackSize
                : Resource == resource && Count + amount <= FactoryRules.MaxStackSize;
        }

        public int AddUpTo(FactoryResource resource, int amount)
        {
            if (amount <= 0)
                return 0;

            if (IsEmpty)
            {
                int added = Math.Min(FactoryRules.MaxStackSize, amount);
                Set(resource, added);
                return added;
            }

            if (Resource != resource || Count >= FactoryRules.MaxStackSize)
                return 0;

            int accepted = Math.Min(FactoryRules.MaxStackSize - Count, amount);
            Add(accepted);
            return accepted;
        }

        public void Clear() =>
            Set(default, 0);
    }

    internal sealed class FactoryInventory
    {
        public const int SlotCount = 50;
        public const int HotbarSlotCount = 10;

        private readonly FactoryInventorySlot[] _slots = new FactoryInventorySlot[SlotCount];

        public FactoryInventory()
        {
            for (int i = 0; i < _slots.Length; i++)
                _slots[i] = new FactoryInventorySlot();
        }

        public FactoryInventorySlot GetSlot(int slot) =>
            slot >= 0 && slot < _slots.Length ? _slots[slot] : null;

        public bool CanAdd(FactoryResource resource, int amount)
        {
            if (amount <= 0) return true;

            int remaining = amount;
            for (int i = 0; i < _slots.Length; i++)
            {
                FactoryInventorySlot slot = _slots[i];
                if (!slot.IsEmpty && slot.Resource == resource)
                    remaining -= FactoryRules.MaxStackSize - slot.Count;
                else if (slot.IsEmpty)
                    remaining -= FactoryRules.MaxStackSize;

                if (remaining <= 0)
                    return true;
            }

            return false;
        }

        public bool TryAdd(FactoryResource resource, int amount)
        {
            if (!CanAdd(resource, amount)) return false;

            int remaining = amount;

            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                FactoryInventorySlot slot = _slots[i];
                if (slot.IsEmpty || slot.Resource != resource || slot.Count >= FactoryRules.MaxStackSize) continue;

                int added = Math.Min(FactoryRules.MaxStackSize - slot.Count, remaining);
                slot.Add(added);
                remaining -= added;
            }

            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                FactoryInventorySlot slot = _slots[i];
                if (!slot.IsEmpty) continue;

                int added = Math.Min(FactoryRules.MaxStackSize, remaining);
                slot.Set(resource, added);
                remaining -= added;
            }

            return true;
        }

        public int Count(FactoryResource resource)
        {
            int count = 0;
            for (int i = 0; i < _slots.Length; i++)
            {
                FactoryInventorySlot slot = _slots[i];
                if (!slot.IsEmpty && slot.Resource == resource)
                    count += slot.Count;
            }

            return count;
        }

        public bool Has(FactoryResource resource, int amount) =>
            Count(resource) >= amount;

        public bool HasAll(FactoryIngredient[] ingredients)
        {
            if (ingredients == null) return true;

            for (int i = 0; i < ingredients.Length; i++)
            {
                if (!Has(ingredients[i].Resource, ingredients[i].Count))
                    return false;
            }

            return true;
        }

        public bool TrySpend(FactoryResource resource, int amount)
        {
            if (!Has(resource, amount)) return false;

            int remaining = amount;
            for (int i = 0; i < _slots.Length && remaining > 0; i++)
            {
                FactoryInventorySlot slot = _slots[i];
                if (slot.IsEmpty || slot.Resource != resource) continue;

                int removed = Math.Min(slot.Count, remaining);
                slot.Remove(removed);
                remaining -= removed;
            }

            return true;
        }

        public bool TrySpendAll(FactoryIngredient[] ingredients)
        {
            if (!HasAll(ingredients)) return false;

            for (int i = 0; i < ingredients.Length; i++)
                TrySpend(ingredients[i].Resource, ingredients[i].Count);

            return true;
        }

        public bool TryConsumeFromSlot(int slotIndex, int amount, FactoryResource expectedResource)
        {
            FactoryInventorySlot slot = GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty || slot.Resource != expectedResource || slot.Count < amount)
                return false;

            slot.Remove(amount);
            return true;
        }

        public void MoveOrSwap(int sourceSlotIndex, int targetSlotIndex)
        {
            if (sourceSlotIndex == targetSlotIndex) return;

            FactoryInventorySlot source = GetSlot(sourceSlotIndex);
            FactoryInventorySlot target = GetSlot(targetSlotIndex);
            if (source == null || target == null || source.IsEmpty) return;

            if (target.IsEmpty)
            {
                target.Set(source.Resource, source.Count);
                source.Clear();
                return;
            }

            FactoryResource tempResource = source.Resource;
            int tempCount = source.Count;
            source.Set(target.Resource, target.Count);
            target.Set(tempResource, tempCount);
        }

        public FactoryInventorySlotSaveData[] ExportSlots()
        {
            FactoryInventorySlotSaveData[] data = new FactoryInventorySlotSaveData[_slots.Length];
            for (int i = 0; i < _slots.Length; i++)
                data[i] = FactoryInventorySlotSaveData.FromSlot(_slots[i]);

            return data;
        }

        public void LoadSlots(FactoryInventorySlotSaveData[] data)
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (data != null && i < data.Length)
                    data[i]?.ApplyTo(_slots[i]);
                else
                    _slots[i].Clear();
            }
        }
    }
}
