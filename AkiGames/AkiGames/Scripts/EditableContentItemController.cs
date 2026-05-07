using AkiGames.Events;
using AkiGames.UI;
using Microsoft.Xna.Framework;

namespace AkiGames.Scripts
{
    public class EditableContentItemController : ContentItemController
    {
        private readonly TextEditSession _renameEditor = new();
        private bool _isDragging;

        protected bool IsRenaming { get; private set; }
        protected bool IsDragging => _isDragging;

        public override void Update() => UpdateRenaming();

        public override void OnDoubleClick()
        {
            if (IsRenaming) return;
            base.OnDoubleClick();
        }

        public override void Deactivate()
        {
            if (IsRenaming)
            {
                CommitRenaming();
                return;
            }

            base.Deactivate();
        }

        public override void Drag(Vector2 cursorPosOnObj)
        {
            if (IsRenaming || !CanStartDragging()) return;

            if (!_isDragging)
            {
                _isDragging = true;
                OnDragStarted();
            }

            if (IsCursorInsideLocalDragArea())
                UpdateLocalDrag(cursorPosOnObj);
            else
                UpdateOuterDrag(cursorPosOnObj);
        }

        public override void OnMouseUpOutside() => OnMouseUp();

        public override void OnMouseUp()
        {
            if (!_isDragging) return;

            if (IsCursorInsideLocalDragArea())
                CompleteLocalDrag();
            else
                CompleteOuterDrag();

            _isDragging = false;
            OnDragEnded();
        }

        public virtual void StartRenaming()
        {
            if (!CanStartRenaming()) return;

            BeforeStartRenaming();
            _renameEditor.Begin(GetRenameInitialValue());
            IsRenaming = true;
            SelectItem();
            UpdateDisplayedName();
        }

        protected virtual bool CanStartRenaming() => true;
        protected virtual void BeforeStartRenaming() { }
        protected virtual string GetRenameInitialValue() => GetDisplayName();
        protected virtual string GetRenameSuffix() => "";
        protected virtual string GetDisplayName() => Name;
        protected virtual string FormatDisplayedName(string visibleName) => visibleName;
        protected virtual bool OnRenameCommitted(string newName) => true;
        protected virtual bool OnRenameCancelled() => true;

        protected virtual bool CanStartDragging() => false;
        protected virtual bool IsCursorInsideLocalDragArea() => false;
        protected virtual void OnDragStarted() { }
        protected virtual void UpdateLocalDrag(Vector2 cursorPosOnObj) { }
        protected virtual void UpdateOuterDrag(Vector2 cursorPosOnObj) { }
        protected virtual void CompleteLocalDrag() { }
        protected virtual void CompleteOuterDrag() { }
        protected virtual void OnDragEnded() { }

        protected void UpdateDisplayedName()
        {
            if (Title == null) return;

            double timeMs = gameTime?.TotalGameTime.TotalMilliseconds ?? 0;
            string visibleName = IsRenaming ?
                _renameEditor.DisplayValue(timeMs) + GetRenameSuffix() :
                GetDisplayName();

            Title.text = FormatDisplayedName(visibleName);
        }

        private void UpdateRenaming()
        {
            if (!IsRenaming) return;

            if (
                Input.LMB.IsDown &&
                Input.MouseHoverTarget != gameObject &&
                !gameObject.IsParentFor(Input.MouseHoverTarget)
            )
            {
                CommitRenaming();
                return;
            }

            switch (_renameEditor.Update())
            {
                case TextEditAction.Cancel:
                    CancelRenaming();
                    return;
                case TextEditAction.Commit:
                    CommitRenaming();
                    return;
            }

            UpdateDisplayedName();
        }

        private void CommitRenaming()
        {
            string newName = _renameEditor.Value.Trim();
            _renameEditor.Finish();
            IsRenaming = false;

            if (OnRenameCommitted(newName))
                UpdateDisplayedName();
        }

        private void CancelRenaming()
        {
            _renameEditor.Cancel();
            IsRenaming = false;

            if (OnRenameCancelled())
                UpdateDisplayedName();
        }
    }
}
