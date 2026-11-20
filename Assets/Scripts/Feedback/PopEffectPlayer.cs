using System.Collections;
using UnityEngine;
using UnityEngine.Pool;

namespace ChromaStrike.Feedback
{
    /// <summary>
    /// Plays the particle burst when a sphere is destroyed.
    /// Particle systems are pooled (UnityEngine.Pool.ObjectPool) instead of created and destroyed
    /// on every hit, which avoids garbage collection spikes during fast shooting.
    /// </summary>
    public sealed class PopEffectPlayer : MonoBehaviour
    {
        [SerializeField] private ParticleSystem prefab;

        private ObjectPool<ParticleSystem> pool;

        private void Awake()
        {
            pool = new ObjectPool<ParticleSystem>(
                createFunc: () => Instantiate(prefab, transform),
                actionOnGet: ps => ps.gameObject.SetActive(true),
                actionOnRelease: ps => { ps.Stop(true, ParticleSystemStopBehavior.StopEmittingAndClear); ps.gameObject.SetActive(false); },
                actionOnDestroy: ps => { if (ps != null) Destroy(ps.gameObject); },
                collectionCheck: false, defaultCapacity: 8, maxSize: 32);
        }

        public void Play(Vector3 position, Color color)
        {
            ParticleSystem ps = pool.Get();
            ps.transform.position = position;

            ParticleSystem.MainModule main = ps.main;
            main.startColor = new ParticleSystem.MinMaxGradient(color, Color.Lerp(color, Color.white, 0.55f));
            ps.Play(true);

            StartCoroutine(ReleaseWhenDone(ps, main.startLifetime.constantMax + main.duration));
        }

        private IEnumerator ReleaseWhenDone(ParticleSystem ps, float seconds)
        {
            yield return new WaitForSeconds(seconds);
            if (ps != null) pool.Release(ps);
        }
    }
}
