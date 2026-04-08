using System;
using System.Threading.Tasks;
using UnityEngine;
using UnityEngine.AddressableAssets;
using VN.Controllers;
using VN.Systems;

namespace VN.Core
{
    /// <summary>
    /// Boot scene installer:
    /// 1) Instantiates ViewStoryTop + VNGameController prefabs from Addressables keys
    /// 2) Wires controllers inside ViewStoryTop into VNGameController
    /// </summary>
    public class VNGameBootstrap : MonoBehaviour
    {
        [Header("Addressable Keys")]
        [SerializeField] private string viewStoryTopPrefabAddress = "VN/ViewStoryTop";
        [SerializeField] private string vnGameControllerPrefabAddress = "VN/VNGameController";

        [Header("Optional Spawn Parents")]
        [SerializeField] private Transform viewStoryTopParent;
        [SerializeField] private Transform vnGameControllerParent;

        [Header("Optional Story Override")]
        [SerializeField] private string overrideStoryId;

        [Header("Mouse Advance Area")]
        [SerializeField] private string mouseAdvanceAreaObjectName = "InputSquare";

        private async void Awake()
        {
            await BootstrapAsync();
        }

        private async Task BootstrapAsync()
        {
            var controller = VNGameController.Instance;
            var controllerWasEnabled = false;
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
                var vnGameControllerInstance = await InstantiatePrefabAsync(vnGameControllerPrefabAddress, vnGameControllerParent);
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

            // Addressables loading is async. Prevent VNGameController.Start from running
            // before dependencies are wired.
            controllerWasEnabled = controller.enabled;
            if (controllerWasEnabled)
            {
                controller.enabled = false;
            }

            try
            {
                GameObject viewStoryTopInstance = null;
                var characterStage = FindFirstObjectByType<CharacterStageController>(FindObjectsInactive.Include);
                var dialogue = FindFirstObjectByType<DialogueUIController>(FindObjectsInactive.Include);
                var choice = FindFirstObjectByType<ChoiceUIController>(FindObjectsInactive.Include);

                if (characterStage == null || dialogue == null || choice == null)
                {
                    viewStoryTopInstance = await InstantiatePrefabAsync(viewStoryTopPrefabAddress, viewStoryTopParent);
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

                if (input != null)
                {
                    var mouseAdvanceArea = FindMouseAdvanceArea(viewStoryTopInstance);
                    if (mouseAdvanceArea != null)
                    {
                        input.SetMouseAdvanceArea(mouseAdvanceArea);
                    }
                    else
                    {
                        Debug.LogWarning($"[VNGameBootstrap] Mouse advance area '{mouseAdvanceAreaObjectName}' not found in ViewStoryTop.");
                    }
                }

                if (!string.IsNullOrWhiteSpace(overrideStoryId))
                {
                    controller.SetStoryId(overrideStoryId);
                }
            }
            finally
            {
                if (controllerWasEnabled && !controller.enabled)
                {
                    controller.enabled = true;
                }
            }
        }

        private RectTransform FindMouseAdvanceArea(GameObject viewStoryTopInstance)
        {
            if (string.IsNullOrWhiteSpace(mouseAdvanceAreaObjectName))
            {
                return null;
            }

            if (viewStoryTopInstance != null)
            {
                var areaInNewView = FindRectTransformByName(viewStoryTopInstance.transform, mouseAdvanceAreaObjectName);
                if (areaInNewView != null)
                {
                    return areaInNewView;
                }
            }

            var allRectTransforms = FindObjectsOfType<RectTransform>(true);
            for (var i = 0; i < allRectTransforms.Length; i++)
            {
                var rect = allRectTransforms[i];
                if (rect != null && rect.name == mouseAdvanceAreaObjectName)
                {
                    return rect;
                }
            }

            return null;
        }

        private static RectTransform FindRectTransformByName(Transform root, string targetName)
        {
            if (root == null || string.IsNullOrWhiteSpace(targetName))
            {
                return null;
            }

            var rects = root.GetComponentsInChildren<RectTransform>(true);
            for (var i = 0; i < rects.Length; i++)
            {
                if (rects[i] != null && rects[i].name == targetName)
                {
                    return rects[i];
                }
            }

            return null;
        }

        private async Task<GameObject> InstantiatePrefabAsync(string address, Transform parent)
        {
            if (string.IsNullOrWhiteSpace(address))
            {
                Debug.LogError("[VNGameBootstrap] Address key is empty.");
                return null;
            }

            try
            {
                var prefab = await Addressables.LoadAssetAsync<GameObject>(address).Task;
                if (prefab == null)
                {
                    Debug.LogError($"[VNGameBootstrap] Could not load prefab at Addressables key: {address}");
                    return null;
                }

                return Instantiate(prefab, parent);
            }
            catch (Exception ex)
            {
                Debug.LogError($"[VNGameBootstrap] Failed to load addressable prefab ({address}).\n{ex}");
                return null;
            }
        }
    }
}