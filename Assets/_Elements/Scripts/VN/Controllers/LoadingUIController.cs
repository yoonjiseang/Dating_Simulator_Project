using UnityEngine;
using UnityEngine.UI;

namespace VN.Controllers
{
    public class LoadingUIController : MonoBehaviour
    {
        [SerializeField] private GameObject loadingRoot;
        [SerializeField] private Slider loadingProgressBar;

        public void SetVisible(bool visible)
        {
            if (loadingRoot != null)
            {
                loadingRoot.SetActive(visible);
            }
            else
            {
                gameObject.SetActive(visible);
            }
        }

        public void SetProgress(float progress)
        {
            if (loadingProgressBar != null)
            {
                loadingProgressBar.value = Mathf.Clamp01(progress);
            }
        }
    }
}