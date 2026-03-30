using System.Collections;
using VN.Core;

namespace VN.Commands
{
    public class SetVariableCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            context.Variables.Apply(context.Data.name, context.Data.op, context.Data.value);
            yield break;
        }
    }
}