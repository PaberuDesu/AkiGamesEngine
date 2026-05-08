using System;
using System.Windows.Forms;
using AkiGames.Core;

namespace AkiGames
{
    public static class Program
    {
        [STAThread]
        static void Main()
        {
            // Настройка высокого DPI
            Application.SetHighDpiMode(HighDpiMode.PerMonitorV2);
            Application.EnableVisualStyles();
            Application.SetCompatibleTextRenderingDefault(false);

            using var game = new Game1();
            game.Run();
        }
    }
}