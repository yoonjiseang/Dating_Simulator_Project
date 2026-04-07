using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;
using VN.Systems;

namespace Menu.Controllers
{
    /// <summary>
    /// Attach this to ViewMenuTop prefab.
    /// Left side tab buttons control right side tab content panels.
    /// </summary>
    public class VNMenuTopController : MonoBehaviour
    {
        [Serializable]
        public class MenuTab
        {
            [Tooltip("Unique key for this tab (e.g. save, load, settings)")]
            public string key;
            public Button tabButton;
            public GameObject contentRoot;
        }

        [Header("Tabs")]
        [SerializeField] private List<MenuTab> tabs = new List<MenuTab>();
        [SerializeField] private string defaultTabKey = "save";

        [Header("Optional")]
        [SerializeField] private Button closeButton;

        private readonly List<Action> _removeListeners = new List<Action>();
        private string _currentTabKey;

        private void OnEnable()
        {
            VNInputRouter.Instance?.AcquireInputBlock();
            BindTabButtons();

            if (closeButton != null)
            {
                closeButton.onClick.AddListener(CloseMenu);
                _removeListeners.Add(() => closeButton.onClick.RemoveListener(CloseMenu));
            }

            if (!string.IsNullOrWhiteSpace(defaultTabKey))
            {
                ShowTab(defaultTabKey);
            }
            else if (tabs.Count > 0 && !string.IsNullOrWhiteSpace(tabs[0].key))
            {
                ShowTab(tabs[0].key);
            }
        }

        private void OnDisable()
        {
            VNInputRouter.Instance?.ReleaseInputBlock();

            for (var i = 0; i < _removeListeners.Count; i++)
            {
                _removeListeners[i]?.Invoke();
            }

            _removeListeners.Clear();
        }

        private void BindTabButtons()
        {
            for (var i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                if (tab == null || tab.tabButton == null || string.IsNullOrWhiteSpace(tab.key))
                {
                    continue;
                }

                var keyCopy = tab.key;
                UnityEngine.Events.UnityAction action = () => ShowTab(keyCopy);
                tab.tabButton.onClick.AddListener(action);
                _removeListeners.Add(() => tab.tabButton.onClick.RemoveListener(action));
            }
        }

        public void ShowStorySaveTab()
        {
            ShowTab("save");
        }

        public void ShowStoryLoadTab()
        {
            ShowTab("load");
        }

        public void ShowSettingsTab()
        {
            ShowTab("settings");
        }

        public void ShowTab(string tabKey)
        {
            if (string.IsNullOrWhiteSpace(tabKey))
            {
                return;
            }

            _currentTabKey = tabKey;

            for (var i = 0; i < tabs.Count; i++)
            {
                var tab = tabs[i];
                if (tab == null || tab.contentRoot == null)
                {
                    continue;
                }

                var isActive = string.Equals(tab.key, tabKey, StringComparison.OrdinalIgnoreCase);
                tab.contentRoot.SetActive(isActive);
            }
        }

        public void CloseMenu()
        {
            gameObject.SetActive(false);
        }

        public string GetCurrentTabKey()
        {
            return _currentTabKey;
        }
    }
}