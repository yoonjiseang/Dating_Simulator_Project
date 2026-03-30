using System.Collections.Generic;
using UnityEngine;

namespace VN.Core
{
    public class ResourceProvider
    {
        private readonly Dictionary<string, Object> _cache = new();

        public Sprite LoadBackground(string bgKey) => LoadSprite($"_ElementsResources/VN/Backgrounds/{bgKey}");
        public AudioClip LoadBgm(string bgmKey) => LoadAudio($"_ElementsResources/VN/BGM/{bgmKey}");
        public AudioClip LoadSfx(string sfxKey) => LoadAudio($"_ElementsResources/VN/SFX/{sfxKey}");
        public AudioClip LoadVoice(string characterId, string voiceKey) => LoadAudio($"_ElementsResources/VN/Characters/{characterId}/voice/{voiceKey}");
        public Sprite LoadCharacterBody(string characterId, string bodyKey) => LoadSprite($"_ElementsResources/VN/Characters/{characterId}/body_{bodyKey}");
        public Sprite LoadCharacterFace(string characterId, string faceKey) => LoadSprite($"_ElementsResources/VN/Characters/{characterId}/face_{faceKey}");

        private Sprite LoadSprite(string path)
        {
            return LoadCached<Sprite>(path);
        }

        private AudioClip LoadAudio(string path)
        {
            return LoadCached<AudioClip>(path);
        }

        private T LoadCached<T>(string path) where T : Object
        {
            if (string.IsNullOrWhiteSpace(path))
            {
                Debug.LogError("[ResourceProvider] Empty path.");
                return null;
            }

            if (_cache.TryGetValue(path, out var cached))
            {
                return cached as T;
            }

            var loaded = Resources.Load<T>(path);
            if (loaded == null)
            {
                Debug.LogError($"[ResourceProvider] Resource not found: {path}");
                return null;
            }

            _cache[path] = loaded;
            return loaded;
        }
    }
}