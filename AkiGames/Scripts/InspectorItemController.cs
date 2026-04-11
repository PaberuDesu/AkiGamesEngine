using System.Reflection;
using AkiGames.Core;
using AkiGames.Scripts.InspectorRedactor;
using AkiGames.UI;
using Color = AkiGames.Core.Color;
using Image = AkiGames.UI.Image;

namespace AkiGames.Scripts
{
    public class InspectorItemController : GameComponent
    {
        public GameComponent component = null!;
        private GameObject content = null!;

        private Color _inactiveColor = Color.Gray;

        private static readonly Dictionary<string, Veldrid.Texture> _icons = [];

        public override void Awake()
        {
            GameObject title = gameObject.Children[4];
            content = gameObject.Children[5];

            int height = 8;
            title.uiTransform.OffsetMin = new Vector2(0, height);
            Image image = title.Children[0].GetComponent<Image>()!;

            Type type = component.GetType();
            title.Children[1].GetComponent<Text>()!.text = type.Name;
            image.texture = type.Name switch
            {
                "UITransform" => _icons["Transform"],
                "Text" => _icons["Text"],
                "Image" => _icons["Image"],
                _ => _icons["Script"]
            };

            height += 35;
            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                if (field.GetCustomAttribute<HideInInspector>() != null) continue;
                GameObject? fieldObj = CreateFieldDescription(field, height, component);
                if (fieldObj != null) height += fieldObj.uiTransform.Height + 5;
            }
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                if (
                    !property.CanRead || !property.GetMethod!.IsPublic ||
                    property.GetCustomAttribute<HideInInspector>() != null
                ) continue;
                GameObject? propertyObj = CreateFieldDescription(property, height, component);
                if (propertyObj != null) height += propertyObj.uiTransform.Height + 5;
            }
            height += 8;
            gameObject.uiTransform.Height = height;
        }
        
        public static void LoadContent()
        {
            _icons.Add("Transform", VeldridGame.UIImages.GetValueOrDefault("InspectorIcons/Transform_Component")!);
            _icons.Add("Text", VeldridGame.UIImages.GetValueOrDefault("InspectorIcons/Text_Component")!);
            _icons.Add("Image", VeldridGame.UIImages.GetValueOrDefault("InspectorIcons/Image_Component")!);
            _icons.Add("Script", VeldridGame.UIImages.GetValueOrDefault("InspectorIcons/Script_Component")!);
        }

        private GameObject? CreateFieldDescription(MemberInfo memberInfo, int yOffset, GameComponent gameComponent)
        {
            string type = "";
            object? value = null;
            bool isSettable = false;
            Array enumValuesArray = Array.Empty<object>();

            if (memberInfo is FieldInfo fieldInfo)
            {
                type = fieldInfo.FieldType.Name;
                value = fieldInfo.GetValue(gameComponent)!;
                isSettable = !fieldInfo.IsInitOnly;
                if (fieldInfo.FieldType.IsEnum) enumValuesArray = Enum.GetValues(fieldInfo.FieldType);
            }
            if (memberInfo is PropertyInfo propertyInfo)
            {
                type = propertyInfo.PropertyType.Name;
                value = propertyInfo.GetValue(gameComponent)!;
                isSettable = propertyInfo.SetMethod != null && propertyInfo.SetMethod.IsPublic;
                if (propertyInfo.PropertyType.IsEnum) enumValuesArray = Enum.GetValues(propertyInfo.PropertyType);
            }

            GameObject fieldDescription;
            switch (type)
            {
                case "Rectangle": return null;
                case "Nullable`1": return null;
                case "String":
                    fieldDescription = VeldridGame.Prefabs["InspectorStringDescriptor"].Copy();
                    fieldDescription.uiTransform.OffsetMin = new Vector2(0, yOffset);
                    fieldDescription.Children[0].GetComponent<Text>()!.text = memberInfo.Name;

                    Text contentText = fieldDescription.Children[2].Children[0].Children[0].GetComponent<Text>()!;
                    if (!isSettable) contentText.TextColor = _inactiveColor;
                    contentText.text = $"{value}";
                    break;
                case "Vector2":
                    fieldDescription = VeldridGame.Prefabs["InspectorVector2Descriptor"].Copy();
                    fieldDescription.uiTransform.OffsetMin = new Vector2(0, yOffset);
                    fieldDescription.Children[0].GetComponent<Text>()!.text = memberInfo.Name;

                    Image imageX = fieldDescription.Children[1].GetComponent<Image>()!;
                    Image imageY = fieldDescription.Children[2].GetComponent<Image>()!;

                    imageX.texture = VeldridGame.UIImages.GetValueOrDefault("UI/InputField");
                    imageY.texture = imageX.texture;

                    if (!isSettable)
                    {
                        imageX.gameObject.IsMouseTargetable = false;
                        imageX.fillColor = _inactiveColor;
                        imageY.gameObject.IsMouseTargetable = false;
                        imageY.fillColor = _inactiveColor;
                    }

                    Vector2 valueV2 = (Vector2)value!;
                    imageX.gameObject.Children[0].GetComponent<Text>()!.text = $"{valueV2.X}";
                    imageY.gameObject.Children[0].GetComponent<Text>()!.text = $"{valueV2.Y}";
                    
                    InspectorVector2InputField inputX = imageX.gameObject.GetComponent<InspectorVector2InputField>()!;
                    inputX.Info = memberInfo;
                    inputX.Component = gameComponent;
                    inputX.coordinate = InspectorVector2InputField.Coordinate.X;
                    InspectorVector2InputField inputY = imageY.gameObject.GetComponent<InspectorVector2InputField>()!;
                    inputY.Info = memberInfo;
                    inputY.Component = gameComponent;
                    inputY.coordinate = InspectorVector2InputField.Coordinate.Y;
                    break;
                case "Color":
                    fieldDescription = VeldridGame.Prefabs["InspectorColorDescriptor"].Copy();
                    fieldDescription.uiTransform.OffsetMin = new Vector2(0, yOffset);
                    fieldDescription.Children[0].GetComponent<Text>()!.text = memberInfo.Name;

                    Image image = fieldDescription.Children[1].GetComponent<Image>()!;
                    image.texture = VeldridGame.UIImages.GetValueOrDefault("UI/Round");
                    image.fillColor = (Color)value!;
                    break;
                case "Boolean":
                    fieldDescription = VeldridGame.Prefabs["InspectorBoolDescriptor"].Copy();
                    fieldDescription.uiTransform.OffsetMin = new Vector2(0, yOffset);
                    fieldDescription.Children[0].GetComponent<Text>()!.text = memberInfo.Name;

                    GameObject checkboxObject = fieldDescription.Children[1];

                    image = checkboxObject.GetComponent<Image>()!;
                    image.texture = VeldridGame.UIImages.GetValueOrDefault((bool)value! ? "UI/CheckboxApproved" : "UI/CheckboxEmpty");
                    
                    InspectorCheckBox checkbox = checkboxObject.GetComponent<InspectorCheckBox>()!;
                    checkbox.value = (bool)value;
                    checkbox.Info = memberInfo;
                    checkbox.Component = gameComponent;
                    checkbox.gameObject.IsMouseTargetable = isSettable;
                    
                    if (!isSettable) image.fillColor = _inactiveColor;
                    break;
                default:
                    fieldDescription = VeldridGame.Prefabs["InspectorFieldDescriptor"].Copy();
                    fieldDescription.uiTransform.OffsetMin = new Vector2(0, yOffset);
                    fieldDescription.Children[0].GetComponent<Text>()!.text = memberInfo.Name;

                    image = fieldDescription.Children[1].GetComponent<Image>()!;
                    if (!isSettable)
                    {
                        image.fillColor = _inactiveColor;
                        image.gameObject.IsMouseTargetable = false;
                    }
                    image.texture = VeldridGame.UIImages.GetValueOrDefault("UI/InputField");

                    image.gameObject.Children[0].GetComponent<Text>()!.text = value is null ? "null" : $"{value}";
                    
                    if (enumValuesArray.Length > 0)
                    {
                        List<string> menuItemNames = [];
                        foreach (object itemName in enumValuesArray)
                        {
                            menuItemNames.Add($"{itemName}");
                        }
                        fieldDescription.Children[1].AddComponent(new InspectorDropDown()
                        {
                            menuItems = menuItemNames,
                            Info = memberInfo,
                            Component = gameComponent
                        });
                    }
                    else if (type == "Single")
                    {
                        fieldDescription.Children[1].AddComponent(new InspectorNumberInputField()
                        {
                            Info = memberInfo,
                            Component = gameComponent
                        });
                    }
                    else if (type == "Int32")
                    {
                        fieldDescription.Children[1].AddComponent(new InspectorNumberInputField()
                        {
                            Info = memberInfo,
                            Component = gameComponent,
                            isInteger = true
                        });
                    }
                    break;
            }
            content.AddChild(fieldDescription);
            return fieldDescription;
        }
    }
}