using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using GameFlow;
using UnityEngine;
using UnityEngine.AddressableAssets;
using UnityEngine.ResourceManagement.AsyncOperations;
using VN.Controllers;
using VN.Systems;

namespace VN.Core
{
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

        [Header("Main Menu from MasterData")]
        [SerializeField] private bool useMainMenu = true;
        [SerializeField] private string storyMasterTableName = "story_detail";
        [SerializeField] private string menuTitle = "Dating Simulator";
        [SerializeField] private bool showLockedStories = true;

        [Header("Main Menu View")]
        [Tooltip("Scene instance or prefab asset 모두 허용. Prefab asset이면 부트 시 런타임 인스턴스로 생성됩니다.")]
        [SerializeField] private StoryRouteMenuController menuControllerInScene;
        [SerializeField] private string menuControllerPrefabAddress = "VN/StoryRouteMenu";
        [SerializeField] private Transform menuControllerParent;

        private async void Awake()
        {
            if (useMainMenu)
            {
                var selectedRoute = await ShowMainMenuAsync();
                if (selectedRoute == null)
                {
                    return;
                }

                StorySelectionState.SetSelectedRoute(selectedRoute.routeId, selectedRoute.storyId);
                if (!string.IsNullOrWhiteSpace(selectedRoute.storyId))
                {
                    overrideStoryId = selectedRoute.storyId;
                }
            }

            await BootstrapAsync();
        }

        private async Task<StoryRouteEntry> ShowMainMenuAsync()
        {
            var routes = await LoadRoutesFromMasterDataAsync();
            if (routes.Count == 0)
            {
                routes = StoryRouteCatalogLoader.CreateFallbackCatalog();
            }

            var menuController = await ResolveMenuControllerAsync();
            if (menuController == null)
            {
                Debug.LogError("[VNGameBootstrap] StoryRouteMenuController is not available. Main menu cannot open.");
                return null;
            }

            var progressVariables = CreateProgressVariableStore();
            var selected = await menuController.ShowAsync(routes, progressVariables, DateTime.UtcNow, menuTitle, showLockedStories);
            menuController.Hide();
            return selected;
        }

        private async Task<StoryRouteMenuController> ResolveMenuControllerAsync()
        {
            var parent = ResolveMenuControllerParent();

            if (menuControllerInScene == null)
            {
                menuControllerInScene = FindFirstObjectByType<StoryRouteMenuController>(FindObjectsInactive.Include);
            }
            else if (!menuControllerInScene.gameObject.scene.IsValid())
            {
                menuControllerInScene = Instantiate(menuControllerInScene, parent);
                menuControllerInScene.transform.SetParent(parent, false);
                menuControllerInScene.gameObject.name = "StoryRouteMenuController_Runtime";
            }

            if (menuControllerInScene != null)
            {
                return menuControllerInScene;
            }

            if (string.IsNullOrWhiteSpace(menuControllerPrefabAddress))
            {
                return null;
            }

            
            var menuInstance = await InstantiatePrefabAsync(menuControllerPrefabAddress, parent);
            if (menuInstance == null)
            {
                return null;
            }

            menuControllerInScene = menuInstance.GetComponent<StoryRouteMenuController>()
                                 ?? menuInstance.GetComponentInChildren<StoryRouteMenuController>(true);
            return menuControllerInScene;
        }

        private Transform ResolveMenuControllerParent()
        {
            if (menuControllerParent != null)
            {
                if (menuControllerParent.gameObject.scene.IsValid())
                {
                    return menuControllerParent;
                }

                Debug.LogWarning("[VNGameBootstrap] menuControllerParent is not a scene object. Falling back to scene Canvas.");
            }

            if (viewStoryTopParent != null && viewStoryTopParent.gameObject.scene.IsValid())
            {
                return viewStoryTopParent;
            }

            var canvas = FindFirstObjectByType<Canvas>(FindObjectsInactive.Include);
            if (canvas != null)
            {
                return canvas.transform;
            }

            return transform;
        }

        private async Task<List<StoryRouteEntry>> LoadRoutesFromMasterDataAsync()
        {
            var provider = new MasterDataProvider();
            var table = await provider.LoadTableAsync(storyMasterTableName);
            var routes = StoryRouteCatalogLoader.LoadFromMasterData(table);

            if (routes.Count == 0)
            {
                Debug.LogWarning($"[VNGameBootstrap] Could not load routes from MDB/{storyMasterTableName}. Using fallback route.");
            }

            return routes;
        }

        private static VariableStore CreateProgressVariableStore()
        {
            var variables = new VariableStore();
            variables.Apply("totalClearCount", "set", GameProgressStore.GetTotalClearCount());
            return variables;
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

                controller.ConfigureDependencies(characterStage, background, dialogue, choice, audio, input, loadingUi);

                if (input != null)
                {
                    var mouseAdvanceArea = FindMouseAdvanceArea(viewStoryTopInstance);
                    if (mouseAdvanceArea != null)
                    {
                        input.SetMouseAdvanceArea(mouseAdvanceArea);
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

            AsyncOperationHandle<GameObject> handle = default;
            try
            {
                handle = Addressables.LoadAssetAsync<GameObject>(address);
                var prefab = await handle.Task;
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
            finally
            {
                if (handle.IsValid())
                {
                    Addressables.Release(handle);
                }
            }
        }
    }
}