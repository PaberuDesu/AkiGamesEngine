using AkiGames.Core;
using AkiGames.Scripts.WindowContentTypes;
using AkiGames.UI.DropDown;

namespace AkiGames.Scripts.Menu
{
    public class ProjectOpener : DropDownItem
    {
        private ExplorerWindowController? _explorerWindowController;

        public override void Awake()
        {
            try
            {
                _explorerWindowController = gameObject.Parent?.Parent?.Parent?.Parent?.Children[0].Children[3]
                    .GetComponent<ExplorerWindowController>();
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log($"Failed to find ExplorerWindowController: {ex.Message}");
            }
        }

        private void OpenProjectDialog()
        {
            try
            {
                using var dialog = new FolderBrowserDialog
                {
                    Description = "Select project folder",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = true
                };

                // Создаём обёртку для IntPtr окна
                var owner = new WindowWrapper(VeldridGame.WindowHandle);
                if (dialog.ShowDialog(owner) == DialogResult.OK)
                {
                    _explorerWindowController?.SetProjectPath(dialog.SelectedPath);
                }
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log($"Error opening project dialog: {ex.Message}");
            }
        }

        public override void OnMouseUp()
        {
            OpenProjectDialog();
            base.OnMouseUp();
        }

        // Вспомогательный класс для привязки IntPtr к IWin32Window
        private class WindowWrapper(IntPtr handle) : IWin32Window
        {
            public IntPtr Handle { get; } = handle;
        }
    }
}