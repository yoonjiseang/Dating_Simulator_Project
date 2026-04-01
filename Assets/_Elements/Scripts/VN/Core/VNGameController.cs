using System.Collections;
using UnityEngine;
using VN.Controllers;
using VN.Systems;

namespace VN.Core
{
    public class VNGameController : MonoBehaviour
    {
        [Header("Story")]
        [SerializeField] private string storyId = "storydata_0000001";

        [Header("Controllers")]
        [SerializeField] private CharacterStageController characterStageController;
        [SerializeField] private BackgroundController backgroundController;
        [SerializeField] private DialogueUIController dialogueUiController;
        [SerializeField] private ChoiceUIController choiceUiController;
        [SerializeField] private AudioController audioController;
        [SerializeField] private VNInputRouter inputRouter;

        private StoryLoader _storyLoader;
        private StoryRuntime _runtime;
        private ResourceProvider _resourceProvider;
        private VariableStore _variableStore;
        private CommandProcessor _processor;
        private SaveLoadManager _saveLoadManager;

        private IEnumerator Start()
        {
            if (!ValidateDependencies())
            {
                yield break;
            }

            _storyLoader = new StoryLoader();
            _runtime = new StoryRuntime();
            _resourceProvider = new ResourceProvider();
            _variableStore = new VariableStore();
            _saveLoadManager = new SaveLoadManager();

            dialogueUiController.Initialize(inputRouter);
            choiceUiController.Initialize();

            var story = _storyLoader.LoadStory(storyId);
            if (story == null)
            {
                yield break;
            }

            _runtime.Initialize(story);

            _processor = new CommandProcessor(
                _runtime,
                _resourceProvider,
                _variableStore,
                characterStageController,
                backgroundController,
                dialogueUiController,
                choiceUiController,
                audioController);

            yield return StartCoroutine(_processor.Run());
        }

        public void Save(string slot)
        {
            if (_saveLoadManager == null || _runtime == null || _variableStore == null)
            {
                Debug.LogError("[VNGameController] Save requested before VN runtime was initialized.");
                return;
            }

            _saveLoadManager.Save(slot, _runtime, _variableStore, backgroundController, characterStageController);
        }

        public void Load(string slot)
        {
            if (_saveLoadManager == null || _runtime == null || _variableStore == null)
            {
                Debug.LogError("[VNGameController] Load requested before VN runtime was initialized.");
                return;
            }

            _saveLoadManager.Load(slot, _runtime, _variableStore, backgroundController, characterStageController);
        }

        private bool ValidateDependencies()
        {
            var hasAllDependencies = true;

            if (dialogueUiController == null)
            {
                Debug.LogError("[VNGameController] dialogueUiController is not assigned.");
                hasAllDependencies = false;
            }

            if (choiceUiController == null)
            {
                Debug.LogError("[VNGameController] choiceUiController is not assigned.");
                hasAllDependencies = false;
            }

            if (inputRouter == null)
            {
                Debug.LogError("[VNGameController] inputRouter is not assigned.");
                hasAllDependencies = false;
            }

            return hasAllDependencies;
        }
    }
}