using System;
using AkiGames.Core.Serialization;
using AkiGames.Events;
using AkiGames.UI;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using Microsoft.Xna.Framework.Input;

namespace AkiGames.Scripts
{
    public class FactoryGame : DrawableComponent
    {
        public int MapWidth = 1024;
        public int MapHeight = 1024;
        public int TileSize = 42;
        public float PlayerSpeed = 5.2f;
        public float InteractionRadius = 6f;

        [DontSerialize, HideInInspector] private FactoryWorld _world;
        [DontSerialize, HideInInspector] private FactoryInventory _inventory;
        [DontSerialize, HideInInspector] private FactoryInteraction _interaction;
        [DontSerialize, HideInInspector] private FactoryCamera _camera;
        [DontSerialize, HideInInspector] private FactoryRenderer _renderer;
        [DontSerialize, HideInInspector] private FactoryHudView _hudView;
        [DontSerialize, HideInInspector] private KeyboardState _previousKeyboard;
        [DontSerialize, HideInInspector] private bool _wasLeftDown;
        [DontSerialize, HideInInspector] private bool _wasRightDown;
        [DontSerialize, HideInInspector] private int _previousScrollWheel;
        [DontSerialize, HideInInspector] private int _selectedHotbarSlot;
        [DontSerialize, HideInInspector] private bool _inventoryOpen;
        [DontSerialize, HideInInspector] private bool _machineOpen;
        [DontSerialize, HideInInspector] private FactoryLevel _openMachineLevel;
        [DontSerialize, HideInInspector] private Point _openMachineTile;
        [DontSerialize, HideInInspector] private bool _pauseMenuOpen;
        [DontSerialize, HideInInspector] private FactoryUiSlotReference? _pickedSlot;
        [DontSerialize, HideInInspector] private FactoryCraftRecipe _hoveredCraftRecipe;
        [DontSerialize, HideInInspector] private FactoryResource? _hoveredResource;
        [DontSerialize, HideInInspector] private string _selectedItemToast = "";
        [DontSerialize, HideInInspector] private float _selectedItemToastSeconds;
        [DontSerialize, HideInInspector] private int _currentSeed = Environment.TickCount;
        [DontSerialize, HideInInspector] private bool _debugInfoVisible;

        public override void Awake()
        {
            InitializeGameState();
        }

        public override void Update()
        {
            EnsureGameState();

            float dt = Math.Min((float)gameTime.ElapsedGameTime.TotalSeconds, 0.05f);
            KeyboardState keyboard = Keyboard.GetState();
            MouseState mouse = Mouse.GetState();

            HandleZoom(mouse);
            HandleMenuKeys(keyboard);
            HandlePauseClick(mouse);

            if (!_pauseMenuOpen)
            {
                HandleBoatExit(keyboard);

                if (!_inventoryOpen)
                    HandleMovement(keyboard, dt);

                HandleHotbarKeys(keyboard);
                HandlePrimaryClick(mouse);
                HandleUiHover(mouse);
                HandleHover(mouse);
                HandleDigging(mouse, dt);
                _world.Update(dt, _inventory);
            }

            UpdateSelectedItemToast(dt);

            if (_pickedSlot.HasValue && (ResolveSlot(_pickedSlot.Value)?.IsEmpty ?? true))
                _pickedSlot = null;

            _previousKeyboard = keyboard;
            _wasLeftDown = mouse.LeftButton == ButtonState.Pressed;
            _wasRightDown = mouse.RightButton == ButtonState.Pressed;
            _previousScrollWheel = mouse.ScrollWheelValue;
        }

