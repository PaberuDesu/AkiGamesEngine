global using GameComponent = AkiGames.Core.GameStructures.GameComponent;

using System;
using System.Reflection;
using AkiGames.Core.Serialization;
using AkiGames.Scripts;
using AkiGames.UI;

namespace AkiGames.Core.GameStructures
{
    public abstract class GameComponent : GameStructure
    {
        public bool Enabled = true;
        [DontSerialize, HideInInspector] public GameObject gameObject;
        [DontSerialize, HideInInspector] public UITransform uiTransform;
        protected override ObjectIdSpace CurrentObjectIdSpace =>
            gameObject?.ObjectIDSpace ?? ObjectIdSpace.Main;
        public override void Update() { if (Enabled) base.Update(); }

        public virtual GameComponent Copy()
        {
            var copy = (GameComponent)Activator.CreateInstance(GetType());
            CopySerializedMembersTo(copy);
            copy.gameObject = null;
            copy.uiTransform = null;
            return copy;
        }

        protected void CopySerializedMembersTo(GameComponent copy)
        {
            Type type = GetType();

            foreach (FieldInfo field in type.GetFields(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                if (!CanCopyField(field)) continue;
                TryCopyMember(() => field.SetValue(copy, field.GetValue(this)));
            }

            foreach (PropertyInfo property in type.GetProperties(BindingFlags.Public | BindingFlags.Instance | BindingFlags.FlattenHierarchy))
            {
                if (!CanCopyProperty(property)) continue;
                TryCopyMember(() => property.SetValue(copy, property.GetValue(this)));
            }
        }

        private static bool CanCopyField(FieldInfo field) =>
            !field.IsInitOnly &&
            field.GetCustomAttribute<DontSerialize>() == null;

        private static bool CanCopyProperty(PropertyInfo property) =>
            property.CanRead &&
            property.CanWrite &&
            property.GetIndexParameters().Length == 0 &&
            property.GetCustomAttribute<DontSerialize>() == null;

        private static void TryCopyMember(Action copy)
        {
            try
            {
                copy();
            }
            catch { }
        }

        public override void OnScrollFromOutsideTheObject(int scrollValue) => OnScroll(scrollValue);

        public override void Dispose()
        {
            // Удаляем компонент из родительского объекта
            GameObject owner = gameObject;
            gameObject = null;
            owner?.RemoveComponent(this);
    
            base.Dispose();
        }
    }
}
