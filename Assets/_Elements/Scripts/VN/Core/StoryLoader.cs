using UnityEngine;
using VN.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VN.Core
{
    public class StoryLoader
    {
        private const string StoryRootPath = "Assets/_ElementsResources/VN/Stories";

        // Editor: Assets/_ElementsResources/VN/Stories/{storyId}.json 직접 로드
        // Runtime: 추후 _ElementsBundles(AssetBundle/Addressables) 로더로 연결 예정
        public StoryData LoadStory(string storyId)
        {
            if (string.IsNullOrWhiteSpace(storyId))
            {
                Debug.LogError("[StoryLoader] storyId is null or empty.");
                return null;
            }

            var assetPath = $"{StoryRootPath}/{storyId}.json";

#if UNITY_EDITOR
            var asset = AssetDatabase.LoadAssetAtPath<TextAsset>(assetPath);
            if (asset == null)
            {
                Debug.LogError($"[StoryLoader] Story json not found at: {assetPath}");
                return null;
            }

            var data = JsonUtility.FromJson<StoryData>(asset.text);
            if (data == null)
            {
                Debug.LogError($"[StoryLoader] Failed to parse json for storyId={storyId} from path={assetPath}");
                return null;
            }

            return data;
#else
            Debug.LogError(
                $"[StoryLoader] Runtime loader for '{assetPath}' is not implemented yet. " +
                "Planned source: Assets/_ElementsBundles.");
            return null;
#endif
        }
    }
}