        public override void Draw(SpriteBatch spriteBatch)
        {
            EnsureGameState();

            Rectangle worldBounds = uiTransform.Bounds;
            _renderer.Draw(spriteBatch, _world, _camera, _interaction, worldBounds, InteractionRadius);
            _hudView.Update(
                _inventory,
                GetOpenMachine(),
                _selectedHotbarSlot,
                _pickedSlot,
                _hoveredCraftRecipe,
                _hoveredResource,
                _interaction.StatusText,
                _world.GetDebugHudText(),
                _debugInfoVisible,
                _inventoryOpen,
                _machineOpen,
                _pauseMenuOpen,
                _selectedItemToastSeconds > 0f ? _selectedItemToast : ""
            );

            if ((_inventoryOpen || _machineOpen) && _pickedSlot.HasValue)
            {
                FactoryInventorySlot slot = ResolveSlot(_pickedSlot.Value);
                if (slot != null && !slot.IsEmpty)
                    _renderer.DrawCursorItem(spriteBatch, slot.Resource, Input.mousePosition);
            }
        }

        internal FactorySaveData ExportSave() =>
            new()
            {
                World = _world.ExportState(),
                InventorySlots = _inventory.ExportSlots(),
                SelectedHotbarSlot = _selectedHotbarSlot
            };

        internal void LoadFromSave(FactorySaveData saveData)
        {
            if (saveData == null) return;

            if (saveData.World != null)
                _currentSeed = saveData.World.Seed;

            InitializeGameState();

            if (saveData.World != null)
                _world.LoadState(saveData.World);

            _inventory.LoadSlots(saveData.InventorySlots);
            _selectedHotbarSlot = Math.Clamp(saveData.SelectedHotbarSlot, 0, FactoryInventory.HotbarSlotCount - 1);
            _inventoryOpen = false;
            _machineOpen = false;
            _pauseMenuOpen = false;
            _pickedSlot = null;
            _hoveredCraftRecipe = null;
            _hoveredResource = null;
            _interaction.Clear();
            _interaction.SetStatus("Save loaded.");
            ShowSelectedItemToastForSlot(_selectedHotbarSlot);
        }

        internal void StartNewWorld(int seed)
        {
            _currentSeed = seed;
            InitializeGameState();
            _interaction.SetStatus($"New world started. Seed {_currentSeed}.");
        }

        private void InitializeGameState()
        {
            MapWidth = Math.Max(256, MapWidth);
            MapHeight = Math.Max(256, MapHeight);
            TileSize = Math.Max(20, TileSize);
            PlayerSpeed = Math.Max(1f, PlayerSpeed);
            InteractionRadius = Math.Max(1f, InteractionRadius);

            _world = new FactoryWorld(MapWidth, MapHeight, _currentSeed);
            _inventory = new FactoryInventory();
            _interaction = new FactoryInteraction();
            _camera = new FactoryCamera(TileSize);
            _renderer = new FactoryRenderer();
            _hudView = new FactoryHudView();
            _hudView.Bind(gameObject);
            _previousKeyboard = Keyboard.GetState();
            _previousScrollWheel = Mouse.GetState().ScrollWheelValue;
            _wasLeftDown = false;
            _wasRightDown = false;
            _selectedHotbarSlot = 0;
            _inventoryOpen = false;
            _machineOpen = false;
            _pauseMenuOpen = false;
            _pickedSlot = null;
            _hoveredCraftRecipe = null;
            _hoveredResource = null;
            _selectedItemToast = "";
            _selectedItemToastSeconds = 0f;
            _debugInfoVisible = false;
        }

        private void EnsureGameState()
        {
            if (_world != null) return;
            InitializeGameState();
        }

        private void HandleMovement(KeyboardState keyboard, float dt)
        {
            Vector2 direction = Vector2.Zero;
            if (keyboard.IsKeyDown(Keys.W)) direction.Y -= 1;
            if (keyboard.IsKeyDown(Keys.S)) direction.Y += 1;
            if (keyboard.IsKeyDown(Keys.A)) direction.X -= 1;
            if (keyboard.IsKeyDown(Keys.D)) direction.X += 1;

            _world.MovePlayer(direction, PlayerSpeed, dt);
        }

