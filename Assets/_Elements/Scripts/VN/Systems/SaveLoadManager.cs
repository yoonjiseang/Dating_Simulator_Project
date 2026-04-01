using System;
using System.Collections.Generic;
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

        public void Load(string slot, StoryRuntime runtime, VariableStore variableStore, BackgroundController bg, CharacterStageController stage)
        {
            if (string.IsNullOrWhiteSpace(slot))
            {
                Debug.LogError("[SaveLoadManager] Load slot is empty.");
                return;
            }

            if (runtime == null || variableStore == null)
            {
                Debug.LogError("[SaveLoadManager] Load dependencies are missing.");
                return;
            }

            var json = PlayerPrefs.GetString(GetKey(slot), string.Empty);
            if (string.IsNullOrWhiteSpace(json))
            {
                Debug.LogWarning($"[SaveLoadManager] Save slot not found: {slot}");
                return;
            }

            var data = JsonUtility.FromJson<SaveData>(json);
            if (data == null)
            {
                Debug.LogError("[SaveLoadManager] Failed to parse save data.");
                return;
            }

            var dict = new Dictionary<string, int>();
            foreach (var v in data.variables)
            {
                dict[v.name] = v.value;
            }
            variableStore.Import(dict);

            runtime.JumpToNode(data.nodeId);
            // 명시적으로 인덱스를 맞춰서 저장 지점 복원
            for (var i = 0; i < data.commandIndex; i++)
            {
                runtime.AdvanceCommand();
            }

            if (!string.IsNullOrWhiteSpace(data.backgroundKey) && bg == null)
            {
                Debug.LogWarning("[SaveLoadManager] Background data exists but BackgroundController is not available.");
            }

            if (data.characters.Count > 0 && stage == null)
            {
                Debug.LogWarning("[SaveLoadManager] Character view data exists but CharacterStageController is not available.");
            }

            Debug.Log("[SaveLoadManager] Runtime state loaded. Visual restore is not implemented in the default save flow.");
        }

        private static string GetKey(string slot) => $"VN_SAVE_{slot}";
    }
}