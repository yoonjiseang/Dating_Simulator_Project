using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;
using UnityEngine;
using VN.Controllers;
using VN.Core;

namespace VN.Systems
{
    public class SaveLoadManager
    {
        [Serializable]
        private class SaveData
        {
            public string nodeId;
            public int commandIndex;
            public string backgroundKey;
            public List<CharacterState> characters = new();
            public List<VariableState> variables = new();
        }

        [Serializable]
        private class CharacterState
        {
            public string characterId;
            public string slot;
            public string bodyKey;
            public string faceKey;
        }

        [Serializable]
        private class VariableState
        {
            public string name;
            public int value;
        }

        public void Save(string slot, StoryRuntime runtime, VariableStore variableStore, BackgroundController bg, CharacterStageController stage)
        {
            if (string.IsNullOrWhiteSpace(slot))
            {
                Debug.LogError("[SaveLoadManager] Save slot is empty.");
                return;
            }

            if (runtime == null || variableStore == null || bg == null || stage == null)
            {
                Debug.LogError("[SaveLoadManager] Save dependencies are missing.");
                return;
            }

            var data = new SaveData
            {
                nodeId = runtime.CurrentNode?.id,
                commandIndex = runtime.CommandIndex,
                backgroundKey = bg.CurrentBackgroundKey
            };

            foreach (var view in stage.ActiveViews)
            {
                data.characters.Add(new CharacterState
                {
                    characterId = view.characterId,
                    slot = view.slot,
                    bodyKey = view.bodyKey,
                    faceKey = view.faceKey
                });
            }

            foreach (var pair in variableStore.ExportCopy())
            {
                data.variables.Add(new VariableState { name = pair.Key, value = pair.Value });
            }

            var json = JsonUtility.ToJson(data);
            PlayerPrefs.SetString(GetKey(slot), json);
            PlayerPrefs.Save();
        }

        public IEnumerator Load(
            string slot,
            StoryRuntime runtime,
            VariableStore variableStore,
            BackgroundController bg,
            CharacterStageController stage,
            ResourceProvider resourceProvider)
        {
            if (string.IsNullOrWhiteSpace(slot))
            {
                Debug.LogError("[SaveLoadManager] Load slot is empty.");
                yield break;
            }

            if (runtime == null || variableStore == null)
            {
                Debug.LogError("[SaveLoadManager] Load dependencies are missing.");
                yield break;
            }

            var json = PlayerPrefs.GetString(GetKey(slot), string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning($"[SaveLoadManager] Save slot not found: {slot}");
                yield break;
            }

            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                Debug.LogError("[SaveLoadManager] Failed to parse save data.");
                yield break;
            }

            var dict = new Dictionary<string, int>();
            foreach (var v in data.variables ?? new List<VariableState>())
            {
                if (string.IsNullOrWhiteSpace(v?.name))
                {
                    continue;
                }

                dict[v.name] = v.value;
            }
            variableStore.Import(dict);

            runtime.JumpToNode(data.nodeId);
            if (!runtime.IsEnded && !runtime.TryRestoreCommandIndex(data.commandIndex))
            {
                Debug.LogWarning($"[SaveLoadManager] Invalid command index in save data: {data.commandIndex}. Fallback to node start.");
            }

            if (!string.IsNullOrWhiteSpace(data.backgroundKey) && bg == null)
            {
                Debug.LogWarning("[SaveLoadManager] Background data exists but BackgroundController is not available.");
            }

            if ((data.characters?.Count ?? 0) > 0 && stage == null)
            {
                Debug.LogWarning("[SaveLoadManager] Character view data exists but CharacterStageController is not available.");
            }

            if (resourceProvider == null)
            {
                Debug.LogWarning("[SaveLoadManager] ResourceProvider is missing. Visual restore will be skipped.");
                yield break;
            }

            yield return RestoreBackground(data, bg, resourceProvider);
            yield return RestoreCharacters(data, stage, resourceProvider);

            Debug.Log("[SaveLoadManager] Runtime + visual state restored from save data.");
        }

        private static string GetKey(string slot) => $"VN_SAVE_{slot}";

        private static IEnumerator RestoreBackground(SaveData data, BackgroundController bg, ResourceProvider resourceProvider)
        {
            if (data == null || bg == null || resourceProvider == null || string.IsNullOrWhiteSpace(data.backgroundKey))
            {
                yield break;
            }

            var sprite = resourceProvider.LoadBackground(data.backgroundKey);
            if (sprite == null)
            {
                Debug.LogWarning($"[SaveLoadManager] Failed to restore background sprite: {data.backgroundKey}");
                yield break;
            }

            yield return bg.SetBackground(sprite, data.backgroundKey, string.Empty, 0f);
        }

        private static IEnumerator RestoreCharacters(SaveData data, CharacterStageController stage, ResourceProvider resourceProvider)
        {
            if (data == null || stage == null || resourceProvider == null)
            {
                yield break;
            }

            var activeIds = stage.ActiveViews?.Select(v => v.characterId).Where(id => !string.IsNullOrWhiteSpace(id)).ToArray();
            if (activeIds != null)
            {
                for (var i = 0; i < activeIds.Length; i++)
                {
                    yield return stage.HideCharacter(activeIds[i], 0f, string.Empty);
                }
            }

            if (data.characters == null || data.characters.Count == 0)
            {
                yield break;
            }

            foreach (var character in data.characters)
            {
                if (character == null || string.IsNullOrWhiteSpace(character.characterId))
                {
                    continue;
                }

                var sprite = ResolveCharacterSprite(resourceProvider, character);
                if (sprite == null)
                {
                    Debug.LogWarning($"[SaveLoadManager] Failed to restore character sprite. characterId={character.characterId}, bodyKey={character.bodyKey}, faceKey={character.faceKey}");
                    continue;
                }

                yield return stage.ShowCharacter(character.characterId, character.slot, sprite, null, 0f, string.Empty);
            }
        }

        private static Sprite ResolveCharacterSprite(ResourceProvider resourceProvider, CharacterState character)
        {
            if (resourceProvider == null || character == null || string.IsNullOrWhiteSpace(character.characterId))
            {
                return null;
            }

            if (!string.IsNullOrWhiteSpace(character.faceKey))
            {
                var faceSprite = resourceProvider.LoadCharacterSprite(character.characterId, character.faceKey);
                if (faceSprite != null)
                {
                    return faceSprite;
                }
            }

            if (!string.IsNullOrWhiteSpace(character.bodyKey))
            {
                var bodyKey = character.bodyKey.Trim();
                var separatorIndex = bodyKey.LastIndexOf('_');
                if (separatorIndex >= 0 && separatorIndex < bodyKey.Length - 1)
                {
                    var faceCandidate = bodyKey[(separatorIndex + 1)..];
                    var spriteByParsedFace = resourceProvider.LoadCharacterSprite(character.characterId, faceCandidate);
                    if (spriteByParsedFace != null)
                    {
                        return spriteByParsedFace;
                    }
                }
            }

            return null;
        }
    }
}