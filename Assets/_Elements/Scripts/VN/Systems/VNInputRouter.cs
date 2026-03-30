using System;
using UnityEngine;
using UnityEngine.InputSystem;

namespace VN.Systems
{
    public class VNInputRouter : MonoBehaviour
    {
        public event Action OnNextPressed;

        private void Update()
        {
            var mousePressed = Mouse.current?.leftButton.wasPressedThisFrame ?? false;
            var spacePressed = Keyboard.current?.spaceKey.wasPressedThisFrame ?? false;

            if (mousePressed || spacePressed)
            {
                OnNextPressed?.Invoke();
            }
        }
    }
}