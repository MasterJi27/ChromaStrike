using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChromaStrike.UI
{
    /// <summary>One line of the top-right counter: coloured dot, "Red Spheres:" label and the number.</summary>
    public sealed class CounterRow : MonoBehaviour
    {
        [SerializeField] private Image dot;
        [SerializeField] private TMP_Text label;
        [SerializeField] private TMP_Text value;

        private int shown = int.MinValue;

        public void Configure(string displayName, Color color)
        {
            label.text = $"{displayName} Spheres:";
            dot.color = color;
        }

        public void SetValue(int count)
        {
            if (count == shown) return;
            bool animate = shown != int.MinValue && isActiveAndEnabled;
            shown = count;
            value.text = count.ToString("00");
            if (animate) StartCoroutine(UiTween.Punch(value.transform, 0.25f, 0.18f));
        }
    }
}
