using System.Collections;
using System.Collections.Generic;
using VN.Core;
using VN.Data;

namespace VN.Commands
{
    public class ChoiceCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            var filteredOptions = FilterOptions(context.Data.options, context);
            if (filteredOptions.Count == 0)
            {
                yield break;
            }

            yield return context.ChoiceUI.ShowChoices(filteredOptions, option =>
            {
                if (option?.set != null)
                {
                    context.Variables.Apply(option.set.name, option.set.op, option.set.value);
                }

                if (!string.IsNullOrWhiteSpace(option?.jump))
                {
                    context.Runtime.JumpToNode(option.jump);
                }
            });
        }
        
        private static List<ChoiceOptionData> FilterOptions(ChoiceOptionData[] options, CommandContext context)
        {
            var filtered = new List<ChoiceOptionData>();
            if (options == null)
            {
                return filtered;
            }

            for (var i = 0; i < options.Length; i++)
            {
                var option = options[i];
                if (option == null)
                {
                    continue;
                }

                if (string.IsNullOrWhiteSpace(option.condition) || context.Variables.Evaluate(option.condition))
                {
                    filtered.Add(option);
                }
            }

            return filtered;
        }
    }
}