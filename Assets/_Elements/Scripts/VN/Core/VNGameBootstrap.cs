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
            var viewStoryTopInstance = InstantiatePrefab(viewStoryTopPrefabPath, viewStoryTopParent);
            var vnGameControllerInstance = InstantiatePrefab(vnGameControllerPrefabPath, vnGameControllerParent);

            if (viewStoryTopInstance == null || vnGameControllerInstance == null)
            {
                Debug.LogError("[VNGameBootstrap] Required prefab instance is missing. Boot aborted.");
                return;
            }

            var controller = vnGameControllerInstance.GetComponent<VNGameController>();
            if (controller == null)
            {
                controller = vnGameControllerInstance.GetComponentInChildren<VNGameController>(true);
            }

            if (controller == null)
            {
                Debug.LogError("[VNGameBootstrap] VNGameController component not found on instantiated VNGameController prefab.");
                return;
            }

            var characterStage = viewStoryTopInstance.GetComponentInChildren<CharacterStageController>(true);
            var dialogue = viewStoryTopInstance.GetComponentInChildren<DialogueUIController>(true);
            var choice = viewStoryTopInstance.GetComponentInChildren<ChoiceUIController>(true);

            var background = controller.GetComponentInChildren<BackgroundController>(true);
            var audio = controller.GetComponentInChildren<AudioController>(true);
            var input = FindFirstObjectByType<VNInputRouter>();
            var loadingUi = viewStoryTopInstance.GetComponentInChildren<LoadingUIController>(true);

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