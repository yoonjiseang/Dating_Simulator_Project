using System;
using System.Collections;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using VN.Data;

namespace VN.Controllers
{
    public class ChoiceUIController : MonoBehaviour
    {
        [SerializeField] private RectTransform root;
        [SerializeField] private Button optionButtonPrefab;

        public void Initialize()
        {
            if (root != null)
            {
                root.gameObject.SetActive(false);
            }
        }

        public IEnumerator ShowChoices(IReadOnlyList<ChoiceOptionData> options, Action<ChoiceOptionData> onSelected)
        {
            if (root == null || optionButtonPrefab == null || options == null || options.Count == 0)
            {
                yield break;
            }

            root.gameObject.SetActive(true);
            foreach (Transform child in root)
            {
                Destroy(child.gameObject);
            }

            var selected = false;
            for (var i = 0; i < options.Count; i++)
            {
                var option = options[i];
                if (option == null)
                {
                    continue;
                }

                var button = Instantiate(optionButtonPrefab, root);
                var tmpText = button.GetComponentInChildren<TMP_Text>();
                if (tmpText != null)
                {
                    tmpText.text = option.text;
                }
                else
                {
                    var text = button.GetComponentInChildren<Text>();
                    if (text != null)
                    {
                        text.text = option.text;
                    }
                }

                button.onClick.AddListener(() =>
                {
                    if (selected)
                    {
                        return;
                    }

                    selected = true;
                    onSelected?.Invoke(option);
                });
            }

            while (!selected)
            {
                yield return null;
            }

            root.gameObject.SetActive(false);
        }
    }
}