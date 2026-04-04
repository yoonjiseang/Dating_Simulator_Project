using System;
using System.Collections.Generic;

namespace VN.Data
{
    [Serializable]
    public class CommandData
    {
        public string type;

        // dialogue
        public string speaker;
        public string characterId;
        public string text;
        public string voice;

        // background
        public string bg;
        public string transition;
        public string sort;
        public float duration = 0.2f;

        // character
        public string slot;
        public string body;
        public string face;
        public string toSlot;

        // audio
        public string bgm;
        public string sfx;

        // wait
        public float waitDuration;

        // choice
        public ChoiceOptionData[] options;

        // jump
        public string targetNodeId;

        // if
        public string condition;
        public string @then;
        public string @else;

        // variable
        public string name;
        public string op;
        public int value;

        public string GetEffectKey()
        {
            return string.IsNullOrWhiteSpace(sort) ? transition : sort;
        }
        
        public string[] GetCharacterIds()
        {
            return SplitMultiValue(characterId);
        }

        public string[] GetSlots()
        {
            return SplitMultiValue(slot);
        }

        public string[] GetFaces()
        {
            return SplitMultiValue(face);
        }

        public static string[] SplitMultiValue(string value)
        {
            if (string.IsNullOrWhiteSpace(value))
            {
                return Array.Empty<string>();
            }

            var chunks = value.Split(new[] { ',', '|', ';' }, StringSplitOptions.RemoveEmptyEntries);
            var results = new List<string>(chunks.Length);
            foreach (var chunk in chunks)
            {
                var trimmed = chunk.Trim();
                if (!string.IsNullOrWhiteSpace(trimmed))
                {
                    results.Add(trimmed);
                }
            }

            return results.ToArray();
        }
    }
}