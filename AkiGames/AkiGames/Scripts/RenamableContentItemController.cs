using AkiGames.Events;
using AkiGames.UI;

namespace AkiGames.Scripts
{
    public class RenamableContentItemController : ContentItemController
    {
        private readonly TextEditSession _renameEditor = new();

        protected bool IsRenaming { get; private set; }

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
