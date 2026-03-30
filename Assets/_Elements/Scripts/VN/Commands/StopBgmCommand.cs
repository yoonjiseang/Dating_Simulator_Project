using System.Collections;
using VN.Core;

namespace VN.Commands
{
    public class StopBgmCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            context.Audio.StopBgm();
            yield break;
        }
    }
}