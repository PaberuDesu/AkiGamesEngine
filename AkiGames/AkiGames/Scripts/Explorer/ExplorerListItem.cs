using System.IO;
using AkiGames.Events;
using AkiGames.Core;
using AkiGames.Scripts.Inspector;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI;
using Microsoft.Xna.Framework.Graphics;

namespace AkiGames.Scripts.Explorer
{
    public class ExplorerListItem : EditableContentItemController
    {
        public bool isFile = false;
        public bool IsImageFile = false;
        public string FilePath = "";

        private static GameObject draggedFile;
        private bool _registerAkiAfterRename = false;
        private bool _writeScriptTemplateAfterRename = false;
        private string _renameExtension = "";

        public override void Awake()
        {
            base.Awake();
            if (draggedFile == null)
            {
                draggedFile = Game1.MainObject.Children[2];
                draggedFile.RefreshBounds();
            }
        }

        protected override bool CanStartDragging() =>
            !string.IsNullOrWhiteSpace(FilePath) &&
            (isFile ? File.Exists(FilePath) : Directory.Exists(FilePath));

        protected override bool IsCursorInsideLocalDragArea() =>
            FindExplorerWindow()?.IsCursorInsideExplorerWindow() ?? false;

        protected override void OnDragStarted()
        {
            draggedFile.IsActive = true;
            Image draggedImage = draggedFile.GetComponent<Image>();
            Texture2D fallbackIcon = gameObject.Children[0].GetComponent<Image>()?.texture;
            draggedImage.texture =
                IsImageFile && Game1.UIImages.TryGetValue("ImageFile", out Texture2D imageFileTexture) ?
                    imageFileTexture :
                    fallbackIcon;
        }

        protected override void UpdateLocalDrag(Microsoft.Xna.Framework.Vector2 cursorPosOnObj) =>
            UpdateDraggedFilePosition();

        protected override void UpdateOuterDrag(Microsoft.Xna.Framework.Vector2 cursorPosOnObj) =>
            UpdateDraggedFilePosition();

        protected override void CompleteLocalDrag()
        {
            ExplorerWindowController explorerWindow = FindExplorerWindow();
            ExplorerListItem targetItem = explorerWindow?.FindExplorerItemAt(Input.MouseHoverTarget);
            if (targetItem == null || targetItem == this || targetItem.isFile)
                return;

            explorerWindow.MoveItemIntoFolder(FilePath, targetItem.FilePath);
        }

        protected override void CompleteOuterDrag()
        {
            if (isFile && IsImageFile)
            {
                InspectorTextureDropField textureDropField =
                    InspectorDropFieldFinder.FindInAncestry<InspectorTextureDropField>(
                        Input.MouseHoverTarget
                    );
                textureDropField?.TryApplyFile(FilePath);
            }
        }

        protected override void OnDragEnded() => draggedFile.IsActive = false;

        private static void UpdateDraggedFilePosition()
        {
            var mousePos = Microsoft.Xna.Framework.Input.Mouse.GetState().Position.ToVector2();
            draggedFile.uiTransform.OffsetMin = mousePos;
            draggedFile.RefreshBounds();
        }

        public override void OnRMBUp()
        {
            SelectItem();
            FindExplorerWindow()?.ShowItemContext(this);
        }

        public override void StartRenaming()
        {
            _registerAkiAfterRename = false;
            _writeScriptTemplateAfterRename = false;
            base.StartRenaming();
        }

        public void StartRenaming(
            bool registerAkiAfterRename,
            bool writeScriptTemplateAfterRename
        )
        {
            _registerAkiAfterRename = registerAkiAfterRename;
            _writeScriptTemplateAfterRename = writeScriptTemplateAfterRename;
            base.StartRenaming();
        }

        protected override void BeforeStartRenaming() =>
            _renameExtension = isFile ? Path.GetExtension(FilePath) : "";

        protected override string GetRenameInitialValue() =>
            isFile ?
                Path.GetFileNameWithoutExtension(FilePath) :
                Path.GetFileName(FilePath);

        protected override string GetRenameSuffix() => _renameExtension;

        protected override string GetDisplayName() => Path.GetFileName(FilePath);

        protected override bool OnRenameCommitted(string newName)
        {
            bool registerAkiAfterRename = _registerAkiAfterRename;
            bool writeScriptTemplateAfterRename = _writeScriptTemplateAfterRename;
            _registerAkiAfterRename = false;
            _writeScriptTemplateAfterRename = false;

            ExplorerWindowController explorerWindow = FindExplorerWindow();
            if (explorerWindow != null)
            {
                explorerWindow.CompleteItemRename(
                    FilePath,
                    newName,
                    isFile,
                    _renameExtension,
                    registerAkiAfterRename,
                    writeScriptTemplateAfterRename
                );
                return false;
            }

            return true;
        }

        protected override bool OnRenameCancelled()
        {
            bool registerAkiAfterRename = _registerAkiAfterRename;
            bool writeScriptTemplateAfterRename = _writeScriptTemplateAfterRename;
            _registerAkiAfterRename = false;
            _writeScriptTemplateAfterRename = false;

            ExplorerWindowController explorerWindow = FindExplorerWindow();
            if (registerAkiAfterRename)
                explorerWindow?.RegisterCreatedScene(FilePath);
            if (writeScriptTemplateAfterRename)
                explorerWindow?.WriteCreatedScriptTemplate(FilePath);

            return true;
        }

        private ExplorerWindowController FindExplorerWindow()
        {
            var ancestry = gameObject.GetAncestry();
            for (int i = ancestry.Count - 1; i >= 0; i--)
            {
                ExplorerWindowController explorerWindow =
                    ancestry[i].GetComponent<ExplorerWindowController>();
                if (explorerWindow != null)
                    return explorerWindow;
            }

            return null;
        }
    }
}
