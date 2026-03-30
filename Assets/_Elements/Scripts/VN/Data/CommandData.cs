using System;

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
    }
}