        private void HandleZoom(MouseState mouse)
        {
            if (_inventoryOpen || _pauseMenuOpen) return;

            int scrollDelta = mouse.ScrollWheelValue - _previousScrollWheel;
            _camera.AdjustZoom(scrollDelta);
        }

        private void HandleMenuKeys(KeyboardState keyboard)
        {
            bool escapePressed = keyboard.IsKeyDown(Keys.Escape) && !_previousKeyboard.IsKeyDown(Keys.Escape);
            bool inventoryPressed = keyboard.IsKeyDown(Keys.E) && !_previousKeyboard.IsKeyDown(Keys.E);
            bool debugPressed = keyboard.IsKeyDown(Keys.F3) && !_previousKeyboard.IsKeyDown(Keys.F3);

            if (debugPressed)
                _debugInfoVisible = !_debugInfoVisible;

            if (escapePressed)
            {
                if (_pauseMenuOpen)
                {
                    _pauseMenuOpen = false;
                    _interaction.SetStatus("Paused menu closed.");
                }
                else if (_machineOpen)
                {
                    CloseStorageMenus();
                    _interaction.SetStatus("Machine closed.");
                }
                else if (_inventoryOpen)
                {
                    CloseStorageMenus();
                    _interaction.SetStatus("Inventory closed.");
                }
                else
                {
                    _pauseMenuOpen = true;
                    _interaction.SetStatus("Paused.");
                }
            }

            if (!inventoryPressed || _pauseMenuOpen) return;

            if (_machineOpen)
                return;

            _inventoryOpen = !_inventoryOpen;
            _pickedSlot = null;
            _hoveredCraftRecipe = null;
            _hoveredResource = null;
        }

        private void HandlePauseClick(MouseState mouse)
        {
            if (!_pauseMenuOpen || mouse.LeftButton != ButtonState.Pressed || _wasLeftDown)
                return;

            Point mousePosition = Input.mousePosition;
            if (!_hudView.TryGetPauseAction(mousePosition, out string action))
                return;

            switch (action)
            {
                case "continue":
                    _pauseMenuOpen = false;
                    _interaction.SetStatus("Back to the game.");
                    break;
                case "save":
                    FactoryApp.Instance?.RequestSaveGame();
                    _interaction.SetStatus("Saving game...");
                    break;
                case "load":
                    FactoryApp.Instance?.RequestLoadGame();
                    break;
                case "menu":
                    FactoryApp.Instance?.RequestMainMenu();
                    break;
            }
        }

        private void HandleUiHover(MouseState mouse)
        {
            _hoveredCraftRecipe = null;
            _hoveredResource = null;
            if (!_inventoryOpen) return;
            Point mousePosition = Input.mousePosition;

            if (_hudView.TryGetStorageSlot(mousePosition, _machineOpen, out FactoryUiSlotReference slotReference))
            {
                FactoryInventorySlot slot = ResolveSlot(slotReference);
                if (slot != null && !slot.IsEmpty)
                    _hoveredResource = slot.Resource;
            }

            if (!_machineOpen && !_hoveredResource.HasValue)
                _hoveredCraftRecipe = _hudView.GetHoveredCraftRecipe(mousePosition);
        }

        private void HandleBoatExit(KeyboardState keyboard)
        {
            bool shiftPressed =
                (keyboard.IsKeyDown(Keys.LeftShift) && !_previousKeyboard.IsKeyDown(Keys.LeftShift)) ||
                (keyboard.IsKeyDown(Keys.RightShift) && !_previousKeyboard.IsKeyDown(Keys.RightShift));

            if (!shiftPressed || !_world.Player.IsOnBoat) return;

            if (_world.TryLeaveBoat())
            {
                _interaction.Clear();
                _interaction.SetStatus("You stepped off the boat.");
                return;
            }

            _interaction.SetStatus("No free shore tile nearby.");
        }

