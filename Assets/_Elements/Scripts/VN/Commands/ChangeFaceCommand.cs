using System.Collections;
using VN.Core;

namespace VN.Commands
{
    public class ChangeFaceCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            var d = context.Data;
            var face = context.ResourceProvider.LoadCharacterFace(d.characterId, d.face);
            context.CharacterStage.ChangeFace(d.characterId, face);
            yield break;
        }
    }
}