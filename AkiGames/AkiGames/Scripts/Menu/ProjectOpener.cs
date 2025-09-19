using System.Windows.Forms;
using AkiGames.Core;

namespace AkiGames.Scripts.Menu
{
    public class ProjectOpener : SubmenuItemController
    {
        private WindowContentTypes.ExplorerWindowController explorerWindowController;

        public override void Awake()
        {
            explorerWindowController = gameObject.Parent.Parent.Parent.Parent.Children[0].Children[3].
                                    GetComponent<WindowContentTypes.ExplorerWindowController>();
        }

        private void OpenProjectDialog()
        {
            try
            {
                // Создаем диалог выбора папки
                var dialog = new FolderBrowserDialog
                {
                    Description = "Select project folder",
                    UseDescriptionForTitle = true,
                    ShowNewFolderButton = true
                };

                // Получаем форму Monogame для родительского окна
                Control form = Control.FromHandle(Game1.WindowHandle);

                // Показываем диалог
                if (dialog.ShowDialog(form) == DialogResult.OK)
                {
                    explorerWindowController?.SetProjectPath(dialog.SelectedPath);
                }
            }
            catch (System.Exception) { }
        }

        public override void OnMouseUp()
        {
            OpenProjectDialog();
            base.OnMouseUp();
        }
    }
}