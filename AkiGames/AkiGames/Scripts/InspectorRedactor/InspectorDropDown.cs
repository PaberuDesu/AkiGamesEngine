using System.Reflection;
using AkiGames.UI;
using Microsoft.Xna.Framework;

namespace AkiGames.Scripts.InspectorRedactor
{
    public class InspectorDropDown : UI.DropDown.DropDown
    {
        public MemberInfo Info { private get; set; }
        public GameComponent Component { private get; set; }

        public override void Awake()
        {
            base.Awake();
            submenu.uiTransform.HorizontalAlignment = UITransform.AlignmentH.Right;
            submenu.uiTransform.origin = new Vector2(1,0);
        }

        //protected override void ChangeValue()
        //{
        //    base.ChangeValue();
        //    if (Info is null || Component is null) return;
        //    if (Info is FieldInfo fieldInfo)
        //    {
        //        fieldInfo.SetValue(Component, value);
        //    }
        //    if (Info is PropertyInfo propertyInfo)
        //    {
        //        propertyInfo.SetValue(Component, value);
        //    }
        //    Component.gameObject.uiTransform.RefreshBounds();
        //}
    }
}