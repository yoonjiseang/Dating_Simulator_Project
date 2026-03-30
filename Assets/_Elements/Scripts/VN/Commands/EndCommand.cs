using System.Collections;
using VN.Core;

namespace VN.Commands
{
    public class EndCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            context.Runtime.End();
            yield break;
        }
    }
}