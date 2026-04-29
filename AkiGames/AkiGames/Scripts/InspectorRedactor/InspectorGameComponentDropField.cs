using System;
using System.Reflection;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI;

namespace AkiGames.Scripts.InspectorRedactor
{
    public class InspectorGameComponentDropField : InteractableComponent
    {
        public MemberInfo Info;
        public GameComponent Component;
        public Type ComponentType;
        public Text TextField;

        public override void Awake()
        {
            image = gameObject.GetComponent<Image>();
            image.fillColor = idleColor;
        }

        public bool TryApplyGameObject(GameObject linkedObject)
        {
            if (linkedObject == null || ComponentType == null) return false;

            GameComponent linkedComponent = linkedObject.GetComponent(ComponentType);
            if (linkedComponent == null)
            {
                ConsoleWindowController.Log(
                    $"{Info?.Name ?? "Component"} field of component {Component?.GetType().Name ?? "unknown"} can't be set from {linkedObject.ObjectName}: {ComponentType.Name} wasn't found."
                );
                return false;
            }

            try
            {
                switch (Info)
                {
                    case FieldInfo fieldInfo when fieldInfo.FieldType.IsAssignableFrom(linkedComponent.GetType()):
                        fieldInfo.SetValue(Component, linkedComponent);
                        break;
                    case PropertyInfo propertyInfo when
                        propertyInfo.PropertyType.IsAssignableFrom(linkedComponent.GetType()) &&
                        propertyInfo.SetMethod != null &&
                        propertyInfo.SetMethod.IsPublic:
                        propertyInfo.SetValue(Component, linkedComponent);
                        break;
                    default:
                        return false;
                }

                if (TextField != null)
                    TextField.text = GetDisplayName(linkedComponent);

                InspectorChangeApplier.Apply(Component);

                return true;
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log(
                    $"{Info?.Name ?? "Component"} field of component {Component?.GetType().Name ?? "unknown"} can't be set from dragged object because of an error: {ex.Message}"
                );
                return false;
            }
        }

        public static string GetDisplayName(GameComponent linkedComponent)
        {
            if (linkedComponent == null) return "null";

            string objectName = linkedComponent.gameObject?.ObjectName ?? "unknown";
            return $"{objectName}.{linkedComponent.GetType().Name}";
        }

        public override void OnMouseDown(){}
    }
}
