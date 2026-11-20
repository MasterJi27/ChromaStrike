using System.Collections;
using TMPro;
using UnityEngine;

namespace ChromaStrike.UI
{
    /// <summary>Short message in the lower middle of the screen, e.g. when a replacement batch appears.</summary>
    [RequireComponent(typeof(CanvasGroup))]
    public sealed class ToastView : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField, Min(0.5f)] private float visibleSeconds = 2.2f;

        private CanvasGroup group;
        private Coroutine running;

        private void Awake()
        {
            group = GetComponent<CanvasGroup>();
            group.alpha = 0f;
        }

        public void Show(string message)
        {
            text.text = message;
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Run());
        }

        private IEnumerator Run()
        {
            yield return UiTween.Fade(group, 1f, 0.15f);
            yield return new WaitForSecondsRealtime(visibleSeconds);
            yield return UiTween.Fade(group, 0f, 0.4f);
        }
    }
}
