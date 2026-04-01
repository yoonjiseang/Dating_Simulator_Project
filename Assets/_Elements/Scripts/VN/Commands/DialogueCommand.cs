using System.Collections;
using VN.Core;

namespace VN.Commands
{
    public class DialogueCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            var d = context.Data;

            var effectKey = d.GetEffectKey();
            if (!string.IsNullOrWhiteSpace(effectKey) && !string.IsNullOrWhiteSpace(d.characterId))
            {
                yield return context.CharacterStage.PlayEffect(d.characterId, effectKey, d.duration);
            }

            if (!string.IsNullOrWhiteSpace(d.voice) && !string.IsNullOrWhiteSpace(d.characterId))
            {
                var voice = context.ResourceProvider.LoadVoice(d.characterId, d.voice);
                context.Audio.PlayVoice(voice);
            }

            context.Runtime.AddBacklog(d.speaker, d.text);
            yield return context.DialogueUI.ShowDialogue(d.speaker, d.text);
        }
    }
}