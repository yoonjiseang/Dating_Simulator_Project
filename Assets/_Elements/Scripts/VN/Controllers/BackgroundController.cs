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

            var normalized = NormalizeTransitionKey(transition);
            switch (normalized)
            {
                case "fade":
                case "fadein":
                    yield return FadeGraphic(backgroundImage, 0f, 1f, duration);
                    break;
                case "slideleft":
                    yield return SlideHorizontal(backgroundImage.rectTransform, duration, backgroundImage.rectTransform.rect.width);
                    break;
                case "slideright":
                    yield return SlideHorizontal(backgroundImage.rectTransform, duration, -backgroundImage.rectTransform.rect.width);
                    break;
                default:
                    SetImageAlpha(1f);
                    if (duration > 0f)
                    {
                        yield return new WaitForSeconds(duration);
                    }
                    break;
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

        private void SetImageAlpha(float alpha)
        {
            if (backgroundImage == null)
            {
                return;
            }

            var color = backgroundImage.color;
            color.a = alpha;
            backgroundImage.color = color;
        }

        private static IEnumerator FadeGraphic(Graphic graphic, float from, float to, float duration)
        {
            if (graphic == null)
            {
                yield break;
            }

            var color = graphic.color;
            color.a = from;
            graphic.color = color;

            if (duration <= 0f)
            {
                color.a = to;
                graphic.color = color;
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                color.a = Mathf.Lerp(from, to, Mathf.Clamp01(elapsed / duration));
                graphic.color = color;
                yield return null;
            }

            color.a = to;
            graphic.color = color;
        }

        private static IEnumerator SlideHorizontal(RectTransform target, float duration, float distance)
        {
            if (target == null)
            {
                yield break;
            }

            if (Mathf.Abs(distance) < 1f)
            {
                distance = Screen.width;
            }

            var basePosition = target.anchoredPosition;
            if (duration <= 0f || Mathf.Approximately(distance, 0f))
            {
                target.anchoredPosition = basePosition;
                yield break;
            }

            var fromPosition = basePosition + Vector2.right * distance;
            var elapsed = 0f;
            target.anchoredPosition = fromPosition;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                target.anchoredPosition = Vector2.Lerp(fromPosition, basePosition, t);
                yield return null;
            }

            target.anchoredPosition = basePosition;
        }

        private static string NormalizeTransitionKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var normalized = key.Trim().ToLowerInvariant().Replace("-", string.Empty).Replace("_", string.Empty).Replace(" ", string.Empty);
            return normalized switch
            {
                "fadein" => "fadein",
                "fade" => "fade",
                "slideleft" => "slideleft",
                "slideright" => "slideright",
                _ => normalized
            };
        }
    }
}
