using System;
using System.Globalization;
using VN.Systems;

namespace GameFlow
{
    public static class StoryRouteAvailability
    {
        private const string DateFormat = "yyyy/MM/dd HH:mm:ss";

        public static bool IsPlayable(StoryRouteEntry route, VariableStore variables, DateTime nowUtc)
        {
            if (route == null)
            {
                return false;
            }

            if (!IsWithinWindow(route.startTimeRaw, route.endTimeRaw, nowUtc))
            {
                return false;
            }

            return IsUnlockSatisfied(route.unlockRule, variables);
        }

        private static bool IsWithinWindow(string startRaw, string endRaw, DateTime nowUtc)
        {
            if (TryParseUtc(startRaw, out var startUtc) && nowUtc < startUtc)
            {
                return false;
            }

            if (TryParseUtc(endRaw, out var endUtc) && nowUtc > endUtc)
            {
                return false;
            }

            return true;
        }

        private static bool IsUnlockSatisfied(string unlockRule, VariableStore variables)
        {
            if (string.IsNullOrWhiteSpace(unlockRule) || unlockRule == "0")
            {
                return true;
            }

            var rule = unlockRule.Trim();

            if (int.TryParse(rule, out var requiredClearCount))
            {
                return variables.GetValue("totalClearCount") >= requiredClearCount;
            }

            if (rule.StartsWith("clear:", StringComparison.OrdinalIgnoreCase))
            {
                var routeId = rule.Substring("clear:".Length).Trim();
                return GameProgressStore.GetRouteClearCount(routeId) > 0;
            }

            if (rule.StartsWith("expr:", StringComparison.OrdinalIgnoreCase))
            {
                var expression = rule.Substring("expr:".Length).Trim();
                return variables.Evaluate(expression);
            }

            return false;
        }

        private static bool TryParseUtc(string value, out DateTime utc)
        {
            utc = default;
            if (string.IsNullOrWhiteSpace(value))
            {
                return false;
            }

            if (!DateTime.TryParseExact(value.Trim(), DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.AssumeUniversal | DateTimeStyles.AdjustToUniversal, out utc))
            {
                return false;
            }

            return true;
        }
    }
}