using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.Events;
using UnityEngine.UI;
using VN.Core;

namespace Menu.Controllers
{
    /// <summary>
    /// Binds multiple Save/Load slot buttons to VNGameController methods.
    /// Useful for menu UIs with many save/load slots (e.g. 10 slots).
    /// </summary>
    public class VNSaveLoadButtonBinder : MonoBehaviour
    {
        public enum SaveLoadAction
        {
            Save,
            Load
        }

        [Serializable]
        public class SlotButtonBinding
        {
            public Button button;
            public string slotName = "slot_1";
            public SaveLoadAction action = SaveLoadAction.Save;
        }

        [Header("Slot Button Bindings")]
        [SerializeField] private List<SlotButtonBinding> bindings = new List<SlotButtonBinding>();

        private class ListenerRegistration
        {
            public Button button;
            public UnityAction action;
        }

        private readonly List<ListenerRegistration> _registeredListeners = new List<ListenerRegistration>();

        private void OnEnable()
        {
            RegisterBindings();
        }

        private void OnDisable()
        {
            UnregisterBindings();
        }

        private void RegisterBindings()
        {
            for (var i = 0; i < bindings.Count; i++)
            {
                var binding = bindings[i];
                if (binding == null || binding.button == null || string.IsNullOrWhiteSpace(binding.slotName))
                {
                    continue;
                }

                var slotCopy = binding.slotName;
                var actionCopy = binding.action;

                UnityAction listener = () => ExecuteBinding(slotCopy, actionCopy);
                binding.button.onClick.AddListener(listener);
                _registeredListeners.Add(new ListenerRegistration
                {
                    button = binding.button,
                    action = listener
                });
            }
        }

        private void UnregisterBindings()
        {
            for (var i = 0; i < _registeredListeners.Count; i++)
            {
                var registration = _registeredListeners[i];
                if (registration != null && registration.button != null)
                {
                    registration.button.onClick.RemoveListener(registration.action);
                }
            }

            _registeredListeners.Clear();
        }

        private void ExecuteBinding(string slotName, SaveLoadAction action)
        {
            var controller = VNGameController.Instance;
            if (controller == null)
            {
                Debug.LogError("[VNSaveLoadButtonBinder] VNGameController.Instance is null. Request ignored.");
                return;
            }

            switch (action)
            {
                case SaveLoadAction.Save:
                    controller.Save(slotName);
                    break;
                case SaveLoadAction.Load:
                    controller.Load(slotName);
                    break;
                default:
                    Debug.LogWarning($"[VNSaveLoadButtonBinder] Unsupported action: {action}");
                    break;
            }
        }

        public void ExecuteSave(string slotName)
        {
            if (!string.IsNullOrWhiteSpace(slotName))
            {
                ExecuteBinding(slotName, SaveLoadAction.Save);
            }
        }

        public void ExecuteLoad(string slotName)
        {
            if (!string.IsNullOrWhiteSpace(slotName))
            {
                ExecuteBinding(slotName, SaveLoadAction.Load);
            }
        }
    }
}