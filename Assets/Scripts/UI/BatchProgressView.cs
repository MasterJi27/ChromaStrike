using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace ChromaStrike.UI
{
    /// <summary>Small bar that fills as you destroy spheres towards the next 10-sphere batch.</summary>
    public sealed class BatchProgressView : MonoBehaviour
    {
        [SerializeField] private Image fill;
        [SerializeField] private TMP_Text label;

        public void SetProgress(int destroyed, int batchSize)
        {
            fill.fillAmount = batchSize <= 0 ? 0f : (float)destroyed / batchSize;
            label.text = $"NEXT BATCH  {destroyed}/{batchSize}";
        }
    }
}
