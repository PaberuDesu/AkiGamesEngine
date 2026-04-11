using System;
using System.Reflection;
using AkiGames.Core;
using AkiGames.UI;

namespace AkiGames.Scripts.InspectorRedactor
{
    public class InspectorNumberInputField : NumberInputField
    {
        public MemberInfo Info { private get; set; } = null!;
        public GameComponent Component { private get; set; } = null!;

        protected override void EndRedacting()
        {
            base.EndRedacting();

            if (Info is FieldInfo fieldInfo)
            {
                if (isInteger)
                    fieldInfo.SetValue(Component, (int)Math.Round(result));
                else
                    fieldInfo.SetValue(Component, result);
            }
            if (Info is PropertyInfo propertyInfo)
            {
                if (isInteger)
                    propertyInfo.SetValue(Component, (int)Math.Round(result));
                else
                    propertyInfo.SetValue(Component, result);
            }
        }
    }
}