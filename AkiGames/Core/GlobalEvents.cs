namespace AkiGames.Core
{
    // Глобальный класс для событий (замена Game1.UpdateAction)
    public static class GlobalEvents
    {
        public static event Action<GameTime> Update = delegate { };
        public static void InvokeUpdate(GameTime gt) => Update(gt);
    }
}