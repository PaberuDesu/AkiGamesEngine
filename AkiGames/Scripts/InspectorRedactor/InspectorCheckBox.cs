using System.Reflection;
using AkiGames.Core;

namespace AkiGames.Scripts.InspectorRedactor
{
    public class InspectorCheckBox : UI.CheckBox
    {
        public MemberInfo Info { private get; set; } = null!;
        public GameComponent Component { private get; set; } = null!;

        protected override void ChangeValue()
        {
            base.ChangeValue();
            if (Info is null || Component is null) return;
            if (Info is FieldInfo fieldInfo)
            {
                fieldInfo.SetValue(Component, value);
            }
            if (Info is PropertyInfo propertyInfo)
            {
                propertyInfo.SetValue(Component, value);
            }
            Component.gameObject.uiTransform.RefreshBounds();
        }
    }
}