using System.Collections;
using VN.Core;

namespace VN.Commands
{
    public class IfCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            var passed = context.Variables.Evaluate(context.Data.condition);
            var target = passed ? context.Data.@then : context.Data.@else;

            if (!string.IsNullOrWhiteSpace(target))
            {
                context.Runtime.JumpToNode(target);
            }

            yield break;
        }
    }
}