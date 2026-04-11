using UnityEngine;

namespace GameFlow
{
    public static class GameProgressStore
    {
        private const string TotalClearCountKey = "GF.TotalClearCount";
        private const string RouteClearCountPrefix = "GF.RouteClear.";

        public static int GetTotalClearCount()
        {
            return PlayerPrefs.GetInt(TotalClearCountKey, 0);
        }

        public static int GetRouteClearCount(string routeId)
        {
            if (string.IsNullOrWhiteSpace(routeId))
            {
                return 0;
            }

            return PlayerPrefs.GetInt(RouteClearCountPrefix + routeId, 0);
        }

        public static void MarkRouteCleared(string routeId)
        {
            if (string.IsNullOrWhiteSpace(routeId))
            {
                return;
            }

            var currentTotal = GetTotalClearCount();
            var currentRoute = GetRouteClearCount(routeId);

            PlayerPrefs.SetInt(TotalClearCountKey, currentTotal + 1);
            PlayerPrefs.SetInt(RouteClearCountPrefix + routeId, currentRoute + 1);
            PlayerPrefs.Save();
        }
    }
}