using System.Collections;
using VN.Core;

namespace VN.Commands
{
    public class MoveCharacterCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            var d = context.Data;
            yield return context.CharacterStage.MoveCharacter(d.characterId, d.toSlot, d.duration);
        }
    }
}