using System.Collections;
using UnityEngine;
using VN.Core;

namespace VN.Commands
{
    public class WaitCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            var sec = context.Data.waitDuration > 0f ? context.Data.waitDuration : context.Data.duration;
            if (sec > 0f)
            {
                yield return new WaitForSeconds(sec);
            }
        }
    }
}