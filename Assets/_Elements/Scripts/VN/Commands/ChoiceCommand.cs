using System.Collections;
using VN.Core;

namespace VN.Commands
{
    public class ChoiceCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            yield return context.ChoiceUI.ShowChoices(context.Data.options, option =>
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
    }
}