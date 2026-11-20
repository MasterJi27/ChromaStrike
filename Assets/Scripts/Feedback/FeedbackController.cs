using UnityEngine;
using ChromaStrike.Core;
using ChromaStrike.Player;
using ChromaStrike.Spheres;

namespace ChromaStrike.Feedback
{
    /// <summary>
    /// Turns game events into sound and particles. Keeping this out of the gameplay classes means
    /// SpawnerManager and GameManager never need to know that audio or effects exist.
    /// </summary>
    public sealed class FeedbackController : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private GameManager game;
        [SerializeField] private SpawnerManager spawner;
        [SerializeField] private ClickHandler clickHandler;
        [SerializeField] private AudioService audioService;
        [SerializeField] private PopEffectPlayer popEffects;
        [SerializeField, Min(1)] private int tickFromSecond = 5;

        private int lastTickSecond = -1;

        private void OnEnable()
        {
            spawner.SphereDestroyed += OnSphereDestroyed;
            spawner.SphereSpawned += OnSphereSpawned;
            spawner.BatchSpawned += OnBatchSpawned;
            clickHandler.ShotFired += OnShotFired;
            game.StateChanged += OnStateChanged;
            game.TimeChanged += OnTimeChanged;
        }

        private void OnDisable()
        {
            spawner.SphereDestroyed -= OnSphereDestroyed;
            spawner.SphereSpawned -= OnSphereSpawned;
            spawner.BatchSpawned -= OnBatchSpawned;
            clickHandler.ShotFired -= OnShotFired;
            game.StateChanged -= OnStateChanged;
            game.TimeChanged -= OnTimeChanged;
        }

        private void OnSphereDestroyed(Sphere sphere)
        {
            audioService.Play(SfxId.Pop);
            popEffects.Play(sphere.transform.position, config.GetDefinition(sphere.Color).color);
        }

        private void OnSphereSpawned(Sphere sphere, SpawnReason reason)
        {
            if (reason == SpawnReason.Periodic) audioService.Play(SfxId.Spawn);
        }

        private void OnBatchSpawned(ColorTally batch) => audioService.Play(SfxId.Batch);

        private void OnShotFired(bool hit)
        {
            if (!hit) audioService.Play(SfxId.EmptyShot);
        }

        private void OnStateChanged(GameState state)
        {
            lastTickSecond = -1;
            if (state == GameState.Playing) audioService.Play(SfxId.GameStart);
            else if (state == GameState.Ended) audioService.Play(SfxId.GameEnd);
        }

        private void OnTimeChanged(float remaining)
        {
            int second = Mathf.CeilToInt(remaining);
            if (second > tickFromSecond || second <= 0 || second == lastTickSecond) return;
            lastTickSecond = second;
            audioService.Play(SfxId.Tick);
        }
    }
}
