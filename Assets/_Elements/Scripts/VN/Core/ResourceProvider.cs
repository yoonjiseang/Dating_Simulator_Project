using System;
using System.Collections;
using System.Collections.Generic;
using System.IO;
using UnityEngine;
using UObject = UnityEngine.Object;
using VN.Data;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VN.Core
{
    public class ResourceProvider
    {
        private const string RootPath = "Assets/_ElementsResources/VN";
        private const int AudioWarmupMaxFrames = 900;

        private static readonly string[] SpriteExtensions = { ".png", ".jpg", ".jpeg" };
        private static readonly string[] AudioExtensions = { ".wav", ".mp3", ".ogg" };

        private readonly Dictionary<string, UObject> _cache = new();

        public Sprite LoadBackground(string bgKey) => LoadSpriteByKey($"Backgrounds/{bgKey}");
        public AudioClip LoadBgm(string bgmKey) => LoadAudioByKey($"BGM/{bgmKey}");
        public AudioClip LoadSfx(string sfxKey) => LoadAudioByKey($"SFX/{sfxKey}");
        public AudioClip LoadVoice(string characterId, string voiceKey) => LoadAudioByKey($"Characters/{characterId}/voice/{voiceKey}");
        public Sprite LoadCharacterBody(string characterId, string bodyKey) => LoadSpriteByKey($"Characters/{characterId}/{characterId}_{bodyKey}");
        public Sprite LoadCharacterFace(string characterId, string faceKey) => LoadSpriteByKey($"Characters/{characterId}/face_{faceKey}");

        public Sprite LoadCharacterSprite(string characterId, string faceKey)
        {
            if (string.IsNullOrWhiteSpace(characterId) || string.IsNullOrWhiteSpace(faceKey))
            {
                Debug.LogError($"[ResourceProvider] Invalid character sprite key. characterId={characterId}, face={faceKey}");
                return null;
            }

            var normalizedCharacterId = characterId.Trim().PadLeft(4, '0');
            var normalizedFaceKey = faceKey.Trim().PadLeft(2, '0');
            return LoadSpriteByKey($"Characters/{normalizedCharacterId}/{normalizedCharacterId}_{normalizedFaceKey}");
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

            foreach (var task in preloadTasks)
            {
                yield return task();
                completedCount++;

                onProgress?.Invoke((float)completedCount / preloadTasks.Count, $"Preloading {completedCount}/{preloadTasks.Count}");

                if (completedCount % Math.Max(1, batchSize) == 0)
                {
                    yield return null;
                }
            }

            onProgress?.Invoke(1f, $"Preloading complete ({completedCount}/{preloadTasks.Count})");
        }

        private List<Func<IEnumerator>> CollectPreloadTasks(StoryData story)
        {
            var tasks = new List<Func<IEnumerator>>();
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

                    TryAddSpriteTask(command.bg, value => LoadBackground(value), "bg", tasks, dedupe);
                    TryAddAudioTask(command.bgm, value => LoadBgm(value), "bgm", tasks, dedupe);
                    TryAddAudioTask(command.sfx, value => LoadSfx(value), "sfx", tasks, dedupe);

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

                            tasks.Add(() => PreloadSpriteCoroutine(() => LoadCharacterSprite(characterId, face)));
                        }
                    }

                    if (!string.IsNullOrWhiteSpace(command.characterId) && !string.IsNullOrWhiteSpace(command.voice))
                    {
                        var key = $"voice:{command.characterId.Trim()}:{command.voice.Trim()}";
                        if (dedupe.Add(key))
                        {
                            var characterId = command.characterId;
                            var voice = command.voice;
                            tasks.Add(() => PreloadAudioCoroutine(() => LoadVoice(characterId, voice)));
                        }
                    }
                }
            }

            return tasks;
        }

        private static void TryAddSpriteTask(string value, Func<string, Sprite> loader, string prefix, ICollection<Func<IEnumerator>> tasks, ISet<string> dedupe)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var normalized = value.Trim();
            var key = $"{prefix}:{normalized}";
            if (dedupe.Add(key))
            {
                tasks.Add(() => PreloadSpriteCoroutine(() => loader(normalized)));
            }
        }

        private static void TryAddAudioTask(string value, Func<string, AudioClip> loader, string prefix, ICollection<Func<IEnumerator>> tasks, ISet<string> dedupe)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return;
            }

            var normalized = value.Trim();
            var key = $"{prefix}:{normalized}";
            if (dedupe.Add(key))
            {
                tasks.Add(() => PreloadAudioCoroutine(() => loader(normalized)));
            }
        }

        private static IEnumerator PreloadSpriteCoroutine(Func<Sprite> loadFunc)
        {
            loadFunc();
            yield break;
        }

        private static IEnumerator PreloadAudioCoroutine(Func<AudioClip> loadFunc)
        {
            var clip = loadFunc();
            if (clip == null)
            {
                yield break;
            }

            if (clip.loadState == AudioDataLoadState.Unloaded)
            {
                clip.LoadAudioData();
            }

            var frame = 0;
            while (clip.loadState == AudioDataLoadState.Loading && frame < AudioWarmupMaxFrames)
            {
                frame++;
                yield return null;
            }

            if (clip.loadState == AudioDataLoadState.Failed)
            {
                Debug.LogWarning($"[ResourceProvider] Audio data warmup failed: {clip.name}");
            }
        }

        private Sprite LoadSpriteByKey(string relativeKey)
        {
            if (TryGetCached(relativeKey, out Sprite cached))
            {
                return cached;
            }

#if UNITY_EDITOR
            var withoutExt = $"{RootPath}/{relativeKey}";
            var assetPath = ResolveExistingPath(withoutExt, SpriteExtensions);
            if (assetPath == null)
            {
                Debug.LogError($"[ResourceProvider] Sprite not found under Assets path: {withoutExt} (tried {string.Join(", ", SpriteExtensions)})");
                return null;
            }

            var sprite = AssetDatabase.LoadAssetAtPath<Sprite>(assetPath);
            if (!sprite)
            {
                var texture = AssetDatabase.LoadAssetAtPath<Texture2D>(assetPath);
                if (!texture)
                {
                    Debug.LogError($"[ResourceProvider] Failed to load sprite/texture at path: {assetPath}");
                    return null;
                }

                sprite = Sprite.Create(
                    texture,
                    new Rect(0f, 0f, texture.width, texture.height),
                    new Vector2(0.5f, 0.5f),
                    100f);
            }

            _cache[relativeKey] = sprite;
            return sprite;
#else
            Debug.LogError(
                $"[ResourceProvider] Runtime loader for key '{relativeKey}' is not implemented yet. " +
                "Planned source: Assets/_ElementsBundles.");
            return null;
#endif
        }

        private AudioClip LoadAudioByKey(string relativeKey)
        {
            if (TryGetCached(relativeKey, out AudioClip cached))
            {
                return cached;
            }

#if UNITY_EDITOR
            var withoutExt = $"{RootPath}/{relativeKey}";
            var assetPath = ResolveExistingPath(withoutExt, AudioExtensions);
            assetPath ??= ResolveAudioPathFallback(relativeKey);
            if (assetPath == null)
            {
                Debug.LogError($"[ResourceProvider] Audio not found under Assets path: {withoutExt} (tried {string.Join(", ", AudioExtensions)} + FindAssets fallback)");
                return null;
            }

            var clip = AssetDatabase.LoadAssetAtPath<AudioClip>(assetPath);
            if (!clip)
            {
                Debug.LogError($"[ResourceProvider] Failed to load audio clip at path: {assetPath}");
                return null;
            }

            _cache[relativeKey] = clip;
            return clip;
#else
            Debug.LogError(
                $"[ResourceProvider] Runtime loader for key '{relativeKey}' is not implemented yet. " +
                "Planned source: Assets/_ElementsBundles.");
            return null;
#endif
        }

        private bool TryGetCached<T>(string key, out T value) where T : UObject
        {
            if (_cache.TryGetValue(key, out var cached))
            {
                value = cached as T;
                return value;
            }

            value = null;
            return false;
        }

#if UNITY_EDITOR
        private static string ResolveAudioPathFallback(string relativeKey)
        {
            var fileNameNoExt = Path.GetFileName(relativeKey);
            if (string.IsNullOrWhiteSpace(fileNameNoExt))
            {
                return null;
            }

            var guids = AssetDatabase.FindAssets($"{fileNameNoExt} t:AudioClip", new[] { RootPath });
            foreach (var guid in guids)
            {
                var path = AssetDatabase.GUIDToAssetPath(guid);
                var nameNoExt = Path.GetFileNameWithoutExtension(path);
                if (string.Equals(nameNoExt, fileNameNoExt, StringComparison.OrdinalIgnoreCase))
                {
                    return path;
                }
            }

            return null;
        }
#endif

#if UNITY_EDITOR
        private static string ResolveExistingPath(string withoutExt, string[] extensions)
        {
            foreach (var ext in extensions)
            {
                var path = withoutExt + ext;
                var asset = AssetDatabase.LoadAssetAtPath<UObject>(path);
                if (asset != null)
                {
                    return path;
                }
            }

            return null;
        }
#endif
    }
}
