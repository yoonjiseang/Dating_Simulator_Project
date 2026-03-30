using System.Collections;
using VN.Core;

namespace VN.Commands
{
    public class JumpCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            context.Runtime.JumpToNode(context.Data.targetNodeId);
            yield break;
        }
    }
}