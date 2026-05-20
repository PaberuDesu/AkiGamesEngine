using System.Collections.Generic;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AkiGames.UI;
using AkiGames.Core;

namespace AkiGames.Scripts
{
    internal sealed class FactoryHudView
    {
        private readonly GameObject[] _hotbarSlots = new GameObject[FactoryInventory.HotbarSlotCount];
        private readonly Image[] _hotbarSlotImages = new Image[FactoryInventory.HotbarSlotCount];
        private readonly Image[] _hotbarIcons = new Image[FactoryInventory.HotbarSlotCount];
        private readonly Text[] _hotbarCounts = new Text[FactoryInventory.HotbarSlotCount];
        private readonly GameObject[] _inventorySlots = new GameObject[FactoryInventory.SlotCount];
        private readonly Image[] _inventorySlotImages = new Image[FactoryInventory.SlotCount];
        private readonly Image[] _inventoryIcons = new Image[FactoryInventory.SlotCount];
        private readonly Text[] _inventoryCounts = new Text[FactoryInventory.SlotCount];
        private readonly GameObject[] _craftButtons = new GameObject[FactoryCrafting.Recipes.Length];
        private readonly Image[] _craftButtonImages = new Image[FactoryCrafting.Recipes.Length];
        private readonly Image[] _craftButtonIcons = new Image[FactoryCrafting.Recipes.Length];
        private readonly Text[] _craftButtonCosts = new Text[FactoryCrafting.Recipes.Length];

        private Image _furnaceFuelSlotImage;
        private Image _furnaceFuelIcon;
        private Text _furnaceFuelCount;
        private Image _furnaceInputSlotImage;
        private Image _furnaceInputIcon;
        private Text _furnaceInputCount;
        private Text _furnaceTitleText;
        private Text _furnaceFuelLabelText;
        private Text _furnaceInputLabelText;
        private Text _craftInfoTitle;
        private Text _craftInfoCost;
        private Text _craftInfoDescription;
        private Text _statusText;
        private Text _positionText;
        private Text _craftingHintText;
        private Text _selectedItemText;
        private Text _furnaceHintText;
        private GameObject _inventoryPanel;
        private GameObject _craftGridRoot;
        private GameObject _furnaceGridRoot;
        private GameObject _pausePanel;
        private GameObject _pauseContinueButton;
        private GameObject _pauseSaveButton;
        private GameObject _pauseLoadButton;
        private GameObject _pauseMenuButton;
        private GameObject _furnaceFuelSlot;
        private GameObject _furnaceInputSlot;

