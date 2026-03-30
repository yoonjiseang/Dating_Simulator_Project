using System.Collections;
using VN.Core;

namespace VN.Commands
{
    public class HideCharacterCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            context.CharacterStage.HideCharacter(context.Data.characterId);
            yield break;
        }
    }
}