using System;
using System.Collections;
using System.Collections.Generic;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using UObject = UnityEngine.Object;
using VN.Data;

namespace VN.Core
{
    public class ResourceProvider
    {
        private const string AddressPrefix = "VN/";
        private const int AudioWarmupMaxFrames = 900;

        private readonly Dictionary<string, UObject> _cache = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, Task<UObject>> _loadingTasks = new(StringComparer.OrdinalIgnoreCase);
        private readonly Dictionary<string, AsyncOperationHandle> _handles = new(StringComparer.OrdinalIgnoreCase);

        public Sprite LoadBackground(string bgKey) => LoadBlocking<Sprite>($"Backgrounds/{bgKey}");
        public AudioClip LoadBgm(string bgmKey) => LoadBlocking<AudioClip>($"BGM/{bgmKey}");
        public AudioClip LoadSfx(string sfxKey) => LoadBlocking<AudioClip>($"SFX/{sfxKey}");
        public AudioClip LoadVoice(string characterId, string voiceKey) => LoadBlocking<AudioClip>($"Characters/{characterId}/voice/{voiceKey}");
        public Sprite LoadCharacterBody(string characterId, string bodyKey) => LoadBlocking<Sprite>($"Characters/{characterId}/{characterId}_{bodyKey}");
        public Sprite LoadCharacterFace(string characterId, string faceKey) => LoadBlocking<Sprite>($"Characters/{characterId}/face_{faceKey}");

        public Sprite LoadCharacterSprite(string characterId, string faceKey)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(faceKey))
            {
                Debug.LogError($"[ResourceProvider] Invalid character sprite key. characterId={characterId}, face={faceKey}");
                return null;
            }

