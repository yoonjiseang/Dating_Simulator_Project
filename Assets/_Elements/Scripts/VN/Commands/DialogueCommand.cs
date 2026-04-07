using System.Collections;
using UnityEngine;
using VN.Core;

namespace VN.Commands
{
    public class DialogueCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            if (context == null)
            {
                Debug.LogError("[DialogueCommand] CommandContext is null.");
                yield break;
            }

            var d = context.Data;
            if (d == null)
            {
                Debug.LogError("[DialogueCommand] CommandData is null.");
                yield break;
            }

            if (context.Runtime == null)
            {
                Debug.LogError("[DialogueCommand] StoryRuntime is not available.");
                yield break;
            }

            if (context.DialogueUI == null)
            {
                Debug.LogError("[DialogueCommand] DialogueUIController is not assigned in VNGameController.");
                yield break;
            }

            var effectKey = d.GetEffectKey();
            if (!string.IsNullOrWhiteSpace(effectKey) && !string.IsNullOrWhiteSpace(d.characterId))
            {
                if (context.CharacterStage == null)
                {
                    Debug.LogWarning("[DialogueCommand] CharacterStageController is not assigned. Effect command will be skipped.");
                }
                else
                {
                    yield return context.CharacterStage.PlayEffect(d.characterId, effectKey, d.duration);
                }
            }

            if (!string.IsNullOrWhiteSpace(d.voice) && !string.IsNullOrWhiteSpace(d.characterId))
            {
                if (context.ResourceProvider == null)
                {
                    Debug.LogWarning("[DialogueCommand] ResourceProvider is not available. Voice playback will be skipped.");
                }
                else if (context.Audio == null)
                {
                    Debug.LogWarning("[DialogueCommand] AudioController is not assigned. Voice playback will be skipped.");
                }
                else
                {
                    var voice = context.ResourceProvider.LoadVoice(d.characterId, d.voice);
                    if (voice == null)
                    {
                        Debug.LogWarning($"[DialogueCommand] Voice clip not found. characterId={d.characterId}, voice={d.voice}");
                    }
                    else
                    {
                        context.Audio.PlayVoice(voice);
                    }
                }
            }

            context.Runtime.AddBacklog(d.speaker, d.text);
            yield return context.DialogueUI.ShowDialogue(d.speaker, d.text);
        }
    }
}