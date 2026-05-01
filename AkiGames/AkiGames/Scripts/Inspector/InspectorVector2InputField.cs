using System.Reflection;
using Microsoft.Xna.Framework;
using AkiGames.Core.Serialization;
using AkiGames.UI;

namespace AkiGames.Scripts.Inspector
{
    public class InspectorVector2InputField : NumberInputField
    {
        public MemberInfo Info { private get; set; }
        [HideInInspector] [DontSerialize] public GameComponent Component { private get; set; }
        public Coordinate coordinate;
        public enum Coordinate
        {
            X,
            Y
        }

        protected override void EndRedacting()
        {
            base.EndRedacting();
            if (Info is null || Component is null) return;
            if (Info is FieldInfo fieldInfo)
            {
                if (coordinate == Coordinate.X)
                {
                    fieldInfo.SetValue(Component, new Vector2(
                        result,
                        ((Vector2)fieldInfo.GetValue(Component)).Y
                    ));
                }
                else
                {
                    fieldInfo.SetValue(Component, new Vector2(
                        ((Vector2)fieldInfo.GetValue(Component)).X,
                        result
                    ));
                }
            }
            if (Info is PropertyInfo propertyInfo)
            {
                if (coordinate == Coordinate.X)
                {
                    propertyInfo.SetValue(Component, new Vector2(
                        result,
                        ((Vector2)propertyInfo.GetValue(Component)).Y
                    ));
                }
                else
                {
                    propertyInfo.SetValue(Component, new Vector2(
                        ((Vector2)propertyInfo.GetValue(Component)).X,
                        result
                    ));
                }
            }
            InspectorChangeApplier.Apply(Component);
        }
    }
}