        private void HandleHotbarKeys(KeyboardState keyboard)
        {
            Keys[] keys =
            [
                Keys.D1, Keys.D2, Keys.D3, Keys.D4, Keys.D5,
                Keys.D6, Keys.D7, Keys.D8, Keys.D9, Keys.D0
            ];

            for (int i = 0; i < keys.Length; i++)
            {
                if (keyboard.IsKeyDown(keys[i]) && !_previousKeyboard.IsKeyDown(keys[i]))
                {
                    _selectedHotbarSlot = i;
                    ShowSelectedItemToastForSlot(i);
                }
            }
        }

        private void HandlePrimaryClick(MouseState mouse)
        {
            if (mouse.LeftButton != ButtonState.Pressed || _wasLeftDown) return;
            Point mousePosition = Input.mousePosition;

            if (_hudView.TryGetHotbarClickedSlot(mousePosition, out int hotbarSlot))
            {
                _selectedHotbarSlot = hotbarSlot;
                ShowSelectedItemToastForSlot(hotbarSlot);
                return;
            }

            if (_pauseMenuOpen) return;

            if (_inventoryOpen)
            {
                if (_hudView.TryGetStorageSlot(mousePosition, _machineOpen, out FactoryUiSlotReference slotReference))
                {
                    HandleStorageSlotClick(slotReference);
                    return;
                }

                if (!_machineOpen && _hudView.TryGetCraftRecipeClick(mousePosition, out FactoryCraftRecipe recipe))
                {
                    CraftItem(recipe);
                    return;
                }

                return;
            }

            Rectangle worldBounds = uiTransform.Bounds;
            if (!worldBounds.Contains(mousePosition)) return;

            Point tile = _camera.ScreenToTile(mousePosition, worldBounds, _world.Player.Position);
            if (!_world.InBounds(tile.X, tile.Y)) return;

            HandleWorldPrimaryAction(tile);
        }

        private void HandleStorageSlotClick(FactoryUiSlotReference slotReference)
        {
            FactoryInventorySlot slot = ResolveSlot(slotReference);
            if (_pickedSlot == null)
            {
                if (slot == null || slot.IsEmpty) return;

                _pickedSlot = slotReference;
                _interaction.SetStatus($"{FactoryRules.ResourceName(slot.Resource)} is ready to move.");
                return;
            }

            if (IsSameSlot(_pickedSlot.Value, slotReference))
            {
                _pickedSlot = null;
                return;
            }

            MoveOrSwapSlots(_pickedSlot.Value, slotReference);
            _pickedSlot = null;
            _interaction.SetStatus("Inventory updated.");
        }

