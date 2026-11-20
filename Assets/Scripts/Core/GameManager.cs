using System;
using UnityEngine;
using ChromaStrike.Player;
using ChromaStrike.Spheres;

namespace ChromaStrike.Core
{
    /// <summary>
    /// The conductor. It owns the game state (Menu -> Playing -> Ended), the 120 second countdown,
    /// and switches the other systems on and off. It contains no spawn or UI logic itself.
    /// </summary>
    public sealed class GameManager : MonoBehaviour
    {
        [SerializeField] private GameConfig config;
        [SerializeField] private SpawnerManager spawner;
        [SerializeField] private ClickHandler clickHandler;
        [SerializeField] private FirstPersonController player;
        [SerializeField] private CursorController cursor;

        private CountdownTimer timer;

        public GameState State { get; private set; } = GameState.Menu;
        public float RemainingSeconds => timer.Remaining;
        public float DurationSeconds => timer.Duration;

        public event Action<GameState> StateChanged;
        /// <summary>Raised every frame while playing with the seconds left.</summary>
        public event Action<float> TimeChanged;

        private void Awake()
        {
            timer = new CountdownTimer(config.roundDurationSeconds);
        }

        private void Start() => EnterState(GameState.Menu);

        private void Update()
        {
            if (State == GameState.Playing) Advance(Time.deltaTime);
        }

        /// <summary>
        /// Moves the round forward by <paramref name="deltaTime"/> seconds.
        /// Public (instead of private) so automated tests can fast forward two minutes in one call.
        /// </summary>
        public void Advance(float deltaTime)
        {
            if (State != GameState.Playing) return;

            // Never simulate past the deadline, so nothing spawns after the last second.
            spawner.Tick(Mathf.Min(deltaTime, timer.Remaining));

            bool finished = timer.Advance(deltaTime);
            TimeChanged?.Invoke(timer.Remaining);
            if (finished) EndGame();
        }

        public void StartGame()
        {
            spawner.ResetAll();
            timer.Reset(config.roundDurationSeconds);
            player.ResetPose();
            spawner.SpawnInitialBatch();
            EnterState(GameState.Playing);
            TimeChanged?.Invoke(timer.Remaining);
        }

        public void RestartGame() => StartGame();

        public void EndGame()
        {
            if (State != GameState.Playing) return;
            EnterState(GameState.Ended);
        }

        public void QuitGame()
        {
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }

        private void EnterState(GameState next)
        {
            State = next;
            bool playing = next == GameState.Playing;

            spawner.enabled = playing; // pending spawn retries must also stop at the deadline
            clickHandler.IsActive = playing;
            player.ControlEnabled = playing;
            cursor.SetGameplayMode(playing);
            // timeScale 0 freezes physics, particles and animations: "stop all gameplay after 2 minutes".
            Time.timeScale = next == GameState.Ended ? 0f : 1f;

            StateChanged?.Invoke(next);
        }
    }
}
