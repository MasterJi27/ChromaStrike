using System.Collections;
using UnityEngine;
using UnityEngine.UI;

namespace ChromaStrike.UI
{
    /// <summary>Centre dot plus a hit marker that flashes when a shot lands on a sphere.</summary>
    public sealed class CrosshairView : MonoBehaviour
    {
        [SerializeField] private CanvasGroup hitMarker;
        [SerializeField] private Image[] dotParts;
        [SerializeField, Min(0.05f)] private float flashSeconds = 0.18f;

        private Coroutine running;

        public void FlashHit()
        {
            if (running != null) StopCoroutine(running);
            running = StartCoroutine(Flash());
        }

        private IEnumerator Flash()
        {
            hitMarker.alpha = 1f;
            hitMarker.transform.localScale = Vector3.one * 1.35f;
            for (float t = 0f; t < flashSeconds; t += Time.unscaledDeltaTime)
            {
                float k = t / flashSeconds;
                hitMarker.alpha = 1f - k;
                hitMarker.transform.localScale = Vector3.one * Mathf.Lerp(1.35f, 1f, k);
                yield return null;
            }
            hitMarker.alpha = 0f;
        }
    }
}