        private void HandleWorldPrimaryAction(Point tile)
        {
            if (Vector2.Distance(_world.Player.Position, FactoryWorld.TileCenter(tile)) > InteractionRadius)
            {
                _interaction.SetStatus("Target is too far away.");
                return;
            }

            if (_world.IsLadderAt(tile))
            {
                if (_world.TryUseLadder(tile))
                {
                    _interaction.Clear();
                    _interaction.SetStatus(_world.Player.Level == FactoryLevel.Cave
                        ? "You climbed down into the cave."
                        : "You climbed back to the surface.");
                }
                else
                {
                    _interaction.SetStatus("There is no free place to step out here.");
                }

                return;
            }

            FactoryTile clickedTile = _world.GetTile(tile);
            if (clickedTile?.ObjectType == FactoryObjectType.WoodDoor)
            {
                if (_world.TryToggleDoor(tile, out bool isOpen))
                    _interaction.SetStatus(isOpen ? "Door opened." : "Door closed.");
                else
                    _interaction.SetStatus("Something is standing in the doorway.");
                return;
            }

            if (clickedTile?.ObjectType is FactoryObjectType.Furnace or FactoryObjectType.SolidFuelDrill)
            {
                _inventoryOpen = true;
                _machineOpen = true;
                _openMachineLevel = _world.Player.Level;
                _openMachineTile = tile;
                _pickedSlot = null;
                _interaction.SetStatus(clickedTile.ObjectType == FactoryObjectType.Furnace
                    ? "Furnace opened."
                    : "Solid fuel drill opened.");
                return;
            }

            if (_world.Player.Level == FactoryLevel.Surface && _world.IsBoatAt(tile) && !_world.Player.IsOnBoat)
            {
                if (_world.TryBoardBoat(tile))
                {
                    _interaction.Clear();
                    _interaction.SetStatus("You are on the boat. Press Shift to get out.");
                }

                return;
            }

            if (_world.GetCreatureAt(_world.Player.Level, tile, blockingOnly: true) != null)
            {
                _interaction.SetStatus("Something living is standing there.");
                return;
            }

            FactoryInventorySlot selectedSlot = _inventory.GetSlot(_selectedHotbarSlot);
            if (selectedSlot == null || selectedSlot.IsEmpty)
            {
                _interaction.SetStatus("Select a placeable item in the hotbar.");
                return;
            }

            switch (selectedSlot.Resource)
            {
                case FactoryResource.RabbitMeat:
                    EatRabbitMeat();
                    break;
                case FactoryResource.Ladder:
                    PlaceLadder(tile);
                    break;
                case FactoryResource.Boat:
                    PlaceBoat(tile);
                    break;
                case FactoryResource.Furnace:
                    PlaceFurnace(tile);
                    break;
                case FactoryResource.SolidFuelDrill:
                    PlaceSolidFuelDrill(tile);
                    break;
                case FactoryResource.WoodFlooring:
                    PlaceWoodFlooring(tile);
                    break;
                case FactoryResource.WoodWall:
                    PlaceWoodWall(tile);
                    break;
                case FactoryResource.StoneWall:
                    PlaceStoneWall(tile);
                    break;
                case FactoryResource.WoodDoor:
                    PlaceWoodDoor(tile);
                    break;
                case FactoryResource.Snare:
                    PlaceSnare(tile);
                    break;
                default:
                    _interaction.SetStatus($"{FactoryRules.ResourceName(selectedSlot.Resource)} cannot be placed.");
                    break;
            }
        }

        private void PlaceLadder(Point tile)
        {
            if (!_world.CanPlaceLadder(tile))
            {
                _interaction.SetStatus("Ladders can only be placed on a hole.");
                return;
            }

            if (!_inventory.TryConsumeFromSlot(_selectedHotbarSlot, 1, FactoryResource.Ladder))
            {
                _interaction.SetStatus("No ladder left in this slot.");
                return;
            }

            _world.PlaceLadder(tile);
            _interaction.SetStatus("Ladder placed. Click it to climb down.");
        }

        private void PlaceBoat(Point tile)
        {
            if (!_world.CanPlaceBoat(tile))
            {
                _interaction.SetStatus("Boats can only be placed on empty water.");
                return;
            }

            if (!_inventory.TryConsumeFromSlot(_selectedHotbarSlot, 1, FactoryResource.Boat))
            {
                _interaction.SetStatus("No boat left in this slot.");
                return;
            }

            _world.PlaceBoat(tile);
            _interaction.SetStatus("Boat placed.");
        }

        private void PlaceFurnace(Point tile)
        {
            if (!_world.CanPlaceFurnace(tile))
            {
                _interaction.SetStatus("Furnaces need an empty dry tile.");
                return;
            }

            if (!_inventory.TryConsumeFromSlot(_selectedHotbarSlot, 1, FactoryResource.Furnace))
            {
                _interaction.SetStatus("No furnace left in this slot.");
                return;
            }

            _world.PlaceFurnace(tile);
            _interaction.SetStatus("Furnace placed.");
        }

        private void PlaceSolidFuelDrill(Point tile)
        {
            if (!_world.CanPlaceSolidFuelDrill(tile))
            {
                _interaction.SetStatus("Drills must be placed on an empty ore tile.");
                return;
            }

            if (!_inventory.TryConsumeFromSlot(_selectedHotbarSlot, 1, FactoryResource.SolidFuelDrill))
            {
                _interaction.SetStatus("No drill left in this slot.");
                return;
            }

            _world.PlaceSolidFuelDrill(tile);
            _interaction.SetStatus("Solid fuel drill placed.");
        }

