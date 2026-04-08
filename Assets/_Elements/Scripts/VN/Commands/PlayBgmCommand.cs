using System.Collections;
using System.Threading.Tasks;
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

            Task<AudioClip> loadTask = context.ResourceProvider.LoadBgmAsync(context.Data.bgm);
            yield return ResourceProvider.WaitForTask(loadTask);
            var clip = loadTask.Status == TaskStatus.RanToCompletion ? loadTask.Result : null;
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