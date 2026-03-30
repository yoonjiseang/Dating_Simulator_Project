using System.Collections;
using UnityEngine;
using UnityEngine.UI;
using VN.Systems;

namespace VN.Controllers
{
    public class DialogueUIController : MonoBehaviour
    {
        [SerializeField] private Text speakerText;
        [SerializeField] private Text dialogueText;
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
            if (speakerText != null) speakerText.text = speaker;
            if (dialogueText == null) yield break;

            dialogueText.text = string.Empty;
            _nextPressed = false;
            var typingDone = false;
            var index = 0;

            while (!typingDone)
            {
                if (_nextPressed)
                {
                    dialogueText.text = text;
                    typingDone = true;
                    _nextPressed = false;
                    break;
                }

                index++;
                dialogueText.text = text.Substring(0, Mathf.Clamp(index, 0, text.Length));
                typingDone = index >= text.Length;
                yield return new WaitForSeconds(typeInterval);
            }

            while (!_nextPressed)
            {
                yield return null;
            }

            _nextPressed = false;
        }

        private void HandleNextPressed()
        {
            _nextPressed = true;
        }
    }
}