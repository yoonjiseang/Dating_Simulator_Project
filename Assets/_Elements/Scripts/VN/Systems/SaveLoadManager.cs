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

            Debug.Log("[SaveLoadManager] Runtime state loaded. Visual restore should be applied by a custom restore flow.");
        }

        private static string GetKey(string slot) => $"VN_SAVE_{slot}";
    }
}