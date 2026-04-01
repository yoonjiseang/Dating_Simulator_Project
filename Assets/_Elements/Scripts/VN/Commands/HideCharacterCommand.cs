using System.Collections;
using VN.Core;

namespace VN.Commands
{
    public class HideCharacterCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            var d = context.Data;
            yield return context.CharacterStage.HideCharacter(d.characterId, d.duration, d.GetEffectKey());
        }
    }
}