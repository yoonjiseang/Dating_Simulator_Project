using UnityEngine;
using VN.Controllers;
using VN.Systems;

namespace VN.Core
{
    /// <summary>
    /// Boot scene installer:
    /// 1) Instantiates ViewStoryTop + VNGameController prefabs from Resources/VN
    /// 2) Wires controllers inside ViewStoryTop into VNGameController
    /// </summary>
    public class VNGameBootstrap : MonoBehaviour
    {
        [Header("Resources Paths (under Resources/VN)")]
        [SerializeField] private string viewStoryTopPrefabPath = "VN/ViewStoryTop";
        [SerializeField] private string vnGameControllerPrefabPath = "VN/VNGameController";

        [Header("Optional Spawn Parents")]
        [SerializeField] private Transform viewStoryTopParent;
        [SerializeField] private Transform vnGameControllerParent;

        [Header("Optional Story Override")]
        [SerializeField] private string overrideStoryId;

        private void Awake()
        {
            var controller = VNGameController.Instance;
            if (controller == null)
            {
                var existingController = FindFirstObjectByType<VNGameController>(FindObjectsInactive.Include);
                if (existingController != null)
                {
                    controller = existingController;
                }
            }

            if (controller == null)
            {
                var vnGameControllerInstance = InstantiatePrefab(vnGameControllerPrefabPath, vnGameControllerParent);
                if (vnGameControllerInstance != null)
                {
                    controller = vnGameControllerInstance.GetComponent<VNGameController>()
                                 ?? vnGameControllerInstance.GetComponentInChildren<VNGameController>(true);
                }
            }

            if (controller == null)
            {
                Debug.LogError("[VNGameBootstrap] VNGameController component not found. Boot aborted.");
                return;
            }

            var characterStage = FindFirstObjectByType<CharacterStageController>(FindObjectsInactive.Include);
            var dialogue = FindFirstObjectByType<DialogueUIController>(FindObjectsInactive.Include);
            var choice = FindFirstObjectByType<ChoiceUIController>(FindObjectsInactive.Include);

            if (characterStage == null || dialogue == null || choice == null)
            {
                var viewStoryTopInstance = InstantiatePrefab(viewStoryTopPrefabPath, viewStoryTopParent);
                if (viewStoryTopInstance == null)
                {
                    Debug.LogError("[VNGameBootstrap] ViewStoryTop prefab could not be instantiated. Boot aborted.");
                    return;
                }

                characterStage = viewStoryTopInstance.GetComponentInChildren<CharacterStageController>(true);
                dialogue = viewStoryTopInstance.GetComponentInChildren<DialogueUIController>(true);
                choice = viewStoryTopInstance.GetComponentInChildren<ChoiceUIController>(true);
            }

            var background = controller.GetComponentInChildren<BackgroundController>(true);
            var audio = controller.GetComponentInChildren<AudioController>(true);
            background ??= FindFirstObjectByType<BackgroundController>(FindObjectsInactive.Include);
            audio ??= FindFirstObjectByType<AudioController>(FindObjectsInactive.Include);

            var input = VNInputRouter.Instance ?? FindFirstObjectByType<VNInputRouter>(FindObjectsInactive.Include);
            var loadingUi = FindFirstObjectByType<LoadingUIController>(FindObjectsInactive.Include);

            controller.ConfigureDependencies(
                characterStage,
                background,
                dialogue,
                choice,
                audio,
                input,
                loadingUi);

            if (!string.IsNullOrWhiteSpace(overrideStoryId))
            {
                controller.SetStoryId(overrideStoryId);
            }
        }

        private GameObject InstantiatePrefab(string resourcePath, Transform parent)
        {
            if (string.IsNullOrWhiteSpace(resourcePath))
            {
                Debug.LogError("[VNGameBootstrap] Resource path is empty.");
                return null;
            }

            var prefab = Resources.Load<GameObject>(resourcePath);
            if (prefab == null)
            {
                Debug.LogError($"[VNGameBootstrap] Could not load prefab at Resources path: {resourcePath}");
                return null;
            }

            return Instantiate(prefab, parent);
        }
    }
}