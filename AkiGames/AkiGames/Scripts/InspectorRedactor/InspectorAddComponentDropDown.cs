using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using AkiGames.Core;
using AkiGames.Events;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI;
using AkiGames.UI.ScrollableList;
using Microsoft.Xna.Framework;

namespace AkiGames.Scripts.InspectorRedactor
{
    public class InspectorAddComponentDropDown : UI.DropDown.DropDown
    {
        private const int ItemHeight = 25;
        private const int MaxVisibleItems = 8;

        [DontSerialize, HideInInspector] public GameObject TargetObject { private get; set; }

        private static GameObject _submenuPrefab;
        private static GameObject _submenuItemPrefab;

        private readonly Dictionary<string, Type> _componentTypesByName = [];
        private ScrollableListController _listController;
        private Text _text;

        public override void Awake()
        {
            idleColor = new Color(55, 55, 55);
            onHoverColor = new Color(66, 66, 66);
            onOpenedColor = new Color(76, 76, 76);

            _submenuItemPrefab ??= Game1.Prefabs["DropDownSubmenuItem"];
            image = gameObject.GetComponent<Image>();
            _text = gameObject.Children[0].GetComponent<Text>();

            image.fillColor = idleColor;

            CreateScrollableSubmenu();
            RebuildMenu();
        }

        public override void Update()
        {
            base.Update();

            if (
                submenu?.IsActive == true &&
                Input.DeltaScroll != 0 &&
                submenu.uiTransform.Contains(Input.mousePosition)
            )
            {
                _listController?.Scroll(Input.DeltaScroll);
            }
        }

        public override void OnMouseDown()
        {
            if (_componentTypesByName.Count == 0) return;

            RebuildMenu();
            base.OnMouseDown();
            if (submenu.IsActive)
                RefreshSubmenuLayout();
        }

        private void CreateScrollableSubmenu()
        {
            _submenuPrefab ??= Game1.Prefabs["ScrollableList"];
            submenu = _submenuPrefab.Copy();
            submenu.IsActive = false;
            ConfigureSubmenuTransform();
            submenu.GetComponent<Image>().fillColor = new Color(55, 55, 55);
            gameObject.AddChild(submenu);

            GameObject mask = submenu.Children[0];
            _listController = mask.Children[0].GetComponent<ScrollableListController>();
        }

        private void ConfigureSubmenuTransform()
        {
            UITransform transform = submenu.uiTransform;
            transform.OffsetMin = new Vector2(0, -4);
            transform.OffsetMax = Vector2.Zero;
            transform.Width = 0;
            transform.Height = ItemHeight;
            transform.HorizontalAlignment = UITransform.AlignmentH.Stretch;
            transform.VerticalAlignment = UITransform.AlignmentV.Top;
            transform.origin = new Vector2(0, 1);
        }

        private void RebuildMenu()
        {
            ClearMenuItems();
            _componentTypesByName.Clear();

            List<Type> componentTypes = [.. GetAddableComponentTypes(TargetObject)
                .OrderBy(type => ProjectScriptLoader.IsProjectScriptAssembly(type.Assembly) ? 0 : 1)
                .ThenBy(type => type.Name)
                .ThenBy(type => type.FullName)];

            Dictionary<string, int> typeNameCounts = componentTypes
                .GroupBy(type => type.Name)
                .ToDictionary(group => group.Key, group => group.Count());

            foreach (Type type in componentTypes)
            {
                string menuName = typeNameCounts[type.Name] == 1 ? type.Name : type.FullName;
                _componentTypesByName[menuName] = type;
            }

            if (_componentTypesByName.Count == 0)
            {
                _text.text = "No components";
                gameObject.IsMouseTargetable = false;
                image.fillColor = new Color(80, 80, 80);
                return;
            }

            _text.text = "Add Component";

            foreach (string menuName in _componentTypesByName.Keys)
            {
                GameObject item = _submenuItemPrefab.Copy();
                item.ObjectName = menuName;
                ConfigureMenuItemText(item, menuName);

                Events.EventHandler eventHandler = item.GetComponent<Events.EventHandler>();
                if (eventHandler != null)
                {
                    string currentMenuName = menuName;
                    eventHandler.OnMouseUpEvent += () => AddComponent(currentMenuName);
                }

                _listController.gameObject.AddChild(item);
            }

            _listController.Refresh();
            RefreshSubmenuLayout();
        }

