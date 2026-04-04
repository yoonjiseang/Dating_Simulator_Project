using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using VN.Core;

namespace VN.Commands
{
    public class ShowCharacterCommand : IVNCommand
    {
        public IEnumerator Execute(CommandContext context)
        {
            var d = context.Data;
            var characterIds = d.GetCharacterIds();
            if (characterIds.Length == 0)
            {
                Debug.LogError("[ShowCharacterCommand] characterId is empty.");
                yield break;
            }

            var slots = d.GetSlots();
            var faces = d.GetFaces();
            var effect = d.GetEffectKey();
            var routines = new List<IEnumerator>(characterIds.Length);

            for (var i = 0; i < characterIds.Length; i++)
            {
                var characterId = characterIds[i];
                var slot = ResolveValueByIndex(slots, i, d.slot);
                var face = ResolveValueByIndex(faces, i, d.face);
                var sprite = context.ResourceProvider.LoadCharacterSprite(characterId, face);

                if (sprite == null)
                {
                    Debug.LogError($"[ShowCharacterCommand] Failed to load character sprite. characterId={characterId}, face={face}");
                }

                routines.Add(context.CharacterStage.ShowCharacter(characterId, slot, sprite, null, d.duration, effect));
            }

            yield return RunCoroutinesInParallel(context, routines);
        }

        private static string ResolveValueByIndex(string[] values, int index, string fallback)
        {
            if (values == null || values.Length == 0)
            {
                return fallback;
            }

            if (index < values.Length)
            {
                return values[index];
            }

            return values[values.Length - 1];
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