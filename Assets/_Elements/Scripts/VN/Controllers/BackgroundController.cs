using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace VN.Controllers
{
    public class BackgroundController : MonoBehaviour
    {
        [SerializeField] private Image backgroundImage;
        public string CurrentBackgroundKey { get; private set; }

        public IEnumerator SetBackground(Sprite sprite, string bgKey, string transition, float duration)
        {
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
    }
}