using System;
using System.Collections.Generic;
using System.Reflection;
using Microsoft.Xna.Framework;
using AkiGames.UI;

namespace AkiGames.Scripts.Inspector
{
    public class InspectorDropDown : UI.DropDown.DropDown
    {
        private MemberInfo info = null;
        public MemberInfo Info
        {
            private get => info;
            set
            {
                info = value;
        
                // Получаем тип поля или свойства
                Type memberType = null;
                if (value is FieldInfo fieldInfo)
                {
                    memberType = fieldInfo.FieldType;
                }
                else if (value is PropertyInfo propertyInfo)
                {
                    memberType = propertyInfo.PropertyType;
                }

                // Если тип является enum, создаем словарь
                if (memberType != null && memberType.IsEnum)
                {
                    enumDictionary = [];
                    Array enumValues = Enum.GetValues(memberType);

                    foreach (var enumValue in enumValues)
                    {
                        string stringValue = enumValue.ToString();
                        enumDictionary[stringValue] = enumValue;
                    }
                }
                else
                {
                    enumDictionary = null;
                }
            }
        }
        public GameComponent Component { private get; set; }

        private Dictionary<string, object> enumDictionary;

        private Text text;

        public override void Awake()
        {
            base.Awake();
            submenu.uiTransform.HorizontalAlignment = UITransform.AlignmentH.Right;
            submenu.uiTransform.origin = new Vector2(1,0);
            text = gameObject.Children[0].GetComponent<Text>();
            ActionOnChoose = Choose;
        }

        private void Choose(string enumStringValue)
        {
            text.text = enumStringValue;
            object enumValue = enumDictionary[enumStringValue];

            if (Info is null || Component is null) return;
            if (Info is FieldInfo fieldInfo)
            {
                fieldInfo.SetValue(Component, enumValue);
            }
            if (Info is PropertyInfo propertyInfo)
            {
                propertyInfo.SetValue(Component, enumValue);
            }

            InspectorChangeApplier.Apply(Component);
        }
    }
}
