using System;
using UnityEngine;

namespace VN.Systems
{
    public class VNInputRouter : MonoBehaviour
    {
        public event Action OnNextPressed;

        private void Update()
        {
            if (Input.GetMouseButtonDown(0) || Input.GetKeyDown(KeyCode.Space))
            {
                OnNextPressed?.Invoke();
            }
        }
    }
}