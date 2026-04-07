using UnityEngine;
using UnityEngine.UI;

namespace Menu.Controllers
{
    /// <summary>
    /// Attach this to ViewStoryTop prefab.
    /// It opens/toggles ViewMenuTop prefab when menu button is clicked.
    /// </summary>
    public class VNMenuLauncher : MonoBehaviour
    {
        [Header("Trigger")]
        [SerializeField] private Button menuButton;

        [Header("Menu Prefab")]
        [SerializeField] private string menuPrefabResourcePath = "VN/ViewMenuTop";

        [Header("Menu Parent")]
        [Tooltip("Optional explicit parent. If null, launcher tries to use this game's Canvas automatically.")]
        [SerializeField] private Transform menuParent;
        [SerializeField] private bool openOnStart = false;

        private GameObject _menuInstance;

        private void Awake()
        {
            ResolveMenuParent();
        }

        private void OnEnable()
        {
            if (menuButton != null)
            {
                menuButton.onClick.AddListener(ToggleMenu);
            }

            if (openOnStart)
            {
                OpenMenu();
            }
        }

        private void OnDisable()
        {
            if (menuButton != null)
            {
                menuButton.onClick.RemoveListener(ToggleMenu);
            }
        }

        private void ResolveMenuParent()
        {
            if (menuParent != null)
            {
                return;
            }

            var canvas = GetComponentInParent<Canvas>(true);
            if (canvas == null)
            {
                canvas = transform.root.GetComponentInChildren<Canvas>(true);
            }

            if (canvas == null)
            {
                canvas = FindFirstObjectByType<Canvas>();
            }

            menuParent = canvas != null ? canvas.transform : transform;
        }

        public void ToggleMenu()
        {
            if (_menuInstance == null)
            {
                OpenMenu();
                return;
            }

            _menuInstance.SetActive(!_menuInstance.activeSelf);
        }

        public void OpenMenu()
        {
            if (_menuInstance == null)
            {
                var prefab = Resources.Load<GameObject>(menuPrefabResourcePath);
                if (prefab == null)
                {
                    Debug.LogError($"[VNMenuLauncher] Menu prefab not found at Resources path: {menuPrefabResourcePath}");
                    return;
                }

                ResolveMenuParent();
                _menuInstance = Instantiate(prefab, menuParent);
                _menuInstance.name = prefab.name;
            }

            _menuInstance.SetActive(true);
        }

        public void CloseMenu()
        {
            if (_menuInstance != null)
            {
                _menuInstance.SetActive(false);
            }
        }
    }
}