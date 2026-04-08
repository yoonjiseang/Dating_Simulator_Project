using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using VN.Controllers;
using VN.Systems;

namespace VN.Core
{
    public class VNGameController : MonoBehaviour
    {
        public static VNGameController Instance { get; private set; }

        [Header("Story")]
        [SerializeField] private string storyId = "storydata_0000001";
        [SerializeField] private bool autoStartOnAwake = true;
        [SerializeField] private bool dontDestroyOnLoad = true;

        [Header("Controllers")]
        [SerializeField] private CharacterStageController characterStageController;
        [SerializeField] private BackgroundController backgroundController;
        [SerializeField] private DialogueUIController dialogueUiController;
        [SerializeField] private ChoiceUIController choiceUiController;
        [SerializeField] private AudioController audioController;
        [SerializeField] private VNInputRouter inputRouter;

        [Header("Loading UI (Optional)")]
        [SerializeField] private LoadingUIController loadingUiController;

        private StoryLoader _storyLoader;
        private StoryRuntime _runtime;
        private ResourceProvider _resourceProvider;
        private VariableStore _variableStore;
        private CommandProcessor _processor;
        private SaveLoadManager _saveLoadManager;
        private Coroutine _processorCoroutine;

        private void Awake()
        {
            var rootObject = transform.root.gameObject;

            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[VNGameController] Duplicate instance detected. Destroying the new one.");
                Destroy(rootObject);
                return;
            }

            Instance = this;

            if (dontDestroyOnLoad)
            {
                DontDestroyOnLoad(rootObject);
            }
        }

        private void OnDestroy()
        {
            if (Instance == this)
            {
                Instance = null;
            }

            _resourceProvider?.ReleaseAll();
            if (_processorCoroutine != null)
            {
                StopCoroutine(_processorCoroutine);
                _processorCoroutine = null;
            }
        }

        private IEnumerator Start()
        {
            if (!autoStartOnAwake)
            {
                yield break;
            }

            yield return StartCoroutine(BeginStory());
        }

        public IEnumerator BeginStory()
        {
            ResolveOptionalDependencies();
            
            if (!ValidateDependencies())
            {
                yield break;
            }

            SetLoadingVisible(true);
            UpdateLoadingProgress(0f);

            _resourceProvider?.ReleaseAll();
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
                SetLoadingVisible(false);
                yield break;
            }

            yield return StartCoroutine(_resourceProvider.PreloadStoryAssets(story, (progress, _) =>
            {
                UpdateLoadingProgress(progress);
            }));

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

            SetLoadingVisible(false);
            _processorCoroutine = StartCoroutine(_processor.Run());
        }
        
        public void ConfigureDependencies(
            CharacterStageController characterStage,
            BackgroundController background,
            DialogueUIController dialogue,
            ChoiceUIController choice,
            AudioController audio,
            VNInputRouter input,
            LoadingUIController loadingUi)
        {
            characterStageController = characterStage;
            backgroundController = background;
            dialogueUiController = dialogue;
            choiceUiController = choice;
            audioController = audio;
            inputRouter = input;
            loadingUiController = loadingUi;
        }

        public void SetStoryId(string id)
        {
            if (!string.IsNullOrWhiteSpace(id))
            {
                storyId = id;
            }
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

            StartCoroutine(LoadAndResume(slot));
        }

        private IEnumerator LoadAndResume(string slot)
        {
            SetLoadingVisible(true);
            UpdateLoadingProgress(0f);
            inputRouter?.AcquireInputBlock();

            try
            {
                if (_processorCoroutine != null)
                {
                    StopCoroutine(_processorCoroutine);
                    _processorCoroutine = null;
                }

                yield return StartCoroutine(_saveLoadManager.Load(
                    slot,
                    _runtime,
                    _variableStore,
                    backgroundController,
                    characterStageController,
                    _resourceProvider));

                if (!_runtime.IsEnded)
                {
                    _processor = new CommandProcessor(
                        _runtime,
                        _resourceProvider,
                        _variableStore,
                        characterStageController,
                        backgroundController,
                        dialogueUiController,
                        choiceUiController,
                        audioController);

                    _processorCoroutine = StartCoroutine(_processor.Run());
                }
            }
            finally
            {
                SetLoadingVisible(false);
                UpdateLoadingProgress(1f);
                inputRouter?.ReleaseInputBlock();
            }
        }

        private void ResolveOptionalDependencies()
        {
            if (loadingUiController == null)
            {
                loadingUiController = GetComponentInChildren<LoadingUIController>(true);
            }

            if (loadingUiController == null)
            {
                loadingUiController = FindFirstObjectByType<LoadingUIController>();
            }
        }

        private bool ValidateDependencies()
        {
            var hasAllDependencies = true;

            if (characterStageController == null)
            {
                Debug.LogError("[VNGameController] characterStageController is not assigned.");
                hasAllDependencies = false;
            }

            if (backgroundController == null)
            {
                Debug.LogError("[VNGameController] backgroundController is not assigned.");
                hasAllDependencies = false;
            }

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

            if (audioController == null)
            {
                Debug.LogError("[VNGameController] audioController is not assigned.");
                hasAllDependencies = false;
            }

            if (loadingUiController == null)
            {
                Debug.LogWarning("[VNGameController] loadingUiController is not assigned. Loading progress UI will be skipped.");
            }

            return hasAllDependencies;
        }

        private void SetLoadingVisible(bool visible)
        {
            if (loadingUiController != null)
            {
                loadingUiController.SetVisible(visible);
            }
        }

        private void UpdateLoadingProgress(float progress)
        {
            if (loadingUiController != null)
            {
                loadingUiController.SetProgress(progress);
            }
        }
    }
}