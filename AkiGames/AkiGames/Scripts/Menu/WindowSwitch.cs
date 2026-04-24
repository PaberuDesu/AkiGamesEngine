using Microsoft.Xna.Framework;
using Microsoft.Xna.Framework.Graphics;
using AkiGames.Core;
using AkiGames.UI;
using AkiGames.UI.DropDown;

namespace AkiGames.Scripts.Menu
{
    public class WindowSwitch : DropDownItem
    {
        public string WindowName;
        private GameObject Toggle;
        private static Texture2D _checkmarkTexture;
        private GameObject Window;
        private static GameObject WindowContainer;

        public override void Awake()
        {
            Toggle = new GameObject("Toggle")
            {
                IsMouseTargetable = false,
                uiTransform = new UITransform()
                {
                    HorizontalAlignment = UITransform.AlignmentH.Left,
                    VerticalAlignment = UITransform.AlignmentV.Middle,
                    OffsetMin = new Vector2(6, 0),
                    Width = 16,
                    Height = 16,
                    origin = new Vector2(0, 0.5f)
                }
            };
            _checkmarkTexture ??= Game1.UIImages["Checkmark"];
            Toggle.AddComponent(new Image()
            {
                texture = _checkmarkTexture
            });
            gameObject.AddChild(Toggle);

            GameObject titleObject = new("title")
            {
                IsMouseTargetable = false,
                uiTransform = new UITransform()
                {
                    HorizontalAlignment = UITransform.AlignmentH.Stretch,
                    VerticalAlignment = UITransform.AlignmentV.Stretch,
                    OffsetMin = new Vector2(28, 0),
                    OffsetMax = new Vector2(6, 0)
                }
            };
            titleObject.AddComponent(new Text()
            {
                text = WindowName,
                HorizontalAlignment = Text.AlignmentH.Left,
            });
            gameObject.AddChild(titleObject);
        }

        public override void Start()
        {
            Window = FindByName();
            Toggle.IsActive = Window.IsActive;
            base.Start();
        }

        private void ToggleWindow()
        {
            Window.IsActive = !Window.IsActive;
            Toggle.IsActive = Window.IsActive;
        }

        private GameObject FindByName()
        {
            WindowContainer ??= Game1.MainObject?.Children[0];
            if (WindowContainer == null) return null;

            string windowCode = WindowName + "Window";

            foreach (GameObject window in WindowContainer.Children)
            {
                if (window.ObjectName == windowCode) return window;
            }

            return null;
        }

        public override void OnMouseUp()
        {
            ToggleWindow();
            base.OnMouseUp();
        }
    }
}
