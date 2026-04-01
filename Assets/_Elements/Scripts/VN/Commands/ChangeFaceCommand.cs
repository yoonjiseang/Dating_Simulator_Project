using System.Collections;
using UnityEngine;
using VN.Core;

namespace VN.Commands
{
    public class ChangeFaceCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            var d = context.Data;
            var sprite = context.ResourceProvider.LoadCharacterSprite(d.characterId, d.face);
            if (sprite == null)
            {
                Debug.LogError($"[ChangeFaceCommand] Failed to load character sprite. characterId={d.characterId}, face={d.face}");
            }
            yield return context.CharacterStage.ChangeCharacterSprite(d.characterId, sprite, d.GetEffectKey(), d.duration);
        }
    }
}