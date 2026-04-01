using System.Collections.Generic;
using System.IO;
using UnityEngine;

#if UNITY_EDITOR
using UnityEditor;
#endif

namespace VN.Core
{
    public class ResourceProvider
    {
        private const string RootPath = "Assets/_ElementsResources/VN";

        private static readonly string[] SpriteExtensions = { ".png", ".jpg", ".jpeg" };
        private static readonly string[] AudioExtensions = { ".wav", ".mp3", ".ogg" };

        private readonly Dictionary<string, Object> _cache = new();

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

                sprite = Sprite.Create(texture, new Rect(0, 0, texture.width, texture.height), new Vector2(0.5f, 0.5f), 100f);
                sprite.name = texture.name;
                Debug.LogWarning($"[ResourceProvider] Loaded Texture2D and created runtime sprite: {assetPath}. Consider setting Texture Type=Sprite.");
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

        private bool TryGetCached<T>(string key, out T value) where T : Object
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
                if (string.Equals(nameNoExt, fileNameNoExt, System.StringComparison.OrdinalIgnoreCase))
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
                var asset = AssetDatabase.LoadAssetAtPath<Object>(path);
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
