using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Content;
using Microsoft.Xna.Framework.Graphics;
using AkiGames.Core;
using AkiGames.UI;
using AkiGames.Scripts.InspectorRedactor;

namespace AkiGames.Scripts
{
    public class InspectorItemController : GameComponent
    {
        public GameComponent component;
        private GameObject content;

        private Color _inactiveColor = Color.Gray;

        private static readonly Dictionary<string, Texture2D> _icons = [];

        public override void Awake()
        {
            GameObject title = gameObject.Children[4];
            content = gameObject.Children[5];

            int height = 8;
            title.uiTransform.OffsetMin = new Vector2(0, height);
            Image image = title.Children[0].GetComponent<Image>();

            Type type = component.GetType();
            title.Children[1].GetComponent<Text>().text = type.Name;
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
                GameObject fieldObj = CreateFieldDescription(field, height, component);
                if (fieldObj != null) height += fieldObj.uiTransform.Height + 5;
            }
            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                if (property.GetCustomAttribute<HideInInspector>() != null) continue;
                GameObject propertyObj = CreateFieldDescription(property, height, component);
                if (propertyObj != null) height += propertyObj.uiTransform.Height + 5;
            }
            height += 8;
            gameObject.uiTransform.Height = height;
        }
        
        public static void LoadContent(ContentManager content)
        {
            _icons.Add("Transform", content.Load<Texture2D>("InspectorIcons/Transform_Component"));
            _icons.Add("Text", content.Load<Texture2D>("InspectorIcons/Text_Component"));
            _icons.Add("Image", content.Load<Texture2D>("InspectorIcons/Image_Component"));
            _icons.Add("Script", content.Load<Texture2D>("InspectorIcons/Script_Component"));
        }

        private GameObject CreateFieldDescription(MemberInfo memberInfo, int yOffset, GameComponent gameComponent)
        {
            string type = "";
            object value = null;
            bool isSettable = false;

            if (memberInfo is FieldInfo fieldInfo)
            {
                type = fieldInfo.FieldType.Name;
                value = fieldInfo.GetValue(gameComponent);
                isSettable = !fieldInfo.IsInitOnly;
            }
            if (memberInfo is PropertyInfo propertyInfo)
            {
                type = propertyInfo.PropertyType.Name;
                value = propertyInfo.GetValue(gameComponent);
                isSettable = propertyInfo.SetMethod != null && propertyInfo.SetMethod.IsPublic;
            }

            GameObject fieldDescription;
            switch (type)
            {
                case "Rectangle": return null;
                case "String":
                    fieldDescription = Game1.Prefabs["InspectorStringDescriptor"].Copy();
                    fieldDescription.uiTransform.OffsetMin = new Vector2(0, yOffset);
                    fieldDescription.Children[0].GetComponent<Text>().text = memberInfo.Name;

                    Text contentText = fieldDescription.Children[2].Children[0].Children[0].GetComponent<Text>();
                    if (!isSettable) contentText.TextColor = _inactiveColor;
                    contentText.text = $"{value}";
                    break;
                case "Vector2":
                    fieldDescription = Game1.Prefabs["InspectorVector2Descriptor"].Copy();
                    fieldDescription.uiTransform.OffsetMin = new Vector2(0, yOffset);
                    fieldDescription.Children[0].GetComponent<Text>().text = memberInfo.Name;

                    Image imageX = fieldDescription.Children[1].GetComponent<Image>();
                    Image imageY = fieldDescription.Children[2].GetComponent<Image>();
                    imageX.texture = Game1.UIImages["InputField"];
                    imageY.texture = Game1.UIImages["InputField"];
                    if (!isSettable)
                    {
                        imageX.fillColor = _inactiveColor;
                        imageY.fillColor = _inactiveColor;
                    }

                    Vector2 valueV2 = (Vector2)value;
                    imageX.gameObject.Children[0].GetComponent<Text>().text = $"{valueV2.X}";
                    imageY.gameObject.Children[0].GetComponent<Text>().text = $"{valueV2.Y}";
                    break;
                case "Color":
                    fieldDescription = Game1.Prefabs["InspectorColorDescriptor"].Copy();
                    fieldDescription.uiTransform.OffsetMin = new Vector2(0, yOffset);
                    fieldDescription.Children[0].GetComponent<Text>().text = memberInfo.Name;

                    Image image = fieldDescription.Children[1].GetComponent<Image>();
                    image.texture = Game1.UIImages["Round"];
                    image.fillColor = (Color)value;
                    break;
                case "Boolean":
                    fieldDescription = Game1.Prefabs["InspectorBoolDescriptor"].Copy();
                    fieldDescription.uiTransform.OffsetMin = new Vector2(0, yOffset);
                    fieldDescription.Children[0].GetComponent<Text>().text = memberInfo.Name;

                    GameObject checkboxObject = fieldDescription.Children[1];

                    image = checkboxObject.GetComponent<Image>();
                    image.texture = (bool)value ? Game1.UIImages["CheckboxApproved"] : Game1.UIImages["CheckboxEmpty"];
                    
                    InspectorCheckBox checkbox = checkboxObject.GetComponent<InspectorCheckBox>();
                    checkbox.value = (bool)value;
                    checkbox.Info = memberInfo;
                    checkbox.Component = gameComponent;
                    checkbox.isSettable = isSettable;
                    
                    if (!isSettable) image.fillColor = _inactiveColor;
                    break;
                default:
                    fieldDescription = Game1.Prefabs["InspectorFieldDescriptor"].Copy();
                    fieldDescription.uiTransform.OffsetMin = new Vector2(0, yOffset);
                    fieldDescription.Children[0].GetComponent<Text>().text = memberInfo.Name;

                    image = fieldDescription.Children[1].GetComponent<Image>();
                    if (!isSettable) image.fillColor = _inactiveColor;
                    image.texture = Game1.UIImages["InputField"];

                    image.gameObject.Children[0].GetComponent<Text>().text = value is null ? "null" : $"{value}";
                    break;
            }
            content.AddChild(fieldDescription);
            return fieldDescription;
        }
    }
}