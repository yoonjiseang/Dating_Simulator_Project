using System.Collections;
using UnityEngine;
using VN.Core;

namespace VN.Commands
{
    public class PlayBgmCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            if (context.Audio == null)
            {
                Debug.LogError("[PlayBgmCommand] AudioController is not assigned in VNGameController.");
                yield break;
            }

            var clip = context.ResourceProvider.LoadBgm(context.Data.bgm);
            if (clip == null)
            {
                Debug.LogWarning($"[PlayBgmCommand] BGM clip not found for key='{context.Data.bgm}'.");
                yield break;
            }
            context.Audio.PlayBgm(clip);
            yield break;
        }
    }
}