using System.Collections;
using UnityEngine;
using VN.Commands;
using VN.Controllers;
using VN.Systems;

namespace VN.Core
{
    public class CommandProcessor
    {
        private readonly StoryRuntime _runtime;
        private readonly ResourceProvider _resourceProvider;
        private readonly VariableStore _variables;
        private readonly CharacterStageController _characterStage;
        private readonly BackgroundController _background;
        private readonly DialogueUIController _dialogueUi;
        private readonly ChoiceUIController _choiceUi;
        private readonly AudioController _audio;

        public CommandProcessor(
            StoryRuntime runtime,
            ResourceProvider resourceProvider,
            VariableStore variables,
            CharacterStageController characterStage,
            BackgroundController background,
            DialogueUIController dialogueUi,
            ChoiceUIController choiceUi,
            AudioController audio)
        {
            _runtime = runtime;
            _resourceProvider = resourceProvider;
            _variables = variables;
            _characterStage = characterStage;
            _background = background;
            _dialogueUi = dialogueUi;
            _choiceUi = choiceUi;
            _audio = audio;
        }

        public IEnumerator Run()
        {
            while (!_runtime.IsEnded)
            {
                var data = _runtime.GetCurrentCommand();
                if (data == null)
                {
                    _runtime.End();
                    yield break;
                }

                var command = CreateCommand(data.type);
                if (command == null)
                {
                    Debug.LogError($"[CommandProcessor] Unknown command type: {data.type}");
                    _runtime.AdvanceCommand();
                    continue;
                }

                var context = new CommandContext(
                    _runtime,
                    data,
                    _resourceProvider,
                    _variables,
                    _characterStage,
                    _background,
                    _dialogueUi,
                    _choiceUi,
                    _audio);

                yield return command.Execute(context);

                if (!_runtime.IsEnded)
                {
                    _runtime.AdvanceCommand();
                }
            }
        }

        private static IVNCommand CreateCommand(string type)
        {
            return type switch
            {
                "dialogue" => new DialogueCommand(),
                "background" => new BackgroundCommand(),
                "showCharacter" => new ShowCharacterCommand(),
                "hideCharacter" => new HideCharacterCommand(),
                "changeFace" => new ChangeFaceCommand(),
                "moveCharacter" => new MoveCharacterCommand(),
                "playBgm" => new PlayBgmCommand(),
                "stopBgm" => new StopBgmCommand(),
                "playSfx" => new PlaySfxCommand(),
                "wait" => new WaitCommand(),
                "choice" => new ChoiceCommand(),
                "jump" => new JumpCommand(),
                "if" => new IfCommand(),
                "setVariable" => new SetVariableCommand(),
                "end" => new EndCommand(),
                _ => null
            };
        }
    }
}