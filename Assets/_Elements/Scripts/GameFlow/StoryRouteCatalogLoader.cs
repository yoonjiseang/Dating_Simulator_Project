using System;
using System.Collections.Generic;
using VN.Data;

namespace GameFlow
{
    public static class StoryRouteCatalogLoader
    {
        public static List<StoryRouteEntry> LoadFromMasterData(MasterDataTable table)
        {
            var results = new List<StoryRouteEntry>();
            if (table == null || table.rows == null || table.rows.Length == 0)
            {
                return results;
            }

            var storyIdIndex = table.GetColumnIndex("story_id");
            var titleIndex = table.GetColumnIndex("title");
            var subTitleIndex = table.GetColumnIndex("sub_title");
            var unlockIdIndex = table.GetColumnIndex("unlock_id");
            var rewardIdIndex = table.GetColumnIndex("reward_id");
            var startTimeIndex = table.GetColumnIndex("start_time");
            var endTimeIndex = table.GetColumnIndex("end_time");

            for (var i = 0; i < table.rows.Length; i++)
            {
                var row = table.rows[i];
                if (row == null)
                {
                    continue;
                }

                var rawStoryId = row.GetCell(storyIdIndex).Trim();
                if (string.IsNullOrWhiteSpace(rawStoryId))
                {
                    continue;
                }

                var normalizedStoryId = NormalizeStoryId(rawStoryId);
                var title = row.GetCell(titleIndex);
                var subTitle = row.GetCell(subTitleIndex);

                results.Add(new StoryRouteEntry
                {
                    routeId = rawStoryId,
                    displayName = string.IsNullOrWhiteSpace(title) ? normalizedStoryId : title,
                    subTitle = subTitle,
                    storyId = normalizedStoryId,
                    unlockRule = row.GetCell(unlockIdIndex),
                    rewardId = row.GetCell(rewardIdIndex),
                    startTimeRaw = row.GetCell(startTimeIndex),
                    endTimeRaw = row.GetCell(endTimeIndex),
                    hiddenWhenLocked = false
                });
            }

            return results;
        }

        public static List<StoryRouteEntry> CreateFallbackCatalog()
        {
            return new List<StoryRouteEntry>
            {
                new StoryRouteEntry
                {
                    routeId = "0000001",
                    displayName = "기본 스토리",
                    storyId = "storydata_0000001",
                    unlockRule = "0"
                }
            };
        }

        public static string NormalizeStoryId(string rawStoryId)
        {
            if (string.IsNullOrWhiteSpace(rawStoryId))
            {
                return string.Empty;
            }

            var trimmed = rawStoryId.Trim();
            if (trimmed.StartsWith("storydata_", StringComparison.OrdinalIgnoreCase))
            {
                return trimmed;
            }

            return $"storydata_{trimmed}";
        }
    }
}