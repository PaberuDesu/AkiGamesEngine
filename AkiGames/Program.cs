using AkiGames.Core;

namespace AkiGames
{
    class Program
    {
        [STAThread]
        static void Main()
        {
            try
            {
                using var game = new VeldridGame();
                game.Run();
            }
            catch (Exception ex)
            {
                Logger.Log(ex);
                Console.WriteLine("Error logged to error_log.txt");
                Console.ReadKey();
            }
        }
    }
}