        public void Bind(GameObject owner)
        {
            GameObject root = owner;
            while (root?.Parent != null)
                root = root.Parent;

            _statusText = FindByName(root, "StatusText")?.GetComponent<Text>();
            _positionText = FindByName(root, "PositionText")?.GetComponent<Text>();
            _selectedItemText = FindByName(root, "SelectedItemText")?.GetComponent<Text>();
            _inventoryPanel = FindByName(root, "InventoryPanel");
            _craftGridRoot = FindByName(root, "CraftGridRoot");
            _furnaceGridRoot = FindByName(root, "FurnaceGridRoot");
            _pausePanel = FindByName(root, "PausePanel");
            _pauseContinueButton = FindByName(root, "PauseContinueButton");
            _pauseSaveButton = FindByName(root, "PauseSaveButton");
            _pauseLoadButton = FindByName(root, "PauseLoadButton");
            _pauseMenuButton = FindByName(root, "PauseMainMenuButton");
            _craftingHintText = FindByName(root, "CraftingHint")?.GetComponent<Text>();
            _furnaceHintText = FindByName(root, "FurnaceHint")?.GetComponent<Text>();
            _furnaceTitleText = FindByName(root, "FurnaceTitle")?.GetComponent<Text>();
            _furnaceFuelLabelText = FindByName(root, "FurnaceFuelLabel")?.GetComponent<Text>();
            _furnaceInputLabelText = FindByName(root, "FurnaceInputLabel")?.GetComponent<Text>();
            _craftInfoTitle = FindByName(root, "CraftInfoTitle")?.GetComponent<Text>();
            _craftInfoCost = FindByName(root, "CraftInfoCost")?.GetComponent<Text>();
            _craftInfoDescription = FindByName(root, "CraftInfoDescription")?.GetComponent<Text>();

            _furnaceFuelSlot = FindByName(root, "FurnaceFuelSlot");
            _furnaceInputSlot = FindByName(root, "FurnaceInputSlot");
            _furnaceFuelSlotImage = _furnaceFuelSlot?.GetComponent<Image>();
            _furnaceInputSlotImage = _furnaceInputSlot?.GetComponent<Image>();
            _furnaceFuelIcon = FindByName(_furnaceFuelSlot, "FurnaceFuelIcon")?.GetComponent<Image>();
            _furnaceInputIcon = FindByName(_furnaceInputSlot, "FurnaceInputIcon")?.GetComponent<Image>();
            _furnaceFuelCount = FindByName(_furnaceFuelSlot, "FurnaceFuelCount")?.GetComponent<Text>();
            _furnaceInputCount = FindByName(_furnaceInputSlot, "FurnaceInputCount")?.GetComponent<Text>();

            for (int i = 0; i < FactoryInventory.HotbarSlotCount; i++)
            {
                GameObject slot = FindByName(root, $"HotbarSlot{i}");
                _hotbarSlots[i] = slot;
                _hotbarSlotImages[i] = slot?.GetComponent<Image>();
                _hotbarIcons[i] = FindByName(slot, $"ResourceIcon{i}")?.GetComponent<Image>();
                _hotbarCounts[i] = FindByName(slot, $"ResourceCount{i}")?.GetComponent<Text>();

                Text keyText = FindByName(slot, $"HotbarKey{i}")?.GetComponent<Text>();
                if (keyText != null)
                    keyText.text = i == 9 ? "0" : (i + 1).ToString();
            }

            for (int i = 0; i < FactoryInventory.SlotCount; i++)
            {
                GameObject slot = FindByName(root, $"InventorySlot{i}");
                _inventorySlots[i] = slot;
                _inventorySlotImages[i] = slot?.GetComponent<Image>();
                _inventoryIcons[i] = FindByName(slot, $"InventoryIcon{i}")?.GetComponent<Image>();
                _inventoryCounts[i] = FindByName(slot, $"InventoryCount{i}")?.GetComponent<Text>();
            }

            for (int i = 0; i < FactoryCrafting.Recipes.Length; i++)
            {
                FactoryCraftRecipe recipe = FactoryCrafting.Recipes[i];
                GameObject button = FindByName(root, recipe.ButtonName);
                _craftButtons[i] = button;
                _craftButtonImages[i] = button?.GetComponent<Image>();
                _craftButtonIcons[i] = FindByName(button, $"{recipe.ButtonName}Icon")?.GetComponent<Image>();
                _craftButtonCosts[i] = FindByName(button, $"{recipe.ButtonName}Cost")?.GetComponent<Text>();
            }
        }

        public void Update(
            FactoryInventory inventory,
            IFactoryStorageMachine machine,
            int selectedHotbarSlot,
            FactoryUiSlotReference? pickedSlot,
            FactoryCraftRecipe hoveredRecipe,
            FactoryResource? hoveredItem,
            string statusText,
            string debugText,
            bool debugVisible,
            bool inventoryOpen,
            bool machineOpen,
            bool pauseOpen,
            string selectedItemToast
        )
        {
            if (_inventoryPanel != null)
                _inventoryPanel.IsActive = inventoryOpen;
            if (_craftGridRoot != null)
                _craftGridRoot.IsActive = inventoryOpen && !machineOpen;
            if (_furnaceGridRoot != null)
                _furnaceGridRoot.IsActive = inventoryOpen && machineOpen;
            if (_pausePanel != null)
                _pausePanel.IsActive = pauseOpen;

            if (_statusText != null)
                _statusText.text = statusText ?? "";

            if (_positionText != null)
                _positionText.text = debugVisible ? (debugText ?? "") : "";

            if (_craftingHintText != null)
                _craftingHintText.text = "LMB move items. LMB world: place or use. RMB: dig.";

            if (_selectedItemText != null)
                _selectedItemText.text = selectedItemToast ?? "";

            for (int i = 0; i < FactoryInventory.HotbarSlotCount; i++)
                UpdateHotbarSlot(inventory, selectedHotbarSlot, pickedSlot, i);

            for (int i = 0; i < FactoryInventory.SlotCount; i++)
                UpdateInventorySlot(inventory, pickedSlot, i);

            UpdateCrafting(inventory, hoveredRecipe);
            UpdateMachine(machine, pickedSlot);
            UpdateInfo(hoveredRecipe, hoveredItem);
        }

        public bool TryGetHotbarClickedSlot(Point mousePosition, out int slotIndex)
        {
            for (int i = 0; i < _hotbarSlots.Length; i++)
            {
                if (_hotbarSlots[i]?.uiTransform.Contains(mousePosition) == true)
                {
                    slotIndex = i;
                    return true;
                }
            }

            slotIndex = -1;
            return false;
        }

