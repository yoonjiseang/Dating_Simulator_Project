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

        private readonly Dictionary<string, CharacterView> _views = new();

        public IEnumerable<CharacterView> ActiveViews => _views.Values;

        public IEnumerator ShowCharacter(string characterId, string slot, Sprite body, Sprite face, float duration)
        {
            if (string.IsNullOrWhiteSpace(characterId))
            {
                Debug.LogError("[CharacterStageController] characterId is empty.");
                yield break;
            }

            if (!_views.TryGetValue(characterId, out var view))
            {
                view = CreateView(characterId);
                _views[characterId] = view;
            }

            view.slot = slot;
            view.bodyKey = body != null ? body.name : string.Empty;
            view.faceKey = face != null ? face.name : string.Empty;

            view.bodyImage.sprite = body;
            view.bodyImage.enabled = body != null;

            if (view.faceImage != null)
            {
                view.faceImage.sprite = null;
                view.faceImage.enabled = false;
            }
            SetSlotPosition(view, slot);

            if (duration > 0f)
            {
                yield return new WaitForSeconds(duration);
            }
        }

        public void HideCharacter(string characterId)
        {
            if (_views.TryGetValue(characterId, out var view))
            {
                if (view.bodyImage != null && view.bodyImage.transform.parent != null)
                {
                    Destroy(view.bodyImage.transform.parent.gameObject);
                }
                _views.Remove(characterId);
            }
        }

        public void ChangeFace(string characterId, Sprite face)
        {
            if (_views.TryGetValue(characterId, out var view))
            {
                if (view.faceImage != null)
                {
                    view.faceImage.sprite = face;
                    view.faceImage.enabled = face != null;
                }
                else
                {
                    view.bodyImage.sprite = face;
                    view.bodyImage.enabled = face != null;
                }

                view.faceKey = face != null ? face.name : string.Empty;
            }
        }

        public void ChangeCharacterSprite(string characterId, Sprite sprite)
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
    }
}