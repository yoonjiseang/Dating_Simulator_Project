using System.Collections;
using UnityEngine;
using VN.Core;

namespace VN.Commands
{
    public class StopBgmCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            if (context.Audio == null)
            {
                Debug.LogError("[StopBgmCommand] AudioController is not assigned in VNGameController.");
                yield break;
            }

            context.Audio.StopBgm();
            yield break;
        }
    }
}