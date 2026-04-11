using System;

namespace GameFlow
{
    [Serializable]
    public class StoryRouteEntry
    {
        public string routeId;
        public string displayName;
        public string subTitle;
        public string storyId;
        public string unlockRule;
        public string rewardId;
        public string startTimeRaw;
        public string endTimeRaw;
        public bool hiddenWhenLocked;
    }
}