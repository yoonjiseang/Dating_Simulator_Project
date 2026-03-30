using UnityEngine;
using VN.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VN.Core
{
    public class StoryLoader
    {
        // 기본: Resources/_ElementsResources/VN/Stories/{storyId}.json(TextAsset)
        // 에디터 보조: Assets/_ElementsResources/VN/Stories/{storyId}.json 직접 로드
        public StoryData LoadStory(string storyId)
        {
            if (string.IsNullOrWhiteSpace(storyId))
            {
                Debug.LogError("[StoryLoader] storyId is null or empty.");
                return null;
            }

            var resourcesPath = $"_ElementsResources/VN/Stories/{storyId}";
            var asset = Resources.Load<TextAsset>(resourcesPath);

#if UNITY_EDITOR
            if (asset == null)
            {
                var editorAssetPath = $"Assets/_ElementsResources/VN/Stories/{storyId}.json";
                asset = AssetDatabase.LoadAssetAtPath<TextAsset>(editorAssetPath);
                if (asset != null)
                {
                    Debug.Log($"[StoryLoader] Loaded story via AssetDatabase fallback: {editorAssetPath}");
                }
            }
#endif

            if (asset == null)
            {
                Debug.LogError(
                    $"[StoryLoader] Story json not found. " +
                    $"Expected Resources path: Resources/{resourcesPath}.json " +
                    $"or editor fallback path: Assets/_ElementsResources/VN/Stories/{storyId}.json");
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