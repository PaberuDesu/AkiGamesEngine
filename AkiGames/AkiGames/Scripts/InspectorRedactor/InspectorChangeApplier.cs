using System;
using AkiGames.Scripts.WindowContentTypes;

namespace AkiGames.Scripts.InspectorRedactor
{
    internal static class InspectorChangeApplier
    {
        public static void Apply(GameComponent component)
        {
            if (component == null) return;

            try
            {
                if (component.gameObject?.Parent != null)
                    component.gameObject.RefreshBounds();

                HierarchyWindowController.ApplyInspectorChanges();
            }
            catch (Exception ex)
            {
                ConsoleWindowController.Log($"Inspector changes can't be applied: {ex.Message}");
            }
        }
    }
}
