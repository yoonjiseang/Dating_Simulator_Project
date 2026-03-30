using VN.Controllers;
using VN.Data;
using VN.Systems;

namespace VN.Core
{
    public class CommandContext
    {
        public StoryRuntime Runtime { get; }
        public CommandData Data { get; }
        public ResourceProvider ResourceProvider { get; }
        public VariableStore Variables { get; }

        public CharacterStageController CharacterStage { get; }
        public BackgroundController Background { get; }
        public DialogueUIController DialogueUI { get; }
        public ChoiceUIController ChoiceUI { get; }
        public AudioController Audio { get; }

        public CommandContext(
            StoryRuntime runtime,
            CommandData data,
            ResourceProvider resourceProvider,
            VariableStore variables,
            CharacterStageController characterStage,
            BackgroundController background,
            DialogueUIController dialogueUI,
            ChoiceUIController choiceUI,
            AudioController audio)
        {
            Runtime = runtime;
            Data = data;
            ResourceProvider = resourceProvider;
            Variables = variables;
            CharacterStage = characterStage;
            Background = background;
            DialogueUI = dialogueUI;
            ChoiceUI = choiceUI;
            Audio = audio;
        }
    }
}