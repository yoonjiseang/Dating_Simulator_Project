using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace VN.Controllers
{
    public class CharacterStageController : MonoBehaviour
    {
        [System.Serializable]
        public class CharacterView
        {
            public string characterId;
            public string slot;
            public string bodyKey;
            public string faceKey;
            public Image bodyImage;
            public Image faceImage;
        }

        [Header("Slot Anchors")]
        [SerializeField] private RectTransform leftSlot;
        [SerializeField] private RectTransform centerSlot;
        [SerializeField] private RectTransform rightSlot;
        [Header("Default Effects")]
        [SerializeField] private float defaultFadeDuration = 0.2f;
        [SerializeField] private float defaultPunchDuration = 0.3f;
        [SerializeField] private float defaultPunchAmplitude = 60f;
        [SerializeField] private int defaultPunchCount = 2;
        [SerializeField] private float defaultPopScale = 1.08f;
        [SerializeField] private float defaultZoomStartScale = 0.92f;
        [SerializeField] private float defaultSlideDistance = 360f;

        private readonly Dictionary<string, CharacterView> _views = new();

        public IEnumerable<CharacterView> ActiveViews => _views.Values;

        public IEnumerator ShowCharacter(string characterId, string slot, Sprite body, Sprite face, float duration, string transition)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                Debug.LogError("[CharacterStageController] characterId is empty.");
                yield break;
            }

            var isFirstAppearance = false;
            if (!_views.TryGetValue(characterId, out var view))
            {
                view = CreateView(characterId);
                _views[characterId] = view;
                isFirstAppearance = true;
            }

            view.slot = slot;
            view.bodyKey = body != null ? body.name : string.Empty;
            view.faceKey = face != null ? face.name : string.Empty;

            view.bodyImage.sprite = body;
            view.bodyImage.enabled = body != null;

            if (view.faceImage != null)
            {
                view.faceImage.sprite = face;
                view.faceImage.enabled = face != null;
            }
            SetSlotPosition(view, slot);

            var requestedDuration = duration > 0f ? duration : defaultFadeDuration;
            var effect = string.IsNullOrWhiteSpace(transition) && isFirstAppearance ? "fadeIn" : transition;
            if (!string.IsNullOrWhiteSpace(effect))
            {
                yield return PlayEffect(characterId, effect, requestedDuration);
                yield break;
            }
            
            if (requestedDuration > 0f)
            {
                yield return new WaitForSeconds(requestedDuration);
            }
        }

        public IEnumerator HideCharacter(string characterId, float duration, string transition)
        {
            if (_views.TryGetValue(characterId, out var view))
            {
                var requestedDuration = duration > 0f ? duration : defaultFadeDuration;
                var effect = string.IsNullOrWhiteSpace(transition) ? "fadeOut" : transition;
                if (!string.IsNullOrWhiteSpace(effect))
                {
                    yield return PlayEffect(characterId, effect, requestedDuration);
                }
                if (view.bodyImage != null && view.bodyImage.transform.parent != null)
                {
                    Destroy(view.bodyImage.transform.parent.gameObject);
                }

                _views.Remove(characterId);
            }
        }

        public IEnumerator ChangeCharacterSprite(string characterId, Sprite sprite, string transition, float duration)
        {
            if (_views.TryGetValue(characterId, out var view))
            {
                view.bodyImage.sprite = sprite;
                view.bodyImage.enabled = sprite != null;
                view.bodyKey = sprite != null ? sprite.name : string.Empty;
                if (view.faceImage != null)
                {
                    view.faceImage.sprite = null;
                    view.faceImage.enabled = false;
                }
                view.faceKey = string.Empty;

                if (!string.IsNullOrWhiteSpace(transition))
                {
                    var requestedDuration = duration > 0f ? duration : defaultPunchDuration;
                    yield return PlayEffect(characterId, transition, requestedDuration);
                }
            }
        }

        public IEnumerator MoveCharacter(string characterId, string toSlot, float duration)
        {
            if (!_views.TryGetValue(characterId, out var view))
            {
                yield break;
            }

            SetSlotPosition(view, toSlot);
            view.slot = toSlot;

            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }
        }

        public IEnumerator PlayEffect(string characterId, string effect, float duration)
        {
            if (!_views.TryGetValue(characterId, out var view))
            {
                yield break;
            }

            var root = view.bodyImage != null ? view.bodyImage.transform.parent as RectTransform : null;
            if (root == null)
            {
                yield break;
            }

            var normalized = NormalizeEffectKey(effect);
            switch (normalized)
            {
                case "fade":
                case "fadein":
                    yield return FadeGraphic(view.bodyImage, 0f, 1f, duration > 0f ? duration : defaultFadeDuration);
                    break;
                case "fadeout":
                    yield return FadeGraphic(view.bodyImage, 1f, 0f, duration > 0f ? duration : defaultFadeDuration);
                    break;
                case "shake":
                case "angry":
                case "punch":
                    yield return PunchVertical(root, duration > 0f ? duration : defaultPunchDuration, defaultPunchAmplitude, defaultPunchCount);
                    break;
                case "pop":
                    yield return PopScale(root, duration > 0f ? duration : defaultPunchDuration, defaultPopScale);
                    break;
                case "zoomin":
                    yield return ZoomIn(root, duration > 0f ? duration : defaultPunchDuration, defaultZoomStartScale);
                    break;
                case "slideleft":
                    yield return SlideHorizontal(root, duration > 0f ? duration : defaultFadeDuration, defaultSlideDistance);
                    break;
                case "slideright":
                    yield return SlideHorizontal(root, duration > 0f ? duration : defaultFadeDuration, -defaultSlideDistance);
                    break;
                default:
                    if (duration > 0f)
                    {
                        yield return new WaitForSeconds(duration);
                    }
                    break;
            }
        }

        private CharacterView CreateView(string characterId)
        {
            var root = new GameObject($"Char_{characterId}", typeof(RectTransform));
            root.transform.SetParent(transform, false);

            var bodyGo = new GameObject("Body", typeof(RectTransform), typeof(Image));
            bodyGo.transform.SetParent(root.transform, false);
            var bodyRect = bodyGo.GetComponent<RectTransform>();
            bodyRect.sizeDelta = new Vector2(810f, 1080f);
            var bodyImage = bodyGo.GetComponent<Image>();

            return new CharacterView
            {
                characterId = characterId,
                bodyImage = bodyImage
            };
        }

        private void SetSlotPosition(CharacterView view, string slot)
        {
            var target = slot switch
            {
                "left" => leftSlot,
                "center" => centerSlot,
                "right" => rightSlot,
                _ => centerSlot
            };

            if (target == null || view?.bodyImage == null)
            {
                return;
            }

            var rect = view.bodyImage.transform.parent as RectTransform;
            if (rect != null)
            {
                rect.anchoredPosition = target.anchoredPosition;
            }
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

        private static IEnumerator PunchVertical(RectTransform target, float duration, float amplitude, int punchCount)
        {
            if (target == null)
            {
                yield break;
            }

            var basePosition = target.anchoredPosition;
            if (duration <= 0f || amplitude <= 0f || punchCount <= 0)
            {
                yield break;
            }

            var elapsed = 0f;
            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.Clamp01(elapsed / duration);
                var damping = 1f - t;
                var wave = Mathf.Sin(t * Mathf.PI * punchCount);
                target.anchoredPosition = basePosition + Vector2.up * (wave * amplitude * damping);
                yield return null;
            }

            target.anchoredPosition = basePosition;
        }

        private static IEnumerator PopScale(RectTransform target, float duration, float overshootScale)
        {
            if (target == null)
            {
                yield break;
            }

            var baseScale = target.localScale;
            if (duration <= 0f || overshootScale <= 1f)
            {
                target.localScale = baseScale;
                yield break;
            }

            var peakScale = baseScale * overshootScale;
            var elapsed = 0f;
            var peakTime = duration * 0.45f;
            while (elapsed < peakTime)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / peakTime));
                target.localScale = Vector3.Lerp(baseScale, peakScale, t);
                yield return null;
            }

            elapsed = 0f;
            var settleTime = Mathf.Max(0.01f, duration - peakTime);
            while (elapsed < settleTime)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / settleTime));
                target.localScale = Vector3.Lerp(peakScale, baseScale, t);
                yield return null;
            }

            target.localScale = baseScale;
        }

        private static IEnumerator ZoomIn(RectTransform target, float duration, float startScale)
        {
            if (target == null)
            {
                yield break;
            }

            var baseScale = target.localScale;
            if (duration <= 0f || startScale <= 0f)
            {
                target.localScale = baseScale;
                yield break;
            }

            var fromScale = baseScale * startScale;
            var elapsed = 0f;
            target.localScale = fromScale;

            while (elapsed < duration)
            {
                elapsed += Time.deltaTime;
                var t = Mathf.SmoothStep(0f, 1f, Mathf.Clamp01(elapsed / duration));
                target.localScale = Vector3.Lerp(fromScale, baseScale, t);
                yield return null;
            }

            target.localScale = baseScale;
        }

        private static IEnumerator SlideHorizontal(RectTransform target, float duration, float distance)
        {
            if (target == null)
            {
                yield break;
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

        private static string NormalizeEffectKey(string key)
        {
            if (string.IsNullOrWhiteSpace(key))
            {
                return string.Empty;
            }

            var normalized = key.Trim().ToLowerInvariant().Replace("-", string.Empty).Replace("_", string.Empty).Replace(" ", string.Empty);
            return normalized switch
            {
                "fadein" => "fadein",
                "fadeout" => "fadeout",
                "fade" => "fade",
                "shaking" => "shake",
                "shake" => "shake",
                "angry" => "angry",
                "punch" => "punch",
                "pop" => "pop",
                "zoomin" => "zoomin",
                "slideleft" => "slideleft",
                "slideright" => "slideright",
                _ => normalized
            };
        }
    }
}
