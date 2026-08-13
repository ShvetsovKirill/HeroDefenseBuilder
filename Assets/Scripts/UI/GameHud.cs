using TMPro;
using UnityEngine;
using HeroDefense.Base;
using HeroDefense.Core;
using HeroDefense.Economy;
using HeroDefense.Enemies;
using HeroDefense.Waves;

namespace HeroDefense.UI
{
    /// <summary>
    /// Игровой HUD на TextMeshPro.
    ///
    /// Заменяет отладочный OnGUI: тот годился, пока цифры смотрел только
    /// разработчик, но OnGUI не масштабируется, не настраивается
    /// и вызывается по нескольку раз за кадр.
    ///
    /// Пока это просто текст. Полоски, иконки и диегетичные индикаторы
    /// придут на этапе UI — но уже поверх нормального Canvas.
    /// </summary>
    public sealed class GameHud : MonoBehaviour
    {
        [Header("Данные")]
        [SerializeField] private TownHall townHall;
        [SerializeField] private EnemyManager enemyManager;
        [SerializeField] private Wallet wallet;
        [SerializeField] private WaveRunner waveRunner;
        [SerializeField] private GameLoop gameLoop;

        [Header("Элементы")]
        [SerializeField] private TMP_Text goldText;
        [SerializeField] private TMP_Text waveText;
        [SerializeField] private TMP_Text enemyCountText;

        [Tooltip("Полоска здоровья ратуши. Заполнение задаётся через fillAmount.")]
        [SerializeField] private UnityEngine.UI.Image townHallFill;

        [SerializeField] private TMP_Text townHallText;

        [Header("Поражение")]
        [SerializeField] private GameObject gameOverRoot;

        private Health TownHallHealth => townHall != null ? townHall.Health : null;

        private void OnEnable()
        {
            if (wallet != null)
                wallet.GoldChanged += OnGoldChanged;

            if (waveRunner != null)
            {
                waveRunner.WaveStarted += OnWaveChanged;
                waveRunner.WaveCleared += OnWaveChanged;
            }

            if (gameLoop != null)
                SetGameOverVisible(gameLoop.IsGameOver);

            RefreshAll();
        }

        private void OnDisable()
        {
            if (wallet != null)
                wallet.GoldChanged -= OnGoldChanged;

            if (waveRunner != null)
            {
                waveRunner.WaveStarted -= OnWaveChanged;
                waveRunner.WaveCleared -= OnWaveChanged;
            }
        }

        private void Update()
        {
            // Эти три меняются постоянно, событиями их не покрыть дёшево.
            UpdateTownHall();
            UpdateEnemyCount();
            UpdateWaveTimer();

            if (gameLoop != null)
                SetGameOverVisible(gameLoop.IsGameOver);
        }

        private void RefreshAll()
        {
            UpdateGold();
            UpdateTownHall();
            UpdateEnemyCount();
            UpdateWaveTimer();
        }

        // ---------- Обновления ----------

        private void OnGoldChanged(int _) => UpdateGold();
        private void OnWaveChanged(int _) => UpdateWaveTimer();

        private void UpdateGold()
        {
            if (goldText != null && wallet != null)
                goldText.text = wallet.Gold.ToString();
        }

        private void UpdateTownHall()
        {
            Health health = TownHallHealth;

            if (health == null)
                return;

            if (townHallFill != null)
                townHallFill.fillAmount = health.Fraction;

            if (townHallText != null)
                townHallText.text = $"{health.Current:F0} / {health.Max:F0}";
        }

        private void UpdateEnemyCount()
        {
            if (enemyCountText != null && enemyManager != null)
                enemyCountText.text = enemyManager.AliveCount.ToString();
        }

        private void UpdateWaveTimer()
        {
            if (waveText == null || waveRunner == null)
                return;

            waveText.text = waveRunner.IsBreak
                ? $"Следующая волна: {waveRunner.BreakTimeLeft:F0}"
                : $"Волна {waveRunner.CurrentWaveNumber} / {waveRunner.TotalWaves}";
        }

        private void SetGameOverVisible(bool visible)
        {
            if (gameOverRoot != null && gameOverRoot.activeSelf != visible)
                gameOverRoot.SetActive(visible);
        }
    }
}
