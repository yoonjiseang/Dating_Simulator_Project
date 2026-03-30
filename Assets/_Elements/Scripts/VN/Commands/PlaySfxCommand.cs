using System.Collections;
using VN.Core;

namespace VN.Commands
{
    public class PlaySfxCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            var clip = context.ResourceProvider.LoadSfx(context.Data.sfx);
            context.Audio.PlaySfx(clip);
            yield break;
        }
    }
}