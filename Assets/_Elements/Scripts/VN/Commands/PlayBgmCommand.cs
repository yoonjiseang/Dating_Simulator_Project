using System.Collections;
using VN.Core;

namespace VN.Commands
{
    public class PlayBgmCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            var clip = context.ResourceProvider.LoadBgm(context.Data.bgm);
            context.Audio.PlayBgm(clip);
            yield break;
        }
    }
}