        private void PlaceWoodFlooring(Point tile)
        {
            if (!_world.CanPlaceWoodFloor(tile))
            {
                _interaction.SetStatus("Wood flooring needs an empty dry tile.");
                return;
            }

            if (!_inventory.TryConsumeFromSlot(_selectedHotbarSlot, 1, FactoryResource.WoodFlooring))
            {
                _interaction.SetStatus("No wood flooring left in this slot.");
                return;
            }

            _world.PlaceWoodFloor(tile);
            _interaction.SetStatus("Wood flooring placed.");
        }

        private void PlaceWoodWall(Point tile)
        {
            if (!_world.CanPlaceWoodWall(tile))
            {
                _interaction.SetStatus("Wood walls need an empty dry tile.");
                return;
            }

            if (!_inventory.TryConsumeFromSlot(_selectedHotbarSlot, 1, FactoryResource.WoodWall))
            {
                _interaction.SetStatus("No wood wall left in this slot.");
                return;
            }

            _world.PlaceWoodWall(tile);
            _interaction.SetStatus("Wood wall placed.");
        }

        private void PlaceStoneWall(Point tile)
        {
            if (!_world.CanPlaceStoneWall(tile))
            {
                _interaction.SetStatus("Stone walls need an empty dry tile.");
                return;
            }

            if (!_inventory.TryConsumeFromSlot(_selectedHotbarSlot, 1, FactoryResource.StoneWall))
            {
                _interaction.SetStatus("No stone wall left in this slot.");
                return;
            }

            _world.PlaceStoneWall(tile);
            _interaction.SetStatus("Stone wall placed.");
        }

        private void PlaceWoodDoor(Point tile)
        {
            if (!_world.CanPlaceWoodDoor(tile))
            {
                _interaction.SetStatus("Wood doors need an empty dry tile.");
                return;
            }

            if (!_inventory.TryConsumeFromSlot(_selectedHotbarSlot, 1, FactoryResource.WoodDoor))
            {
                _interaction.SetStatus("No wood door left in this slot.");
                return;
            }

            _world.PlaceWoodDoor(tile);
            _interaction.SetStatus("Wood door placed.");
        }

        private void PlaceSnare(Point tile)
        {
            if (!_world.CanPlaceSnare(tile))
            {
                _interaction.SetStatus("Snares need an empty dry surface tile.");
                return;
            }

            if (!_inventory.TryConsumeFromSlot(_selectedHotbarSlot, 1, FactoryResource.Snare))
            {
                _interaction.SetStatus("No snare left in this slot.");
                return;
            }

            _world.PlaceSnare(tile);
            _interaction.SetStatus("Snare placed.");
        }

        private void EatRabbitMeat()
        {
            FactoryInventorySlot selectedSlot = _inventory.GetSlot(_selectedHotbarSlot);
            if (selectedSlot == null || selectedSlot.IsEmpty || selectedSlot.Resource != FactoryResource.RabbitMeat)
            {
                _interaction.SetStatus("No rabbit meat left in this slot.");
                return;
            }

            if (!_world.Player.TryRestoreFood(1))
            {
                _interaction.SetStatus("Food is already full.");
                return;
            }

            _inventory.TryConsumeFromSlot(_selectedHotbarSlot, 1, FactoryResource.RabbitMeat);

            _interaction.SetStatus("You ate rabbit meat and restored 1 food.");
        }

        private void CraftItem(FactoryCraftRecipe recipe)
        {
            if (recipe == null) return;

            if (!_inventory.HasAll(recipe.Ingredients))
            {
                _interaction.SetStatus($"Need {recipe.CostLabel} for {recipe.DisplayName.ToLowerInvariant()}.");
                return;
            }

            if (!_inventory.CanAdd(recipe.Result, 1))
            {
                _interaction.SetStatus("Inventory is full.");
                return;
            }

            _inventory.TrySpendAll(recipe.Ingredients);
            _inventory.TryAdd(recipe.Result, 1);
            _interaction.SetStatus($"{recipe.DisplayName} crafted.");
        }

