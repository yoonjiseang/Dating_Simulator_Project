using System;

namespace VN.Data
{
    [Serializable]
    public class ChoiceOptionData
    {
        public string text;
        public string jump;
        public string condition;
        public VariableMutationData set;
    }
}