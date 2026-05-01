using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using AkiGames.Core;
using AkiGames.Core.Serialization;
using AkiGames.Events;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI;

namespace AkiGames.Scripts.Inspector
{
    public class ComponentInspectorSettings : InteractableComponent
    {
        private static GameObject _menuPrefab;
        private static GameObject _menuItemPrefab;
        private static GameComponent _copiedValues;
        private static Type _copiedType;

        private GameObject _menu;
        private Column _menuColumn;
        private InspectorItemController _inspectorItem;

        public override void Awake()
        {
            image = gameObject.GetComponent<Image>();
            image?.fillColor = idleColor;

            _inspectorItem = gameObject.Parent?.GetComponent<InspectorItemController>();
            if (GetInspectedComponent() is null or InspectorGameObjectParameters)
            {
                gameObject.Dispose();
                return;
            }

            EnsureMenu();
        }

        public override void OnMouseDown()
        {
            if (_menu == null) return;

            if (_menu.IsActive)
            {
                HideMenu();
                return;
            }

            base.OnMouseDown();
            ShowMenu();
        }

        public override void Deactivate()
        {
            if (
                _menu != null &&
                (Input.MouseHoverTarget == null || !gameObject.IsParentFor(Input.MouseHoverTarget))
            )
            {
                HideMenu();
                return;
            }

            base.Deactivate();
        }


        private void EnsureMenu()
        {
            if (_menu != null) return;

            _menuPrefab ??= Game1.Prefabs["DropDownSubmenu"];
            _menuItemPrefab ??= Game1.Prefabs["DropDownSubmenuItem"];

            _menu = _menuPrefab.Copy();
            _menu.uiTransform.HorizontalAlignment = UITransform.AlignmentH.Right;
            _menu.uiTransform.VerticalAlignment = UITransform.AlignmentV.Bottom;
            _menu.uiTransform.origin = new Vector2(1, 0);
            _menu.IsActive = false;
            _menuColumn = _menu.GetComponent<Column>();
            gameObject.AddChild(_menu);
        }

        private void ShowMenu()
        {
            RebuildMenu();
            _menu.IsActive = true;
            isRedacting = true;
            image?.fillColor = onOpenedColor;
            _menu.RefreshBounds();
        }

        private void HideMenu()
        {
            _menu?.IsActive = false;
            StopInteracting();
        }

        private void RebuildMenu()
        {
            _menu.Children = [];

            GameComponent component = GetInspectedComponent();
            List<GameComponent> components = component?.gameObject?.Components ?? [];
            int componentIndex = component == null ? -1 : components.IndexOf(component);

            AddMenuItem("Copy values", true, CopyValues);
            AddMenuItem("Paste values", CanPasteValues(component), PasteValues);
            AddMenuItem("Move up", componentIndex > 0, MoveUp);
            AddMenuItem("Move down", componentIndex >= 0 && componentIndex < components.Count - 1, MoveDown);
            AddMenuItem("Delete", component != null, DeleteComponent);

            _menuColumn?.Refresh();
            _menu.RefreshBounds();
        }

        private void AddMenuItem(string text, bool isEnabled, Action action)
        {
            GameObject item = _menuItemPrefab.Copy();
            item.ObjectName = text;
            item.IsMouseTargetable = isEnabled;

            Text textComponent = item.GetComponent<Text>();
            if (textComponent != null)
            {
                textComponent.text = text;
                if (!isEnabled) textComponent.TextColor = new Color(140, 140, 140);
            }

            Image itemImage = item.GetComponent<Image>();
            if (itemImage != null && !isEnabled)
                itemImage.fillColor = new Color(35, 35, 35);

            Events.EventHandler eventHandler = item.GetComponent<Events.EventHandler>();
            if (eventHandler != null && isEnabled)
            {
                eventHandler.OnMouseUpEvent += () =>
                {
                    HideMenu();
                    action?.Invoke();
                };
            }

            _menu.AddChild(item);
        }

        private GameComponent GetInspectedComponent() =>
            _inspectorItem?.component;

        private static bool CanPasteValues(GameComponent component) =>
            component != null &&
            _copiedValues != null &&
            _copiedType == component.GetType();

        private void CopyValues()
        {
            GameComponent component = GetInspectedComponent();
            if (component == null) return;

            _copiedValues = component.Copy();
            _copiedType = component.GetType();
        }

        private void PasteValues()
        {
            GameComponent component = GetInspectedComponent();
            if (!CanPasteValues(component)) return;

            CopyInspectableMembers(_copiedValues, component);
            ApplyAndReload(component.gameObject);
        }

        private void MoveUp() => MoveComponent(-1);
        private void MoveDown() => MoveComponent(1);

        private void MoveComponent(int direction)
        {
            GameComponent component = GetInspectedComponent();
            GameObject owner = component?.gameObject;
            if (owner == null) return;

            List<GameComponent> components = owner.Components;
            int index = components.IndexOf(component);
            int targetIndex = index + direction;
            if (index < 0 || targetIndex < 0 || targetIndex >= components.Count) return;

            (components[index], components[targetIndex]) = (components[targetIndex], components[index]);
            owner.Components = components;
            ApplyAndReload(owner);
        }

        private void DeleteComponent()
        {
            GameComponent component = GetInspectedComponent();
            GameObject owner = component?.gameObject;
            if (owner == null) return;

            owner.RemoveComponent(component);
            ApplyAndReload(owner);
        }

        private static void CopyInspectableMembers(GameComponent source, GameComponent target)
        {
            Type type = source.GetType();

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                if (!CanCopyField(field)) continue;
                TryCopyMember(field.Name, () => field.SetValue(target, field.GetValue(source)));
            }

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                if (!CanCopyProperty(property)) continue;
                TryCopyMember(property.Name, () => property.SetValue(target, property.GetValue(source)));
            }
        }

        private static bool CanCopyField(FieldInfo field) =>
            !field.IsInitOnly &&
            field.GetCustomAttribute<DontSerialize>() == null &&
            field.GetCustomAttribute<HideInInspector>() == null;

        private static bool CanCopyProperty(PropertyInfo property) =>
            property.CanRead &&
            property.GetMethod?.IsPublic == true &&
            property.SetMethod?.IsPublic == true &&
            property.GetIndexParameters().Length == 0 &&
            property.GetCustomAttribute<DontSerialize>() == null &&
            property.GetCustomAttribute<HideInInspector>() == null;

        private static void TryCopyMember(string memberName, Action copy)
        {
            try
            {
                copy();
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log(
                    $"{memberName} value can't be pasted into component because of an error: {ex.Message}"
                );
            }
        }

        private static void ApplyAndReload(GameObject owner)
        {
            if (owner == null) return;

            owner.RefreshBounds();
            HierarchyWindowController.ApplyInspectorChanges();
            InspectorWindowController.LoadFor(owner);
        }
    }
}
