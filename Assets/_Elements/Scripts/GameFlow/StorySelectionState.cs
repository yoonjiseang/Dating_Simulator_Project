using VN.Systems;

namespace GameFlow
{
    public static class StorySelectionState
    {
        public static string SelectedRouteId { get; private set; }
        public static string SelectedStoryId { get; private set; }

        public static void SetSelectedRoute(string routeId, string storyId)
        {
            SelectedRouteId = routeId;
            SelectedStoryId = storyId;
        }

        public static void ApplyProgressToVariables(VariableStore variables)
        {
            if (variables == null)
            {
                return;
            }

            variables.Apply("totalClearCount", "set", GameProgressStore.GetTotalClearCount());

            if (!string.IsNullOrWhiteSpace(SelectedRouteId))
            {
                var currentRouteClear = GameProgressStore.GetRouteClearCount(SelectedRouteId);
                variables.Apply("selectedRouteClearCount", "set", currentRouteClear);
            }
        }
    }
}