        public bool TryGetStorageSlot(Point mousePosition, bool machineOpen, out FactoryUiSlotReference slotReference)
        {
            if (_inventoryPanel?.IsActive != true)
            {
                slotReference = default;
                return false;
            }

            for (int i = 0; i < _inventorySlots.Length; i++)
            {
                if (_inventorySlots[i]?.uiTransform.Contains(mousePosition) == true)
                {
                    slotReference = new FactoryUiSlotReference(FactoryUiSlotKind.Inventory, i);
                    return true;
                }
            }

            if (machineOpen)
            {
                if (_furnaceFuelSlot?.uiTransform.Contains(mousePosition) == true)
                {
                    slotReference = new FactoryUiSlotReference(FactoryUiSlotKind.FurnaceFuel);
                    return true;
                }

                if (_furnaceInputSlot?.uiTransform.Contains(mousePosition) == true)
                {
                    slotReference = new FactoryUiSlotReference(FactoryUiSlotKind.FurnaceInput);
                    return true;
                }
            }

            slotReference = default;
            return false;
        }

        public bool TryGetCraftRecipeClick(Point mousePosition, out FactoryCraftRecipe recipe)
        {
            if (_inventoryPanel?.IsActive != true || _craftGridRoot?.IsActive != true)
            {
                recipe = null;
                return false;
            }

            for (int i = 0; i < FactoryCrafting.Recipes.Length; i++)
            {
                if (_craftButtons[i]?.uiTransform.Contains(mousePosition) == true)
                {
                    recipe = FactoryCrafting.Recipes[i];
                    return true;
                }
            }

            recipe = null;
            return false;
        }

        public FactoryCraftRecipe GetHoveredCraftRecipe(Point mousePosition)
        {
            if (_inventoryPanel?.IsActive != true || _craftGridRoot?.IsActive != true)
                return null;

            for (int i = 0; i < FactoryCrafting.Recipes.Length; i++)
            {
                if (_craftButtons[i]?.uiTransform.Contains(mousePosition) == true)
                    return FactoryCrafting.Recipes[i];
            }

            return null;
        }

        public bool TryGetPauseAction(Point mousePosition, out string action)
        {
            if (_pausePanel?.IsActive != true)
            {
                action = null;
                return false;
            }

            if (_pauseContinueButton?.uiTransform.Contains(mousePosition) == true)
            {
                action = "continue";
                return true;
            }

            if (_pauseSaveButton?.uiTransform.Contains(mousePosition) == true)
            {
                action = "save";
                return true;
            }

            if (_pauseLoadButton?.uiTransform.Contains(mousePosition) == true)
            {
                action = "load";
                return true;
            }

            if (_pauseMenuButton?.uiTransform.Contains(mousePosition) == true)
            {
                action = "menu";
                return true;
            }

            action = null;
            return false;
        }

        private void UpdateHotbarSlot(FactoryInventory inventory, int selectedHotbarSlot, FactoryUiSlotReference? pickedSlot, int slotIndex)
        {
            bool selected = slotIndex == selectedHotbarSlot;
            bool picked = pickedSlot?.Kind == FactoryUiSlotKind.Inventory && pickedSlot?.Index == slotIndex;
            if (_hotbarSlotImages[slotIndex] != null)
            {
                _hotbarSlotImages[slotIndex].fillColor = picked
                    ? new Color(107, 92, 56)
                    : selected
                        ? new Color(70, 82, 74)
                        : new Color(39, 43, 42);
            }

            UpdateResourceVisual(_hotbarIcons[slotIndex], _hotbarCounts[slotIndex], inventory.GetSlot(slotIndex));
        }

        private void UpdateInventorySlot(FactoryInventory inventory, FactoryUiSlotReference? pickedSlot, int slotIndex)
        {
            bool picked = pickedSlot?.Kind == FactoryUiSlotKind.Inventory && pickedSlot?.Index == slotIndex;
            if (_inventorySlotImages[slotIndex] != null)
            {
                _inventorySlotImages[slotIndex].fillColor = picked
                    ? new Color(110, 95, 59)
                    : new Color(46, 49, 48);
            }

            UpdateResourceVisual(_inventoryIcons[slotIndex], _inventoryCounts[slotIndex], inventory.GetSlot(slotIndex));
        }

