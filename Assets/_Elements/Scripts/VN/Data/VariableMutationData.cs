using System;

namespace VN.Data
{
    [Serializable]
    public class VariableMutationData
    {
        public string name;
        public string op;
        public int value;
    }
}