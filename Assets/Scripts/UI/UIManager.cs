using TMPro;
using UnityEngine;
using UnityEngine.UI;
using ChromaStrike.Core;
using ChromaStrike.Player;
using ChromaStrike.Spheres;

namespace ChromaStrike.UI
{
    /// <summary>
    /// Connects game events to the three screens: Start, In-Game HUD and End.
    /// It only listens and displays; it never changes game rules (buttons simply call GameManager).
    /// </summary>
    public sealed class UIManager : MonoBehaviour
    {
        [Header("Systems")]
        [SerializeField] private GameConfig config;
        [SerializeField] private GameManager game;
        [SerializeField] private SpawnerManager spawner;
        [SerializeField] private ClickHandler clickHandler;

        [Header("Screens")]
        [SerializeField] private PanelFader startScreen;
        [SerializeField] private PanelFader hudScreen;
        [SerializeField] private PanelFader endScreen;

        [Header("HUD")]
        [SerializeField] private TimerView timerView;
        [SerializeField] private CounterRow redRow;
        [SerializeField] private CounterRow greenRow;
        [SerializeField] private CounterRow blueRow;
        [SerializeField] private BatchProgressView batchProgress;
        [SerializeField] private ToastView toast;
        [SerializeField] private CrosshairView crosshair;

        [Header("End screen")]
        [SerializeField] private TMP_Text endRed;
        [SerializeField] private TMP_Text endGreen;
        [SerializeField] private TMP_Text endBlue;
        [SerializeField] private TMP_Text endSummary;

        [Header("Buttons")]
        [SerializeField] private Button startButton;
        [SerializeField] private Button restartButton;
        [SerializeField] private Button quitFromMenuButton;
        [SerializeField] private Button quitFromEndButton;

        private void Awake()
        {
            redRow.Configure(config.GetDefinition(SphereColor.Red).displayName, config.GetDefinition(SphereColor.Red).color);
            greenRow.Configure(config.GetDefinition(SphereColor.Green).displayName, config.GetDefinition(SphereColor.Green).color);
            blueRow.Configure(config.GetDefinition(SphereColor.Blue).displayName, config.GetDefinition(SphereColor.Blue).color);

            startButton.onClick.AddListener(game.StartGame);
            restartButton.onClick.AddListener(game.RestartGame);
            quitFromMenuButton.onClick.AddListener(game.QuitGame);
            quitFromEndButton.onClick.AddListener(game.QuitGame);
        }

        private void OnEnable()
        {
            game.StateChanged += OnStateChanged;
            game.TimeChanged += timerView.SetTime;
            spawner.LiveCountsChanged += OnLiveCountsChanged;
            spawner.BatchProgressChanged += batchProgress.SetProgress;
            spawner.BatchSpawned += OnBatchSpawned;
            clickHandler.ShotFired += OnShotFired;
        }

        private void OnDisable()
        {
            game.StateChanged -= OnStateChanged;
            game.TimeChanged -= timerView.SetTime;
            spawner.LiveCountsChanged -= OnLiveCountsChanged;
            spawner.BatchProgressChanged -= batchProgress.SetProgress;
            spawner.BatchSpawned -= OnBatchSpawned;
            clickHandler.ShotFired -= OnShotFired;
        }

        private void Start()
        {
            OnLiveCountsChanged(spawner.LiveCounts);
            batchProgress.SetProgress(0, config.replacementBatchSize);
            timerView.SetTime(config.roundDurationSeconds);
            OnStateChanged(game.State, instant: true);
        }

        private void OnStateChanged(GameState state) => OnStateChanged(state, false);

        private void OnStateChanged(GameState state, bool instant)
        {
            startScreen.Show(state == GameState.Menu, instant);
            hudScreen.Show(state == GameState.Playing, instant);
            endScreen.Show(state == GameState.Ended, instant);

            if (state == GameState.Ended) FillEndScreen();
        }

        private void OnLiveCountsChanged(ColorTally counts)
        {
            redRow.SetValue(counts.red);
            greenRow.SetValue(counts.green);
            blueRow.SetValue(counts.blue);
        }

        private void OnBatchSpawned(ColorTally batch) =>
            toast.Show($"BATCH RESTORED   {batch.red} RED   {batch.green} GREEN   {batch.blue} BLUE");

        private void OnShotFired(bool hit)
        {
            if (hit) crosshair.FlashHit();
        }

        private void FillEndScreen()
        {
            ColorTally finalCounts = spawner.LiveCounts;
            endRed.text = finalCounts.red.ToString();
            endGreen.text = finalCounts.green.ToString();
            endBlue.text = finalCounts.blue.ToString();
            endSummary.text = $"{spawner.TotalDestroyed} spheres destroyed   |   {spawner.BatchesSpawned} batches restored";
        }
    }
}
