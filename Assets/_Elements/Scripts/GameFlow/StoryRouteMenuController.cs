using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VN.Systems;

namespace GameFlow
{
    public class StoryRouteMenuController : MonoBehaviour
    {
        [Header("UI")]
        [SerializeField] private Text titleText;
        [SerializeField] private Transform buttonContainer;
        [SerializeField] private Button routeButtonPrefab;
        [SerializeField] private Button closeButton;

        [Header("Text")]
        [SerializeField] private string lockedSuffix = " (조건 미충족)";

        private readonly List<GameObject> _spawnedButtons = new();

        public Task<StoryRouteEntry> ShowAsync(
            IReadOnlyList<StoryRouteEntry> routes,
            VariableStore progressVariables,
            DateTime nowUtc,
            string menuTitle,
            bool showLockedItems)
        {
            var tcs = new TaskCompletionSource<StoryRouteEntry>();

            if (titleText != null)
            {
                titleText.text = menuTitle;
            }

            if (closeButton != null)
            {
                closeButton.onClick.RemoveAllListeners();
                closeButton.onClick.AddListener(() => tcs.TrySetResult(null));
            }

            ClearButtons();

            if (routes != null)
            {
                for (var i = 0; i < routes.Count; i++)
                {
                    var route = routes[i];
                    if (route == null)
                    {
                        continue;
                    }

                    var isPlayable = StoryRouteAvailability.IsPlayable(route, progressVariables, nowUtc);
                    if (!isPlayable && !showLockedItems)
                    {
                        continue;
                    }

                    SpawnRouteButton(route, isPlayable, tcs);
                }
            }

            gameObject.SetActive(true);
            return tcs.Task;
        }

        public void Hide()
        {
            gameObject.SetActive(false);
        }

        private void SpawnRouteButton(StoryRouteEntry route, bool isPlayable, TaskCompletionSource<StoryRouteEntry> tcs)
        {
            if (routeButtonPrefab == null)
            {
                return;
            }

            var parent = ResolveButtonParent();
            if (parent == null)
            {
                return;
            }

            var button = Instantiate(routeButtonPrefab, parent);
            _spawnedButtons.Add(button.gameObject);

            var label = BuildRouteLabel(route, isPlayable);
            ApplyButtonLabel(button, label);
            button.interactable = isPlayable;

            if (isPlayable)
            {
                button.onClick.AddListener(() =>
                {
                    tcs.TrySetResult(route);
                });
            }
        }

        private string BuildRouteLabel(StoryRouteEntry route, bool isPlayable)
        {
            var subTitlePart = string.IsNullOrWhiteSpace(route.subTitle) ? string.Empty : $"\n<size=70%>{route.subTitle}</size>";
            var title = $"{route.displayName}{subTitlePart}";
            return isPlayable ? title : title + lockedSuffix;
        }

        private static void ApplyButtonLabel(Button button, string label)
        {
            var tmp = button.GetComponentInChildren<TMP_Text>(true);
            if (tmp != null)
            {
                tmp.text = label;
                return;
            }

            var text = button.GetComponentInChildren<Text>(true);
            if (text != null)
            {
                text.text = label;
            }
        }


        private Transform ResolveButtonParent()
        {
            var parent = buttonContainer != null ? buttonContainer : transform;
            if (!parent.gameObject.scene.IsValid())
            {
                parent = transform;
            }

            if (!parent.gameObject.scene.IsValid())
            {
                Debug.LogError("[StoryRouteMenuController] Button container is not a scene object. Assign scene instance in _Boot.");
                return null;
            }

            return parent;
        }

        private void ClearButtons()
        {
            for (var i = 0; i < _spawnedButtons.Count; i++)
            {
                var go = _spawnedButtons[i];
                if (go != null)
                {
                    Destroy(go);
                }
            }

            _spawnedButtons.Clear();
        }
    }
}