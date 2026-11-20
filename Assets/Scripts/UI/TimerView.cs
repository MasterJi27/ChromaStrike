using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChromaStrike.UI
{
    /// <summary>Top-centre countdown. Turns amber under 30 s, red under 10 s and pulses every second.</summary>
    public sealed class TimerView : MonoBehaviour
    {
        [SerializeField] private TMP_Text text;
        [SerializeField] private Image underline;
        [SerializeField] private Color normalColor = Color.white;
        [SerializeField] private Color warningColor = new Color(1f, 0.74f, 0.2f);
        [SerializeField] private Color criticalColor = new Color(1f, 0.33f, 0.3f);
        [SerializeField, Min(1f)] private float warningSeconds = 30f;
        [SerializeField, Min(1f)] private float criticalSeconds = 10f;

        private int lastSecond = -1;

        public void SetTime(float seconds)
        {
            int whole = Mathf.CeilToInt(Mathf.Max(0f, seconds));
            text.text = $"{whole / 60:00}:{whole % 60:00}";

            Color color = seconds <= criticalSeconds ? criticalColor : seconds <= warningSeconds ? warningColor : normalColor;
            text.color = color;
            underline.color = color;

            if (whole != lastSecond && seconds <= criticalSeconds && seconds > 0f && isActiveAndEnabled)
                StartCoroutine(UiTween.Punch(text.transform, 0.12f, 0.25f));
            lastSecond = whole;
        }
    }
}
