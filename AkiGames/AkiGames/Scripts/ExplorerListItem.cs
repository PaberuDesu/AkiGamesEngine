using AkiGames.Events;
using AkiGames.Core;
using AkiGames.Scripts.InspectorRedactor;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI;
using Microsoft.Xna.Framework.Graphics;

namespace AkiGames.Scripts
{
    public class ExplorerListItem : ContentItemController
    {
        public bool isFile = false;
        public bool IsImageFile = false;
        public string FilePath = "";

        private static GameObject draggedFile;
        private bool _isDragging = false;

        public override void Awake()
        {
            base.Awake();
            if (draggedFile == null)
            {
                draggedFile = Game1.MainObject.Children[2];
                draggedFile.RefreshBounds();
            }
        }

        private void StartDrag()
        {
            draggedFile.IsActive = true;
            Image draggedImage = draggedFile.GetComponent<Image>();
            Texture2D fallbackIcon = gameObject.Children[0].GetComponent<Image>()?.texture;
            draggedImage.texture =
                IsImageFile && Game1.UIImages.TryGetValue("ImageFile", out Texture2D imageFileTexture) ?
                    imageFileTexture :
                    fallbackIcon;
            _isDragging = true;
        }

        public override void Drag(
            Microsoft.Xna.Framework.Vector2 cursorPosOnObj
        )
        {
            if (!isFile) return;
            StartDrag();
            var mousePos = Microsoft.Xna.Framework.Input.Mouse.GetState().Position.ToVector2();
            draggedFile.uiTransform.OffsetMin = mousePos;
            draggedFile.RefreshBounds();
        }

        public override void OnMouseUpOutside() => OnMouseUp();

        public override void OnMouseUp()
        {
            if (_isDragging && IsImageFile)
            {
                InspectorTextureDropField textureDropField =
                    FindTextureDropField(Input.MouseHoverTarget);
                textureDropField?.TryApplyFile(FilePath);
            }

            _isDragging = false;
            draggedFile.IsActive = false;
        }

        private static InspectorTextureDropField FindTextureDropField(GameObject target)
        {
            while (target != null)
            {
                InspectorTextureDropField textureDropField =
                    target.GetComponent<InspectorTextureDropField>();
                if (textureDropField != null) return textureDropField;

                target = target.Parent;
            }

            return null;
        }
    }
}
