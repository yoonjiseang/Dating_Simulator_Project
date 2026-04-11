using System.IO;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VN.Data;
#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VN.Core
{
    public class StoryLoader
    {
        private static readonly string[] EditorStoryRootPaths =
        {
            "Assets/_ElementsResources/GameFlow/Stories",
            "Assets/_ElementsResources/VN/Stories"
        };
        private const string AddressableStoryPrefix = "VN/Stories/";

        // Editor: Assets/_ElementsResources/VN/Stories/{storyId}.json 직접 로드
        // Runtime: Addressables -> Resources -> StreamingAssets 순으로 fallback
        public StoryData LoadStory(string storyId)
        {
            if (string.IsNullOrWhiteSpace(storyId))
            {
                Debug.LogError("[StoryLoader] storyId is null or empty.");
                return null;
            }

            string assetPath = null;

#if UNITY_EDITOR
            TextAsset asset = null;
            for (var i = 0; i < EditorStoryRootPaths.Length; i++)
            {
                var path = $"{EditorStoryRootPaths[i]}/{storyId}.json";
                asset = AssetDatabase.LoadAssetAtPath<TextAsset>(path);
                if (asset != null)
                {
                    assetPath = path;
                    break;
                }
            }

            if (asset == null)
            {
                Debug.LogError($"[StoryLoader] Story json not found for id: {storyId}");
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
            var story = LoadFromAddressables(storyId);
            if (story != null)
            {
                return story;
            }

            story = LoadFromResources(storyId);
            if (story != null)
            {
                return story;
            }

            story = LoadFromStreamingAssets(storyId);
            if (story != null)
            {
                return story;
            }

            Debug.LogError($"[StoryLoader] Failed to load story data for storyId='{storyId}' from all runtime sources.");
            return null;
#endif
        }

#if !UNITY_EDITOR
        private static StoryData LoadFromAddressables(string storyId)
        {
            var addressCandidates = new[]
            {
                AddressableStoryPrefix + storyId,
                AddressableStoryPrefix + storyId + ".json",
                "VN/" + storyId
            };

            foreach (var address in addressCandidates)
            {
                AsyncOperationHandle<TextAsset> handle = default;
                try
                {
                    handle = Addressables.LoadAssetAsync<TextAsset>(address);
                    var asset = handle.WaitForCompletion();
                    if (asset == null)
                    {
                        continue;
                    }

                    var parsed = JsonUtility.FromJson<StoryData>(asset.text);
                    if (parsed != null)
                    {
                        return parsed;
                    }
                }
                catch (System.Exception ex)
                {
                    Debug.LogWarning($"[StoryLoader] Addressables load failed for '{address}'. Trying fallback. {ex.Message}");
                }
                finally
                {
                    if (handle.IsValid())
                    {
                        Addressables.Release(handle);
                    }
                }
            }

            return null;
        }

        private static StoryData LoadFromResources(string storyId)
        {
            var resourceCandidates = new[]
            {
                $"VN/Stories/{storyId}",
                $"Stories/{storyId}",
                storyId
            };

            for (var i = 0; i < resourceCandidates.Length; i++)
            {
                var asset = Resources.Load<TextAsset>(resourceCandidates[i]);
                if (asset == null)
                {
                    continue;
                }

                var parsed = JsonUtility.FromJson<StoryData>(asset.text);
                if (parsed != null)
                {
                    return parsed;
                }
            }

            return null;
        }

        private static StoryData LoadFromStreamingAssets(string storyId)
        {
            var filePath = Path.Combine(Application.streamingAssetsPath, "VN", "Stories", storyId + ".json");
            if (!File.Exists(filePath))
            {
                return null;
            }

            var json = File.ReadAllText(filePath);
            return string.IsNullOrWhiteSpace(json) ? null : JsonUtility.FromJson<StoryData>(json);
        }
#endif
    }
}
