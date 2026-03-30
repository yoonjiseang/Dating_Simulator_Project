using System;

namespace VN.Data
{
    [Serializable]
    public class StoryData
    {
        public string storyId;
        public string startNode;
        public NodeData[] nodes;
    }
}