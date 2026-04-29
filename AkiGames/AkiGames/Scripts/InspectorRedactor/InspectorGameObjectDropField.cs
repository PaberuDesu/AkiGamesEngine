using System;
using System.Reflection;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI;

namespace AkiGames.Scripts.InspectorRedactor
{
    public class InspectorGameObjectDropField : InteractableComponent
    {
        public MemberInfo Info;
        public GameComponent Component;
        public Text TextField;

        public override void Awake()
        {
            image = gameObject.GetComponent<Image>();
            image.fillColor = idleColor;
        }

        public bool TryApplyGameObject(GameObject linkedObject)
        {
            if (linkedObject == null) return false;

            try
            {
                switch (Info)
                {
                    case FieldInfo fieldInfo when fieldInfo.FieldType == typeof(GameObject):
                        fieldInfo.SetValue(Component, linkedObject);
                        break;
                    case PropertyInfo propertyInfo when
                        propertyInfo.PropertyType == typeof(GameObject) &&
                        propertyInfo.SetMethod != null &&
                        propertyInfo.SetMethod.IsPublic:
                        propertyInfo.SetValue(Component, linkedObject);
                        break;
                    default:
                        return false;
                }

                if (TextField != null)
                    TextField.text = GetDisplayName(linkedObject);

                return true;
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log(
                    $"{Info?.Name ?? "GameObject"} field of component {Component?.GetType().Name ?? "unknown"} can't be set from dragged object because of an error: {ex.Message}"
                );
                return false;
            }
        }

        public static string GetDisplayName(GameObject linkedObject) =>
            linkedObject == null ? "null" : linkedObject.ObjectName;

        public override void OnMouseDown(){}
    }
}
