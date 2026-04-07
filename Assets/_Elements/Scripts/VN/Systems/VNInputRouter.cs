using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.InputSystem;

namespace VN.Systems
{
    public class VNInputRouter : MonoBehaviour
    {
        public static VNInputRouter Instance { get; private set; }

        [SerializeField] private bool dontDestroyOnLoad = true;
        public event Action OnNextPressed;

        private int _inputBlockCount;

        private void Awake()
        {
            var rootObject = transform.root.gameObject;

            if (Instance != null && Instance != this)
            {
                Debug.LogWarning("[VNInputRouter] Duplicate instance detected. Destroying the new one.");
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
        }

        public void AcquireInputBlock()
        {
            _inputBlockCount++;
        }

        public void ReleaseInputBlock()
        {
            _inputBlockCount = Mathf.Max(0, _inputBlockCount - 1);
        }

        public bool IsInputBlocked()
        {
            return _inputBlockCount > 0;
        }

        private void Update()
        {
            if (IsInputBlocked())
            {
                return;
            }

            var mousePressed = Mouse.current?.leftButton.wasPressedThisFrame ?? false;
            var spacePressed = Keyboard.current?.spaceKey.wasPressedThisFrame ?? false;

            var isPointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            if (mousePressed && isPointerOverUi)
            {
                // Ignore clicks consumed by UI (e.g. menu open button) so VN text does not advance.
                mousePressed = false;
            }

            if (mousePressed || spacePressed)
            {
                OnNextPressed?.Invoke();
            }
        }
    }
}