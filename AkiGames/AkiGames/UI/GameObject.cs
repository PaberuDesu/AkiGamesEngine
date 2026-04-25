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
        public int ObjectID;
        [DontSerialize, HideInInspector] public ObjectIdSpace ObjectIDSpace = ObjectIdSpace.Main;
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

        private static readonly Dictionary<ObjectIdSpace, Dictionary<int, GameObject>> _objectsById = new()
        {
            [ObjectIdSpace.Main] = [],
            [ObjectIdSpace.Game] = []
        };
        private static readonly Dictionary<ObjectIdSpace, int> _nextObjectIds = new()
        {
            [ObjectIdSpace.Main] = 1,
            [ObjectIdSpace.Game] = 1
        };
        private static ObjectIdSpace _activeObjectIdSpace = ObjectIdSpace.Main;

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
        public void RemoveComponent(GameComponent component)
        {
            // Удаляем компонент из внутреннего списка
            if (!_components.Remove(component)) return;

            if (component is IDisposable disposable)
                disposable.Dispose();
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
                    child.SetObjectIdSpaceRecursive(ObjectIDSpace);
                    ChildAdded?.Invoke(child);
                }
            }
        }
        public void AddChild(GameObject child)
        {
            _children.Add(child);
            child.Parent = this;
            child.SetObjectIdSpaceRecursive(ObjectIDSpace);
            if (_isAwakened) child.AkiGamesAwakeTree();
            ChildAdded?.Invoke(child);
        }

        public void RemoveChild(GameObject child)
        {
            if (_children.Remove(child))
            {
                child.Parent = null;
            }
        }

        private bool _isAwakened = false;
        protected override ObjectIdSpace CurrentObjectIdSpace => ObjectIDSpace;
        public override void Awake() => EnsureUniqueObjectId();
        public override void AkiGamesAwakeTree()
        {
            _isAwakened = true;
            base.AkiGamesAwakeTree();
            uiTransform.gameObject = this;
            foreach (GameObject child in Children) child.AkiGamesAwakeTree();
            foreach (GameComponent component in Components) component.AkiGamesAwakeTree();
        }

        public void AkiGamesAwakeTree(ObjectIdSpace objectIdSpace)
        {
            SetObjectIdSpaceRecursive(objectIdSpace);
            AkiGamesAwakeTree();
        }

        public void AkiGamesEditorAwakeTree(ObjectIdSpace objectIdSpace)
        {
            SetObjectIdSpaceRecursive(objectIdSpace);
            AkiGamesEditorAwakeTree();
        }

        public void AkiGamesEditorAwakeTree()
        {
            EnsureUniqueObjectId();
            uiTransform.gameObject = this;

            foreach (GameComponent component in _components)
            {
                component.gameObject = this;
                component.uiTransform = uiTransform;
            }

            foreach (GameObject child in _children)
            {
                child.Parent = this;
                child.AkiGamesEditorAwakeTree();
            }
        }

        public void SetObjectIdSpaceRecursive(ObjectIdSpace objectIdSpace)
        {
            ObjectIDSpace = objectIdSpace;
            foreach (GameObject child in _children)
            {
                child.SetObjectIdSpaceRecursive(objectIdSpace);
            }
        }

        private void EnsureUniqueObjectId()
        {
            Dictionary<int, GameObject> objectsById = _objectsById[ObjectIDSpace];
            int nextObjectId = _nextObjectIds[ObjectIDSpace];

            if (ObjectID > 0 &&
                objectsById.TryGetValue(ObjectID, out GameObject existingObject) &&
                existingObject == this)
            {
                if (ObjectID >= nextObjectId) _nextObjectIds[ObjectIDSpace] = ObjectID + 1;
                return;
            }

            if (ObjectID > 0 && !objectsById.ContainsKey(ObjectID))
            {
                objectsById[ObjectID] = this;
                if (ObjectID >= nextObjectId) _nextObjectIds[ObjectIDSpace] = ObjectID + 1;
                return;
            }

            while (objectsById.ContainsKey(nextObjectId))
            {
                nextObjectId++;
            }

            ObjectID = nextObjectId;
            objectsById[ObjectID] = this;
            _nextObjectIds[ObjectIDSpace] = nextObjectId + 1;
        }

        public void EnsureUniqueObjectIdsInTree()
        {
            HashSet<int> usedObjectIds = [];
            int nextObjectId = 1;
            EnsureUniqueObjectIdsInTree(usedObjectIds, ref nextObjectId);
        }

        private void EnsureUniqueObjectIdsInTree(HashSet<int> usedObjectIds, ref int nextObjectId)
        {
            if (ObjectID <= 0 || !usedObjectIds.Add(ObjectID))
            {
                while (usedObjectIds.Contains(nextObjectId))
                {
                    nextObjectId++;
                }

                ObjectID = nextObjectId;
                usedObjectIds.Add(ObjectID);
            }

            if (ObjectID >= nextObjectId)
            {
                nextObjectId = ObjectID + 1;
            }

            foreach (GameObject child in _children)
            {
                child.EnsureUniqueObjectIdsInTree(usedObjectIds, ref nextObjectId);
            }
        }

        public static IDisposable UseObjectIdSpace(ObjectIdSpace objectIdSpace)
        {
            ObjectIdSpace previousObjectIdSpace = _activeObjectIdSpace;
            _activeObjectIdSpace = objectIdSpace;
            return new ObjectIdSpaceScope(previousObjectIdSpace);
        }

        public static GameObject FindById(int objectId) =>
            FindById(objectId, _activeObjectIdSpace);

        public static GameObject FindById(int objectId, ObjectIdSpace objectIdSpace)
        {
            if (objectId <= 0) return null;
            Dictionary<int, GameObject> objectsById = _objectsById[objectIdSpace];
            return objectsById.TryGetValue(objectId, out GameObject gameObject) ? gameObject : null;
        }

        private class ObjectIdSpaceScope(ObjectIdSpace previousObjectIdSpace) : IDisposable
        {
            public void Dispose()
            {
                _activeObjectIdSpace = previousObjectIdSpace;
            }
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

        public GameObject Copy(bool preserveObjectId = false)
        {
            // Создаем поверхностную копию через MemberwiseClone
            var copy = (GameObject)MemberwiseClone();
            copy._isAwakened = false;

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
                componentCopy.uiTransform = copy.uiTransform;
                copy.AddComponent(componentCopy);
            }

            // Копируем дочерние объекты с глубоким копированием
            copy._children = [];
            foreach (var child in _children)
            {
                GameObject childCopy = child.Copy(preserveObjectId);
                copy.AddChild(childCopy);
                childCopy.Parent = copy;
            }

            // Сбрасываем родителя и состояние
            copy.Parent = null;
            if (!preserveObjectId) copy.ObjectID = 0;
            copy._isAwakened = false;

            return copy;
        }

        public override void Dispose()
        {
            if (ObjectID > 0 &&
                _objectsById[ObjectIDSpace].TryGetValue(ObjectID, out GameObject existingObject) &&
                existingObject == this)
            {
                _objectsById[ObjectIDSpace].Remove(ObjectID);
            }

            // Отвязываемся от родителя
            Parent?.RemoveChild(this);

            // Рекурсивно удаляем детей
            foreach (var child in _children.ToList())
                child.Dispose();

            // Удаляем все компоненты
            foreach (var component in _components.ToList())
                component.Dispose();

            // Очищаем списки
            _children.Clear();
            _components.Clear();

            base.Dispose();
        }
    }
}
