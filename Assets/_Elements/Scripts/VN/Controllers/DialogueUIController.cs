using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using TMPro;
using VN.Systems;

namespace VN.Controllers
{
    public class DialogueUIController : MonoBehaviour
    {
        [Header("UGUI Text (legacy)")]
        [SerializeField] private Text speakerText;
        [SerializeField] private Text dialogueText;

        [Header("TMP Text (optional)")]
        [SerializeField] private TMP_Text speakerTmpText;
        [SerializeField] private TMP_Text dialogueTmpText;

        [SerializeField] private float typeInterval = 0.03f;

        private VNInputRouter _inputRouter;
        private bool _nextPressed;

        public void Initialize(VNInputRouter inputRouter)
        {
            _inputRouter = inputRouter;
            if (_inputRouter != null)
            {
                _inputRouter.OnNextPressed += HandleNextPressed;
            }
        }

        private void OnDestroy()
        {
            if (_inputRouter != null)
            {
                _inputRouter.OnNextPressed -= HandleNextPressed;
            }
        }

        public IEnumerator ShowDialogue(string speaker, string text)
        {
            text ??= string.Empty;
            SetSpeakerText(speaker);

            if (!HasDialogueTarget())
            {
                Debug.LogError("[DialogueUIController] dialogue text target is not assigned. Assign UI Text or TMP_Text in Inspector.");
                yield break;
            }

            SetDialogueText(string.Empty);
            _nextPressed = false;
            var typingDone = false;
            var index = 0;

            while (!typingDone)
            {
                if (_nextPressed)
                {
                    SetDialogueText(text);
                    typingDone = true;
                    _nextPressed = false;
                    break;
                }

                index++;
                SetDialogueText(text.Substring(0, Mathf.Clamp(index, 0, text.Length)));
                typingDone = index >= text.Length;
                yield return new WaitForSeconds(typeInterval);
            }

            while (!_nextPressed)
            {
                yield return null;
            }

            _nextPressed = false;
        }

        private bool HasDialogueTarget()
        {
            return dialogueText != null || dialogueTmpText != null;
        }

        private void SetSpeakerText(string value)
        {
            if (speakerText != null)
            {
                speakerText.text = value;
            }

            if (speakerTmpText != null)
            {
                speakerTmpText.text = value;
            }
        }

        private void SetDialogueText(string value)
        {
            if (dialogueText != null)
            {
                dialogueText.text = value;
            }

            if (dialogueTmpText != null)
            {
                dialogueTmpText.text = value;
            }
        }

        private void HandleNextPressed()
        {
            _nextPressed = true;
        }
    }
}