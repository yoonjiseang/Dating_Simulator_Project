using System.Collections;
using VN.Core;

namespace VN.Commands
{
    public class BackgroundCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            var d = context.Data;
            var sprite = context.ResourceProvider.LoadBackground(d.bg);
            yield return context.Background.SetBackground(sprite, d.bg, d.transition, d.duration);
        }
    }
}