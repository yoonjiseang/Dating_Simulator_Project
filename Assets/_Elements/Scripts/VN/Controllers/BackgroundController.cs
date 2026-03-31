using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VN.Controllers
{
    public class BackgroundController : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        public string CurrentBackgroundKey { get; private set; }

        private void Awake()
        {
            EnsureBackgroundImage();
        }

        public IEnumerator SetBackground(Sprite sprite, string bgKey, string transition, float duration)
        {
            EnsureBackgroundImage();

            if (backgroundImage == null)
            {
                Debug.LogError("[BackgroundController] backgroundImage is null.");
                yield break;
            }

            CurrentBackgroundKey = bgKey;
            backgroundImage.sprite = sprite;

            // 최소 실행형: transition 값은 확장 포인트로만 남겨둠
            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }
        }

        private void EnsureBackgroundImage()
        {
            if (backgroundImage != null) return;

            var canvas = FindFirstObjectByType<Canvas>();
            if (canvas == null)
            {
                var canvasGo = new GameObject("VNBackgroundCanvas", typeof(Canvas), typeof(CanvasScaler), typeof(GraphicRaycaster));
                canvas = canvasGo.GetComponent<Canvas>();
                canvas.renderMode = RenderMode.ScreenSpaceOverlay;
                canvas.sortingOrder = -100;
            }

            var imageGo = new GameObject("BackgroundImage", typeof(RectTransform), typeof(Image));
            imageGo.transform.SetParent(canvas.transform, false);

            var rect = imageGo.GetComponent<RectTransform>();
            rect.anchorMin = Vector2.zero;
            rect.anchorMax = Vector2.one;
            rect.offsetMin = Vector2.zero;
            rect.offsetMax = Vector2.zero;

            backgroundImage = imageGo.GetComponent<Image>();
            backgroundImage.preserveAspect = true;

            imageGo.transform.SetAsFirstSibling();
        }
    }
}