        private void HandleHover(MouseState mouse)
        {
            if (_inventoryOpen || _pauseMenuOpen || mouse.LeftButton == ButtonState.Pressed || mouse.RightButton == ButtonState.Pressed)
                return;

            Point mousePosition = Input.mousePosition;
            Rectangle worldBounds = uiTransform.Bounds;
            if (!worldBounds.Contains(mousePosition))
            {
                if (_world.Player.IsOnBoat)
                    _interaction.SetStatus("You are on the boat. Press Shift to get out.");
                return;
            }

            Point tile = _camera.ScreenToTile(mousePosition, worldBounds, _world.Player.Position);
            if (!_world.InBounds(tile.X, tile.Y)) return;

            string hoverText = _world.DescribeHoverTile(tile, InteractionRadius);
            if (_world.Player.IsOnBoat)
                hoverText = $"You are on the boat. Press Shift to get out. | {hoverText}";

            _interaction.SetStatus(hoverText);
        }

        private void HandleDigging(MouseState mouse, float dt)
        {
            if (_inventoryOpen || _pauseMenuOpen)
            {
                _interaction.Clear();
                return;
            }

            if (mouse.RightButton != ButtonState.Pressed)
            {
                _interaction.Clear();
                return;
            }

            Point mousePosition = Input.mousePosition;
            Rectangle worldBounds = uiTransform.Bounds;
            if (!worldBounds.Contains(mousePosition))
            {
                _interaction.Clear();
                return;
            }

            Point tile = _camera.ScreenToTile(mousePosition, worldBounds, _world.Player.Position);
            _interaction.Update(_world, _inventory, tile, InteractionRadius, dt);
        }

        private void CloseStorageMenus()
        {
            _inventoryOpen = false;
            _machineOpen = false;
            _pickedSlot = null;
            _hoveredCraftRecipe = null;
            _hoveredResource = null;
        }

        private IFactoryStorageMachine GetOpenMachine() =>
            _machineOpen ? _world.GetStorageMachine(_openMachineLevel, _openMachineTile) : null;

        private FactoryInventorySlot ResolveSlot(FactoryUiSlotReference slotReference)
        {
            return slotReference.Kind switch
            {
                FactoryUiSlotKind.Inventory => _inventory.GetSlot(slotReference.Index),
                FactoryUiSlotKind.FurnaceFuel => GetOpenMachine()?.FuelSlot,
                FactoryUiSlotKind.FurnaceInput => GetOpenMachine()?.PrimarySlot,
                _ => null
            };
        }

        private void MoveOrSwapSlots(FactoryUiSlotReference sourceReference, FactoryUiSlotReference targetReference)
        {
            FactoryInventorySlot source = ResolveSlot(sourceReference);
            FactoryInventorySlot target = ResolveSlot(targetReference);
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

        private static bool IsSameSlot(FactoryUiSlotReference a, FactoryUiSlotReference b) =>
            a.Kind == b.Kind && a.Index == b.Index;

        private void ShowSelectedItemToastForSlot(int slotIndex)
        {
            FactoryInventorySlot slot = _inventory.GetSlot(slotIndex);
            if (slot == null || slot.IsEmpty)
            {
                _selectedItemToast = "";
                _selectedItemToastSeconds = 0f;
                return;
            }

            _selectedItemToast = FactoryRules.ResourceName(slot.Resource);
            _selectedItemToastSeconds = 1f;
        }

        private void UpdateSelectedItemToast(float dt)
        {
            if (_selectedItemToastSeconds <= 0f) return;

            _selectedItemToastSeconds = Math.Max(0f, _selectedItemToastSeconds - dt);
            if (_selectedItemToastSeconds <= 0f)
                _selectedItemToast = "";
        }
    }
}
