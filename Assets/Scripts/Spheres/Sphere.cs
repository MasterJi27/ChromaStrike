using System;
using System.Collections;
using UnityEngine;

namespace ChromaStrike.Spheres
{
    /// <summary>
    /// One target sphere. It knows its colour, plays a small grow-in animation and tells the
    /// outside world when it was destroyed. All game rules live elsewhere (SpawnerManager).
    /// </summary>
    [RequireComponent(typeof(SphereCollider))]
    [DisallowMultipleComponent]
    public sealed class Sphere : MonoBehaviour
    {
        [SerializeField] private SphereColor color;
        [SerializeField, Min(0.05f)] private float growDuration = 0.35f;

        private bool destroyed;

        public SphereColor Color => color;

        /// <summary>Raised once, right before the object is removed.</summary>
        public event Action<Sphere> Destroyed;

        private void OnEnable() => StartCoroutine(GrowIn());

        /// <summary>Called by ClickHandler when the centre ray hits this sphere.</summary>
        public void Pop()
        {
            if (destroyed) return;
            destroyed = true;
            Destroyed?.Invoke(this);
            Destroy(gameObject);
        }

        /// <summary>Scales from 0 to 1 with a slight overshoot ("ease out back").</summary>
        private IEnumerator GrowIn()
        {
            const float overshoot = 1.70158f;
            Vector3 target = Vector3.one;
            for (float t = 0f; t < growDuration; t += Time.deltaTime)
            {
                float x = t / growDuration - 1f;
                float eased = 1f + (overshoot + 1f) * x * x * x + overshoot * x * x;
                transform.localScale = target * Mathf.Max(0.01f, eased);
                yield return null;
            }
            transform.localScale = target;
        }
    }
}