            var normalizedCharacterId = characterId.Trim().PadLeft(4, '0');
            var normalizedFaceKey = faceKey.Trim().PadLeft(2, '0');
            return LoadBlocking<Sprite>($"Characters/{normalizedCharacterId}/{normalizedCharacterId}_{normalizedFaceKey}");
        }

        public IEnumerator PreloadStoryAssets(StoryData story, Action<float, string> onProgress = null, int batchSize = 8)
        {
            if (story?.nodes == null || story.nodes.Length == 0)
            {
                onProgress?.Invoke(1f, "No story nodes to preload.");
                yield break;
            }

            var preloadTasks = CollectPreloadTasks(story);
            if (preloadTasks.Count == 0)
            {
                onProgress?.Invoke(1f, "No referenced assets to preload.");
                yield break;
            }

            var completedCount = 0;
            onProgress?.Invoke(0f, $"Preloading 0/{preloadTasks.Count}");

            var stride = Math.Max(1, batchSize);
            for (var offset = 0; offset < preloadTasks.Count; offset += stride)
            {
                var count = Math.Min(stride, preloadTasks.Count - offset);
                var batch = new Task[count];

                for (var i = 0; i < count; i++)
                {
                    batch[i] = preloadTasks[offset + i]();
                }

                var allTask = Task.WhenAll(batch);
                yield return WaitTask(allTask);

                completedCount += count;
                onProgress?.Invoke((float)completedCount / preloadTasks.Count, $"Preloading {completedCount}/{preloadTasks.Count}");
                yield return null;
            }

            onProgress?.Invoke(1f, $"Preloading complete ({completedCount}/{preloadTasks.Count})");
        }

        private List<Func<Task>> CollectPreloadTasks(StoryData story)
        {
            var tasks = new List<Func<Task>>();
            var dedupe = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

            foreach (var node in story.nodes)
            {
                if (node?.commands == null)
                {
                    continue;
                }

                foreach (var command in node.commands)
                {
                    if (command == null)
                    {
                        continue;
                    }

                    TryAddAssetTask(command.bg, value => LoadAssetAsync<Sprite>($"Backgrounds/{value}"), "bg", tasks, dedupe);
                    TryAddAssetTask(command.bgm, value => LoadAssetAsync<AudioClip>($"BGM/{value}"), "bgm", tasks, dedupe);
                    TryAddAssetTask(command.sfx, value => LoadAssetAsync<AudioClip>($"SFX/{value}"), "sfx", tasks, dedupe);

                    var characterIds = command.GetCharacterIds();
                    var faces = command.GetFaces();
                    if (characterIds.Length > 0 && faces.Length > 0)
                    {
                        for (var i = 0; i < characterIds.Length; i++)
                        {
                            var characterId = characterIds[i];
                            var face = i < faces.Length ? faces[i] : faces[faces.Length - 1];
                            var key = $"char:{characterId}:{face}";
                            if (!dedupe.Add(key))
                            {
                                continue;
                            }

                            tasks.Add(() => LoadCharacterSpriteAsync(characterId, face));
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(command.characterId) && !string.IsNullOrWhiteSpace(command.voice))
                    {
                        var key = $"voice:{command.characterId.Trim()}:{command.voice.Trim()}";
                        if (dedupe.Add(key))
                        {
                            var characterId = command.characterId;
                            var voice = command.voice;
                            tasks.Add(() => LoadAssetAsync<AudioClip>($"Characters/{characterId}/voice/{voice}"));
                        }
                    }
                }
            }

            return tasks;
        }

        private static void TryAddAssetTask<T>(string value, Func<string, Task<T>> loader, string prefix, ICollection<Func<Task>> tasks, ISet<string> dedupe)
            where T : UObject
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var normalized = value.Trim();
            var key = $"{prefix}:{normalized}";
            if (dedupe.Add(key))
            {
                tasks.Add(async () => await loader(normalized));
            }
        }

        private async Task<Sprite> LoadCharacterSpriteAsync(string characterId, string faceKey)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(faceKey))
            {
                Debug.LogError($"[ResourceProvider] Invalid character sprite key. characterId={characterId}, face={faceKey}");
                return null;
            }
            var normalizedCharacterId = characterId.Trim().PadLeft(4, '0');
            var normalizedFaceKey = faceKey.Trim().PadLeft(2, '0');
            return await LoadAssetAsync<Sprite>($"Characters/{normalizedCharacterId}/{normalizedCharacterId}_{normalizedFaceKey}");
        }

        private T LoadBlocking<T>(string relativeKey) where T : UObject
        {
            if (string.IsNullOrWhiteSpace(relativeKey))
            {
                return null;
            }

            var normalizedRelative = NormalizeRelativeKey(relativeKey);
            if (_cache.TryGetValue(normalizedRelative, out var cached))
            {
                return cached as T;
            }

            var address = ToAddress(normalizedRelative);
            var handle = Addressables.LoadAssetAsync<T>(address);
            _handles[normalizedRelative] = handle;

            var result = handle.WaitForCompletion();
            if (result == null)
            {
                Debug.LogError($"[ResourceProvider] Failed to load addressable asset synchronously: {normalizedRelative}");
                Addressables.Release(handle);
                _handles.Remove(normalizedRelative);
                return null;
            }

            _cache[normalizedRelative] = result;
            return result;
        }

        private async Task<T> LoadAssetAsync<T>(string relativeKey) where T : UObject
        {
            if (string.IsNullOrWhiteSpace(relativeKey))
            {
                return null;
            }

            var normalizedRelative = NormalizeRelativeKey(relativeKey);
            if (_cache.TryGetValue(normalizedRelative, out var cached))
            {
                return cached as T;
            }

            if (_loadingTasks.TryGetValue(normalizedRelative, out var loadingTask))
            {
                return await CastTask<T>(loadingTask);
            }

            var address = ToAddress(normalizedRelative);
            var handle = Addressables.LoadAssetAsync<T>(address);
            _handles[normalizedRelative] = handle;

            var task = AwaitAndCache(handle, normalizedRelative);
            _loadingTasks[normalizedRelative] = task;

            var loaded = await task;
            return loaded as T;
        }

        private async Task<UObject> AwaitAndCache<T>(AsyncOperationHandle<T> handle, string normalizedRelative) where T : UObject
        {
            try
            {
                var result = await handle.Task;
                if (result == null)
                {
                    Debug.LogError($"[ResourceProvider] Addressables returned null for key: {normalizedRelative}");
                    _handles.Remove(normalizedRelative);
                    return null;
                }

                _cache[normalizedRelative] = result;

                if (result is AudioClip clip)
                {
                    await WarmupAudioAsync(clip);
                }

                return result;
            }
            catch (Exception ex)
            {
                Debug.LogError($"[ResourceProvider] Failed to load addressable asset: {normalizedRelative}\n{ex}");
                if (_handles.TryGetValue(normalizedRelative, out var cachedHandle))
                {
                    Addressables.Release(cachedHandle);
                    _handles.Remove(normalizedRelative);
                }

                return null;
            }
            finally
            {
                _loadingTasks.Remove(normalizedRelative);
            }
        }

        private static async Task WarmupAudioAsync(AudioClip clip)
        {
            if (clip == null)
            {
                return;
            }

            if (clip.loadState == AudioDataLoadState.Unloaded)
            {
                clip.LoadAudioData();
            }

            var frame = 0;
            while (clip.loadState == AudioDataLoadState.Loading && frame < AudioWarmupMaxFrames)
            {
                frame++;
                await Task.Yield();
            }

            if (clip.loadState == AudioDataLoadState.Failed)
            {
                Debug.LogWarning($"[ResourceProvider] Audio data warmup failed: {clip.name}");
            }
        }

        private static IEnumerator WaitTask(Task task)
        {
            while (!task.IsCompleted)
            {
                yield return null;
            }

            if (task.IsFaulted)
            {
                Debug.LogException(task.Exception);
            }
        }

        private static async Task<T> CastTask<T>(Task<UObject> task) where T : UObject
        {
            var result = await task;
            return result as T;
        }

        private static string ToAddress(string normalizedRelativeKey) => AddressPrefix + normalizedRelativeKey;

        private static string NormalizeRelativeKey(string relativeKey)
        {
            return relativeKey.Trim().Replace('\\', '/');
        }
    }
}
