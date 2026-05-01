using System.Reflection;
using AkiGames.Core.Serialization;
using AkiGames.UI;

namespace AkiGames.Scripts.Inspector
{
    public class InspectorCheckBox : CheckBox
    {
        public MemberInfo Info { private get; set; }
        [HideInInspector] [DontSerialize] public GameComponent Component { private get; set; }

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
            InspectorChangeApplier.Apply(Component);
        }
    }
}
