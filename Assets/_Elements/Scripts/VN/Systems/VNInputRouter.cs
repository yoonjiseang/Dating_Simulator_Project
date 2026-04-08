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

        [Header("Mouse Advance Area")]
        [SerializeField] private bool restrictMouseAdvanceToArea = true;
        [SerializeField] private RectTransform mouseAdvanceArea;

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

        public void SetMouseAdvanceArea(RectTransform area)
        {
            mouseAdvanceArea = area;
        }

        private void Update()
        {
            if (IsInputBlocked())
            {
                return;
            }

            var mousePressed = Mouse.current?.leftButton.wasPressedThisFrame ?? false;
            var spacePressed = Keyboard.current?.spaceKey.wasPressedThisFrame ?? false;

            if (mousePressed && !IsMouseAdvanceAllowed())
            {
                mousePressed = false;
            }

            if (mousePressed || spacePressed)
            {
                OnNextPressed?.Invoke();
            }
        }

        private bool IsMouseAdvanceAllowed()
        {
            if (Mouse.current == null)
            {
                return false;
            }

            if (restrictMouseAdvanceToArea)
            {
                if (mouseAdvanceArea == null)
                {
                    return false;
                }

                var screenPoint = Mouse.current.position.ReadValue();
                var eventCamera = ResolveEventCamera(mouseAdvanceArea);
                // If advance area is a UI RectTransform, pointer is naturally over UI.
                // In that case, inside-area click should still be allowed.
                return RectTransformUtility.RectangleContainsScreenPoint(mouseAdvanceArea, screenPoint, eventCamera);
            }

            var isPointerOverUi = EventSystem.current != null && EventSystem.current.IsPointerOverGameObject();
            return !isPointerOverUi;
        }

        private static Camera ResolveEventCamera(RectTransform rectTransform)
        {
            var canvas = rectTransform.GetComponentInParent<Canvas>();
            if (canvas == null)
            {
                return null;
            }

            if (canvas.renderMode == RenderMode.ScreenSpaceOverlay)
            {
                return null;
            }

            return canvas.worldCamera;
        }
    }
}