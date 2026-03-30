using System.Collections;
using UnityEngine;
using VN.Core;

namespace VN.Commands
{
    public class BackgroundCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            var d = context.Data;

            if (context.Background == null)
            {
                Debug.LogError("[BackgroundCommand] BackgroundController is not assigned in VNGameController.");
                yield break;
            }

            var sprite = context.ResourceProvider.LoadBackground(d.bg);
            if (sprite == null)
            {
                Debug.LogWarning($"[BackgroundCommand] Background sprite not found for key='{d.bg}'. Skipping visual update.");
                yield break;
            }
            
            yield return context.Background.SetBackground(sprite, d.bg, d.transition, d.duration);
        }
    }
}