        private static void ConfigureMenuItemText(GameObject item, string menuName)
        {
            Text text = item.GetComponent<Text>();
            if (text == null) return;

            text.text = menuName;
            text.HorizontalWrap = Text.WrapModeH.DotsAfter;
            text.HorizontalAlignment = Text.AlignmentH.Center;

            UITransform textTransform = text.uiTransform;
            textTransform.OffsetMin = new Vector2(8, 0);
            textTransform.OffsetMax = new Vector2(8, 0);
            textTransform.HorizontalAlignment = UITransform.AlignmentH.Stretch;
            textTransform.anchorLeftTop = new Vector2(0, textTransform.anchorLeftTop.Y);
            textTransform.anchorRightBottom = new Vector2(1, textTransform.anchorRightBottom.Y);
        }

        private void ClearMenuItems()
        {
            if (_listController == null) return;

            foreach (GameObject child in _listController.gameObject.Children)
                child.Dispose();

            _listController.gameObject.Children = [];
        }

        private void RefreshSubmenuLayout()
        {
            if (submenu == null || _listController == null) return;

            int visibleItems = Math.Clamp(_componentTypesByName.Count, 1, MaxVisibleItems);
            submenu.uiTransform.Height = visibleItems * ItemHeight;
            submenu.RefreshBounds(gameObject.uiTransform);
            _listController.Refresh();
            _listController.Update();
        }

        private void AddComponent(string menuName)
        {
            Hide();

            if (TargetObject == null || !_componentTypesByName.TryGetValue(menuName, out Type componentType))
                return;

            if (componentType == typeof(UITransform) && TargetObject.GetComponent<UITransform>() != null)
            {
                ConsoleWindowController.Log($"{TargetObject.ObjectName} already has UITransform component.");
                return;
            }

            try
            {
                GameComponent component = (GameComponent)Activator.CreateInstance(componentType);
                if (component is UITransform uiTransform)
                    TargetObject.uiTransform = uiTransform;

                TargetObject.AddComponent(component);
                HierarchyWindowController.ApplyInspectorChanges();
                InspectorWindowController.LoadFor(TargetObject);
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log($"Component {componentType.Name} can't be added: {ex.Message}");
            }
        }

        private static IEnumerable<Type> GetAddableComponentTypes(GameObject targetObject)
        {
            HashSet<Type> componentTypes = [];

            foreach (Type type in ProjectScriptLoader.GetActiveComponentTypes())
                componentTypes.Add(type);

            foreach (Assembly assembly in AppDomain.CurrentDomain.GetAssemblies())
            {
                if (ProjectScriptLoader.IsProjectScriptAssembly(assembly))
                    continue;

                foreach (Type type in GetLoadableTypes(assembly))
                {
                    if (IsBuiltInAddableComponent(type) || IsActiveEditorScriptComponent(type))
                        componentTypes.Add(type);
                }
            }

            return componentTypes.Where(type =>
                IsCreatableComponentType(type) &&
                (type != typeof(UITransform) || targetObject?.GetComponent<UITransform>() == null)
            );
        }

        private static bool IsBuiltInAddableComponent(Type type)
        {
            string namespaceName = type.Namespace ?? "";
            return namespaceName.StartsWith("AkiGames.UI", StringComparison.Ordinal) ||
                   type == typeof(Events.EventHandler);
        }

        private static bool IsActiveEditorScriptComponent(Type type)
        {
            if (!ProjectScriptLoader.ActiveProjectIsRunningEditor)
                return false;

            string namespaceName = type.Namespace ?? "";
            return namespaceName.StartsWith("AkiGames.Scripts", StringComparison.Ordinal);
        }

        private static bool IsCreatableComponentType(Type type) =>
            type != null &&
            type != typeof(InspectorGameObjectParameters) &&
            typeof(GameComponent).IsAssignableFrom(type) &&
            !type.IsAbstract &&
            type.GetConstructor(Type.EmptyTypes) != null;

        private static IEnumerable<Type> GetLoadableTypes(Assembly assembly)
        {
            try
            {
                return assembly.GetTypes();
            }
            catch (ReflectionTypeLoadException ex)
            {
                return ex.Types.Where(type => type != null);
            }
        }
    }
}
