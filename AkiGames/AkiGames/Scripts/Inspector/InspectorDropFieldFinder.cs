namespace AkiGames.Scripts.Inspector
{
    public static class InspectorDropFieldFinder
    {
        public static T FindInAncestry<T>(GameObject target) where T : GameComponent
        {
            while (target != null)
            {
                T dropField = target.GetComponent<T>();
                if (dropField != null) return dropField;

                target = target.Parent;
            }

            return null;
        }
    }
}
