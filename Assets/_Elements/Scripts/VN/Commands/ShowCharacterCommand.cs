using System.Collections;
using UnityEngine;
using VN.Core;

namespace VN.Commands
{
    public class ShowCharacterCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            var d = context.Data;
            var sprite = context.ResourceProvider.LoadCharacterSprite(d.characterId, d.face);
            if (sprite == null)
            {
                Debug.LogError($"[ShowCharacterCommand] Failed to load character sprite. characterId={d.characterId}, face={d.face}");
            }
            yield return context.CharacterStage.ShowCharacter(d.characterId, d.slot, sprite, null, d.duration);
        }
    }
}