        private void UpdateCrafting(FactoryInventory inventory, FactoryCraftRecipe hoveredRecipe)
        {
            for (int i = 0; i < FactoryCrafting.Recipes.Length; i++)
            {
                FactoryCraftRecipe recipe = FactoryCrafting.Recipes[i];
                bool canCraft = inventory.HasAll(recipe.Ingredients) &&
                    inventory.CanAdd(recipe.Result, 1);
                bool hovered = hoveredRecipe?.Result == recipe.Result;

                if (_craftButtonImages[i] != null)
                {
                    _craftButtonImages[i].fillColor = hovered
                        ? canCraft
                            ? new Color(83, 108, 87)
                            : new Color(90, 66, 66)
                        : canCraft
                            ? new Color(61, 79, 64)
                            : new Color(68, 55, 55);
                }

                if (_craftButtonIcons[i] != null)
                {
                    string texturePath = FactoryTextureCatalog.GetResourceTexture(recipe.Result);
                    _craftButtonIcons[i].texture = LoadTexture(texturePath);
                    _craftButtonIcons[i].fillColor = Color.White;
                    _craftButtonIcons[i].Enabled = _craftButtonIcons[i].texture != null;
                }

                if (_craftButtonCosts[i] != null)
                    _craftButtonCosts[i].text = "";
            }
        }

        private void UpdateMachine(IFactoryStorageMachine machine, FactoryUiSlotReference? pickedSlot)
        {
            FactoryInventorySlot fuelSlot = machine?.FuelSlot;
            FactoryInventorySlot primarySlot = machine?.PrimarySlot;

            if (_furnaceTitleText != null)
                _furnaceTitleText.text = machine?.TitleText ?? "Machine";

            if (_furnaceFuelLabelText != null)
                _furnaceFuelLabelText.text = machine?.FuelLabelText ?? "Fuel";

            if (_furnaceInputLabelText != null)
                _furnaceInputLabelText.text = machine?.PrimaryLabelText ?? "Input";

            if (_furnaceHintText != null)
                _furnaceHintText.text = machine?.HintText ?? "Place fuel and material here. Esc closes.";

            if (_furnaceFuelSlotImage != null)
                _furnaceFuelSlotImage.fillColor = pickedSlot?.Kind == FactoryUiSlotKind.FurnaceFuel
                    ? new Color(110, 95, 59)
                    : new Color(46, 49, 48);

            if (_furnaceInputSlotImage != null)
                _furnaceInputSlotImage.fillColor = pickedSlot?.Kind == FactoryUiSlotKind.FurnaceInput
                    ? new Color(110, 95, 59)
                    : new Color(46, 49, 48);

            UpdateResourceVisual(_furnaceFuelIcon, _furnaceFuelCount, fuelSlot);
            UpdateResourceVisual(_furnaceInputIcon, _furnaceInputCount, primarySlot);
        }

        private void UpdateInfo(FactoryCraftRecipe hoveredRecipe, FactoryResource? hoveredItem)
        {
            if (_craftInfoTitle == null || _craftInfoCost == null || _craftInfoDescription == null)
                return;

            if (hoveredItem.HasValue)
            {
                _craftInfoTitle.text = FactoryRules.ResourceName(hoveredItem.Value);
                _craftInfoCost.text = "";
                _craftInfoDescription.text = FactoryRules.ResourceDescription(hoveredItem.Value);
                return;
            }

            if (hoveredRecipe != null)
            {
                _craftInfoTitle.text = hoveredRecipe.DisplayName;
                _craftInfoCost.text = hoveredRecipe.CostLabel;
                _craftInfoDescription.text = hoveredRecipe.Description;
                return;
            }

            _craftInfoTitle.text = "Info";
            _craftInfoCost.text = "";
            _craftInfoDescription.text = "Hover a recipe or an item slot to see more information.";
        }

        private static void UpdateResourceVisual(Image icon, Text countText, FactoryInventorySlot slot)
        {
            bool isVisible = slot != null && !slot.IsEmpty;

            if (icon != null)
            {
                icon.Enabled = isVisible;
                if (isVisible)
                {
                    string texturePath = FactoryTextureCatalog.GetResourceTexture(slot.Resource);
                    icon.texture = LoadTexture(texturePath);
                    icon.fillColor = Color.White;
                }
                else
                {
                    icon.texture = null;
                }
            }

            if (countText != null)
                countText.text = isVisible ? slot.Count.ToString() : "";
        }

        private static readonly Dictionary<string, Texture2D> _staticTextureCache = [];

        private static Texture2D LoadTexture(string texturePath)
        {
            if (string.IsNullOrWhiteSpace(texturePath))
                return null;

            if (_staticTextureCache.TryGetValue(texturePath, out Texture2D texture))
                return texture;

            texture = Game1.LoadGameTexture(texturePath);
            _staticTextureCache[texturePath] = texture;
            return texture;
        }

        private static GameObject FindByName(GameObject root, string objectName)
        {
            if (root == null) return null;
            if (root.ObjectName == objectName) return root;

            foreach (GameObject child in root.Children)
            {
                GameObject found = FindByName(child, objectName);
                if (found != null) return found;
            }

            return null;
        }
    }
}
