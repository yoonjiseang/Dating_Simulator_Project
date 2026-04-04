using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VN.Core;

namespace VN.Commands
{
    public class HideCharacterCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            var d = context.Data;
            var characterIds = d.GetCharacterIds();
            if (characterIds.Length == 0)
            {
                Debug.LogError("[HideCharacterCommand] characterId is empty.");
                yield break;
            }

            var routines = new List<IEnumerator>(characterIds.Length);
            for (var i = 0; i < characterIds.Length; i++)
            {
                routines.Add(context.CharacterStage.HideCharacter(characterIds[i], d.duration, d.GetEffectKey()));
            }

            yield return RunCoroutinesInParallel(context, routines);
        }

        private static IEnumerator RunCoroutinesInParallel(CommandContext context, IReadOnlyList<IEnumerator> routines)
        {
            if (routines == null || routines.Count == 0)
            {
                yield break;
            }

            var completedCount = 0;
            for (var i = 0; i < routines.Count; i++)
            {
                context.CharacterStage.StartCoroutine(RunWithCompletion(routines[i], () => completedCount++));
            }

            while (completedCount < routines.Count)
            {
                yield return null;
            }
        }

        private static IEnumerator RunWithCompletion(IEnumerator routine, System.Action onComplete)
        {
            if (routine != null)
            {
                yield return routine;
            }

            onComplete?.Invoke();
        }
    }
}