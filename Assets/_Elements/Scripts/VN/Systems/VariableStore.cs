using System;
using System.Collections.Generic;
using System.Text.RegularExpressions;
using UnityEngine;

namespace VN.Systems
{
    public class VariableStore
    {
        private static readonly Regex ConditionRegex = new(@"^\s*(\w+)\s*(==|!=|>=|<=|>|<)\s*(-?\d+)\s*$");
        private readonly Dictionary<string, int> _variables = new();

        public IReadOnlyDictionary<string, int> Variables => _variables;

        public void Apply(string name, string op, int value)
        {
            if (string.IsNullOrWhiteSpace(name))
            {
                Debug.LogError("[VariableStore] Variable name is empty.");
                return;
            }

            _variables.TryGetValue(name, out var current);
            switch (op)
            {
                case "set":
                    _variables[name] = value;
                    break;
                case "add":
                    _variables[name] = current + value;
                    break;
                case "sub":
                case "subtract":
                    _variables[name] = current - value;
                    break;
                default:
                    Debug.LogError($"[VariableStore] Unknown op: {op}");
                    break;
            }
        }

        public int GetValue(string name)
        {
            return _variables.TryGetValue(name, out var value) ? value : 0;
        }

        public bool Evaluate(string condition)
        {
            if (string.IsNullOrWhiteSpace(condition))
            {
                return false;
            }

            var match = ConditionRegex.Match(condition);
            if (!match.Success)
            {
                Debug.LogError($"[VariableStore] Invalid condition: {condition}");
                return false;
            }

            var left = GetValue(match.Groups[1].Value);
            var op = match.Groups[2].Value;
            var right = int.Parse(match.Groups[3].Value);

            return op switch
            {
                "==" => left == right,
                "!=" => left != right,
                ">" => left > right,
                "<" => left < right,
                ">=" => left >= right,
                "<=" => left <= right,
                _ => throw new ArgumentOutOfRangeException()
            };
        }

        public Dictionary<string, int> ExportCopy()
        {
            return new Dictionary<string, int>(_variables);
        }

        public void Import(Dictionary<string, int> values)
        {
            _variables.Clear();
            if (values == null) return;
            foreach (var pair in values)
            {
                _variables[pair.Key] = pair.Value;
            }
        }
    }
}