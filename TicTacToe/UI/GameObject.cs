using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.Xna.Framework;
using AkiGames.Core;
using static AkiGames.Events.Input;

namespace AkiGames.UI
{
    public class GameObject(string name) : GameStructure
    {
        public string ObjectName = name;
        public bool IsActive = true;
        public bool IsGlobalActive
        {
            get
            {
                if (Parent != null) return IsActive && Parent.IsGlobalActive;
                else return IsActive;
            }
        }
        public bool IsMouseTargetable = true;

        [DontSerialize, HideInInspector] public UITransform uiTransform = new();

        private double _lastClickTime = -DoubleClickThreshold;
        private const double DoubleClickThreshold = 500; // ms

        public GameObject Parent = null;

        private List<GameComponent> _components = [];
        public List<GameComponent> Components
        {
            get => [.. _components];
            set
            {
                _components = value;
                foreach (GameComponent component in value)
                {
                    component.gameObject = this;
                    component.uiTransform = uiTransform;
                }
            }
        }
        public T GetComponent<T>() where T : GameComponent =>
            Components.OfType<T>().FirstOrDefault();
        public void AddComponent(GameComponent component)
        {
            _components.Add(component);
            component.gameObject = this;
            component.uiTransform = uiTransform;
            if (_isAwakened) component.AkiGamesAwakeTree();
        }

        public event Action<GameObject> ChildAdded;
        private List<GameObject> _children = [];
        public List<GameObject> Children
        {
            get => [.. _children];
            set
            {
                _children = value;
                foreach (GameObject child in value)
                {
                    child.Parent = this;
                    ChildAdded?.Invoke(child);
                }
            }
        }
        public void AddChild(GameObject child)
        {
            _children.Add(child);
            child.Parent = this;
            if (_isAwakened) child.AkiGamesAwakeTree();
            ChildAdded?.Invoke(child);
        }

        private bool _isAwakened = false;
        public override void AkiGamesAwakeTree()
        {
            _isAwakened = true;
            base.AkiGamesAwakeTree();
            uiTransform.gameObject = this;
            foreach (GameObject child in Children) child.AkiGamesAwakeTree();
            foreach (GameComponent component in Components) component.AkiGamesAwakeTree();
        }

        public virtual void RefreshBounds(UITransform parentTransform = null)
        {
            parentTransform ??= Parent.uiTransform;
            uiTransform.RefreshBounds(parentTransform);
            RefreshContentBounds();
        }
        
        protected virtual void RefreshContentBounds()
        {
            foreach (var child in Children) child.RefreshBounds(uiTransform);
        }

        public bool IsParentFor(GameObject pretendingChild)
        {
            if (pretendingChild == this) return true;

            GameObject FamilyTreeMember = pretendingChild.Parent;
            while (FamilyTreeMember != null)
            {
                if (FamilyTreeMember == this)
                {
                    return true;
                }
                FamilyTreeMember = FamilyTreeMember.Parent;
            }
            return false;
        }

        public List<GameObject> GetAncestry()
        {
            List<GameObject> lineage = [];

            // Собираем цепочку от текущего объекта до корня
            GameObject current = this;
            while (current != null)
            {
                lineage.Add(current);
                current = current.Parent;
            }

            // Разворачиваем список для порядка [корень -> ... -> текущий]
            lineage.Reverse();
            return lineage;
        }

        private void HandleMouseEvent(Action<GameComponent> eventHandler)
        {
            foreach (GameComponent component in Components) eventHandler?.Invoke(component);
        }

        public override void OnMouseEnter() =>
            HandleMouseEvent(component => component.OnMouseEnter());
        public override void OnMouseExit() =>
            HandleMouseEvent(component => component.OnMouseExit());
        public override void OnMouseDown() =>
            HandleMouseEvent(component => component.OnMouseDown());
        public override void OnMouseUp()
        {
            HandleMouseEvent(component => component.OnMouseUp());

            double currentTime = gameTime.TotalGameTime.TotalMilliseconds;
            double elapsed = currentTime - _lastClickTime;
            // Проверяем двойной клик
            bool isDoubleClick = elapsed < DoubleClickThreshold;
            if (isDoubleClick) OnDoubleClick();
            _lastClickTime = currentTime;
        }
        public override void OnDoubleClick() =>
            HandleMouseEvent(component => component.OnDoubleClick());
        public override void OnMouseUpOutside() =>
            HandleMouseEvent(component => component.OnMouseUpOutside());
        public override void OnRMBUp() =>
            HandleMouseEvent(component => component.OnRMBUp());
        public override void Deactivate() =>
            HandleMouseEvent(component => component.Deactivate());
        public override void Drag(Vector2 cursorPosOnObj) =>
            HandleMouseEvent(component => component.Drag(cursorPosOnObj));
        public override void OnScroll(int scrollValue) =>
            HandleMouseEvent(component => component.OnScroll(scrollValue));
        public override void OnScrollFromOutsideTheObject(int scrollValue) =>
            HandleMouseEvent(component => component.OnScrollFromOutsideTheObject(scrollValue));

        public override void ProcessHotkey(HotKey hotkey) =>
            HandleMouseEvent(component => component.ProcessHotkey(hotkey));
        
        public virtual void SortByLayers()
        {
            Image image = GetComponent<Image>();
            if (image != null && image.Enabled) image.AddToLayer();
            Text text = GetComponent<Text>();
            if (text != null && text.Enabled) text.AddToLayer();
            foreach (var child in Children)
            {
                if (child.IsActive) child.SortByLayers();
            }
        }

        public GameObject Copy()
        {
            // Создаем поверхностную копию через MemberwiseClone
            var copy = (GameObject)MemberwiseClone();

            // Копируем элементы с глубоким копированием
            copy._components = [];
            copy.uiTransform = new UITransform();
            foreach (var component in _components)
            {
                if (component is UITransform uiTr)
                {
                    copy.uiTransform = uiTr.Copy();
                    break;
                }
            }
            copy.uiTransform.gameObject = copy;
            copy.AddComponent(copy.uiTransform);
            foreach (var component in _components)
            {
                if (component is UITransform) continue;
                GameComponent componentCopy = component.Copy();
                componentCopy.gameObject = copy;
                component.uiTransform = copy.uiTransform;
                copy.AddComponent(componentCopy);
            }

            // Копируем дочерние объекты с глубоким копированием
            copy._children = [];
            foreach (var child in _children)
            {
                GameObject childCopy = child.Copy();
                copy.AddChild(childCopy);
                childCopy.Parent = copy;
            }

            // Сбрасываем родителя и состояние
            copy.Parent = null;
            copy._isAwakened = false;

            return copy;
        }
    }
}
