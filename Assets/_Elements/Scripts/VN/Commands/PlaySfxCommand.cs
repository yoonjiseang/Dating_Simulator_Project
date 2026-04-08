using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using VN.Core;

namespace VN.Commands
{
    public class PlaySfxCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            if (context == null)
            {
                Debug.LogError("[PlaySfxCommand] CommandContext is null.");
                yield break;
            }

            if (context.Audio == null)
            {
                Debug.LogError("[PlaySfxCommand] AudioController is not assigned in VNGameController.");
                yield break;
            }

            if (context.ResourceProvider == null)
            {
                Debug.LogError("[PlaySfxCommand] ResourceProvider is not available.");
                yield break;
            }

            if (context.Data == null)
            {
                Debug.LogError("[PlaySfxCommand] CommandData is null.");
                yield break;
            }

            Task<AudioClip> loadTask = context.ResourceProvider.LoadSfxAsync(context.Data.sfx);
            yield return ResourceProvider.WaitForTask(loadTask);
            var clip = loadTask.Status == TaskStatus.RanToCompletion ? loadTask.Result : null;
            if (clip == null)
            {
                Debug.LogWarning($"[PlaySfxCommand] SFX clip not found for key='{context.Data.sfx}'.");
                yield break;
            }

            context.Audio.PlaySfx(clip);
            yield break;
        }
    }
}