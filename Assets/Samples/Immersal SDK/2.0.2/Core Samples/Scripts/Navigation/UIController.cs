using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using Immersal.XR;

namespace Immersal.Samples.Navigation
{
    public class UIController : MonoBehaviour
    {
        private CanvasGroup UI;

        void Start()
        {
            UI = GetComponent<CanvasGroup>();

            if (UI == null)
            {
                Debug.LogError("CanvasGroup component not found on this GameObject.");
                return;
            }
            // UI.alpha = 1;
        }

        public void SetAlpha(float alpha)
        {
            if (UI != null)
            {
                UI.alpha = alpha;
            }
        }

        public void FadeOut(float duration)
        {
            StartCoroutine(FadeCanvasGroup(UI, UI.alpha, 0, duration));
        }

        public void FadeIn(float duration)
        {
            StartCoroutine(FadeCanvasGroup(UI, UI.alpha, 1, duration));
        }

        private IEnumerator FadeCanvasGroup(CanvasGroup cg, float start, float end, float duration)
        {
            float elapsedTime = 0;

            while (elapsedTime < duration)
            {
                cg.alpha = Mathf.Lerp(start, end, elapsedTime / duration);
                elapsedTime += Time.deltaTime;
                yield return null;
            }

            cg.alpha = end; 
        }

    }
}


