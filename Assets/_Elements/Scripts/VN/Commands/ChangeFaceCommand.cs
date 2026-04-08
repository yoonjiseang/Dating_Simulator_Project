using System.Collections;
using System.Threading.Tasks;
using UnityEngine;
using VN.Core;

namespace VN.Commands
{
    public class ChangeFaceCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            var d = context.Data;
            Task<Sprite> loadTask = context.ResourceProvider.LoadCharacterSpriteAsync(d.characterId, d.face);
            yield return ResourceProvider.WaitForTask(loadTask);
            var sprite = loadTask.Status == TaskStatus.RanToCompletion ? loadTask.Result : null;
            if (sprite == null)
            {
                Debug.LogError($"[ChangeFaceCommand] Failed to load character sprite. characterId={d.characterId}, face={d.face}");
            }
            yield return context.CharacterStage.ChangeCharacterSprite(d.characterId, sprite, d.GetEffectKey(), d.duration);
        }
    }
}