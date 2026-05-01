using System;
using System.IO;
using System.Reflection;
using Microsoft.Xna.Framework.Graphics;
using AkiGames.Core;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI;

namespace AkiGames.Scripts.Inspector
{
    public class InspectorTextureDropField : InteractableComponent
    {
        public MemberInfo Info;
        public GameComponent Component;
        public Text TextField;

        public override void Awake()
        {
            image = gameObject.GetComponent<Image>();
            image.fillColor = idleColor;
        }

        public bool TryApplyFile(string filePath)
        {
            if (!ContentFileUtility.IsImageFile(filePath) || !File.Exists(filePath))
                return false;

            Texture2D texture = Game1.LoadGameTexture(filePath);
            if (texture == null) return false;

            try
            {
                switch (Info)
                {
                    case FieldInfo fieldInfo when fieldInfo.FieldType == typeof(Texture2D):
                        fieldInfo.SetValue(Component, texture);
                        break;
                    case PropertyInfo propertyInfo when
                        propertyInfo.PropertyType == typeof(Texture2D) &&
                        propertyInfo.SetMethod != null &&
                        propertyInfo.SetMethod.IsPublic:
                        propertyInfo.SetValue(Component, texture);
                        break;
                    default:
                        return false;
                }

                if (TextField != null)
                    TextField.text = ContentFileUtility.GetDisplayName(filePath);

                InspectorChangeApplier.Apply(Component);

                return true;
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log(
                    $"{Info?.Name ?? "Texture"} field of component {Component?.GetType().Name ?? "unknown"} can't be set from dragged file because of an error: {ex.Message}"
                );
                return false;
            }
        }

        public override void OnMouseDown(){}
    }
}
