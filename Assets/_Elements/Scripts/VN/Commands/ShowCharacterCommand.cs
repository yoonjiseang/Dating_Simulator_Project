using System.Collections;
using VN.Core;

namespace VN.Commands
{
    public class ShowCharacterCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            var d = context.Data;
            var body = context.ResourceProvider.LoadCharacterBody(d.characterId, d.body);
            var face = context.ResourceProvider.LoadCharacterFace(d.characterId, d.face);
            yield return context.CharacterStage.ShowCharacter(d.characterId, d.slot, body, face, d.duration);
        }
    }
}