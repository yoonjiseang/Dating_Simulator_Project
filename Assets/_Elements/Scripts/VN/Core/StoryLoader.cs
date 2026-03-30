using UnityEngine;
using VN.Data;

namespace VN.Core
{
    public class StoryLoader
    {
        // _ElementsResources/VN/Stories/{storyId}.json(TextAsset) 로드
        public StoryData LoadStory(string storyId)
        {
            if (string.IsNullOrWhiteSpace(storyId))
            {
                Debug.LogError("[StoryLoader] storyId is null or empty.");
                return null;
            }

            var asset = Resources.Load<TextAsset>($"_ElementsResources/VN/Stories/{storyId}");
            if (asset == null)
            {
                Debug.LogError($"[StoryLoader] Story json not found: Resources/_ElementsResources/VN/Stories/{storyId}");
                return null;
            }

            var data = JsonUtility.FromJson<StoryData>(asset.text);
            if (data == null)
            {
                Debug.LogError($"[StoryLoader] Failed to parse json for storyId={storyId}");
                return null;
            }

            return data;
        }
    }
}