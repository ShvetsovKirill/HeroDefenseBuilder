using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HeroDefense.Base;
using HeroDefense.Core;
using HeroDefense.Economy;
using HeroDefense.Squads;
using HeroDefense.Waves;

namespace HeroDefense.UI
{
    /// <summary>
    /// Игровой HUD.
    ///
    /// Ссылки на системы берутся из SceneContext, поля разметки назначаются
    /// вручную: HUD знает про свою вёрстку, но не про то, где на сцене лежит
    /// кошелёк.
    ///
    /// Постоянного счётчика врагов и полоски HP ратуши здесь нет намеренно.
    /// Число живых врагов не отвечает ни на один вопрос игрока: «много ли
    /// осталось» показывает слайдер, «где они» — сама карта. А состояние
    /// ратуши нужно знать ровно в один момент — когда до неё добрались,
    /// а ты на другом конце поляны. Поэтому вместо индикатора — сигнал.
    /// </summary>
    public sealed class GameHud : MonoBehaviour
    {
        [Header("Системы")]
        [SerializeField] private WaveRunner waveRunner;

        [Header("Ресурсы")]
        [SerializeField] private TMP_Text goldText;

        [Tooltip("Счётчик живых бойцов отрядов.")]
        [SerializeField] private TMP_Text armyText;

        [Tooltip("Опыт. Пока заглушка: в бою ресурс один — золото (D31). " +
                 "Опыт появится вместе с метой (D78).")]
        [SerializeField] private TMP_Text xpText;

        [Header("Волна")]
        [SerializeField] private TMP_Text waveCountText;

        [Tooltip("Строка под номером волны: таймер паузы или прогресс.")]
        [SerializeField] private TMP_Text nextWaveCountText;

        [Tooltip("Доля перебитых врагов волны. Не время: волна из двух бугаёв " +
                 "и волна из двадцати роевых идут разное время, но полоска " +
                 "в обоих случаях читается одинаково.")]
        [SerializeField] private Slider waveProgressSlider;

        [Header("Король")]
        [SerializeField] private TMP_Text kingNameText;

        [Header("Тревога ратуши")]
        [Tooltip("Красная рамка по краю экрана. Появляется только когда ратушу " +
                 "бьют — это и есть «громкая обратная связь» из D2.")]
        [SerializeField] private CanvasGroup damageVignette;

        [Tooltip("Сколько секунд тревога держится после последнего удара.")]
        [SerializeField] private float alarmHoldTime = 1.5f;

        [Tooltip("Скорость появления и затухания.")]
        [SerializeField] private float alarmFadeSpeed = 4f;

        [Tooltip("Текст с HP ратуши. Показывается только во время тревоги. " +
                 "Необязательно.")]
        [SerializeField] private TMP_Text townHallText;

        [Header("Модальные экраны")]
        [SerializeField] private GameObject gameOverRoot;
        [SerializeField] private GameObject victoryRoot;

        private float _alarmTimer;

        private Wallet Wallet => SceneContext.Current?.Wallet;
        private TownHall Hall => SceneContext.Current?.TownHall;

        // Start, а не OnEnable: SceneContext должен успеть зарегистрироваться.
        private void Start()
        {
            Subscribe();
            RefreshStatic();
            HideAlarmInstantly();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        private void Subscribe()
        {
            if (Wallet != null)
                Wallet.GoldChanged += OnGoldChanged;

            if (waveRunner != null)
            {
                waveRunner.WaveStarted += OnWaveChanged;
                waveRunner.WaveCleared += OnWaveChanged;
            }

            if (Hall != null && Hall.Health != null)
                Hall.Health.Damaged += OnTownHallDamaged;

            SquadRegistry.Changed += UpdateArmyCount;
        }

        private void Unsubscribe()
        {
            if (Wallet != null)
                Wallet.GoldChanged -= OnGoldChanged;

            if (waveRunner != null)
            {
                waveRunner.WaveStarted -= OnWaveChanged;
                waveRunner.WaveCleared -= OnWaveChanged;
            }

            if (Hall != null && Hall.Health != null)
                Hall.Health.Damaged -= OnTownHallDamaged;

            SquadRegistry.Changed -= UpdateArmyCount;
        }

        private void Update()
        {
            UpdateWaveLine();
            UpdateWaveProgress();
            UpdateAlarm();
            UpdateModals();
        }

        /// <summary>То, что меняется редко и обновляется по событиям.</summary>
        private void RefreshStatic()
        {
            UpdateGold();
            UpdateWaveNumber();
            UpdateKingName();
            UpdateArmyCount();
        }

        // ---------- Ресурсы ----------

        private void OnGoldChanged(int _) => UpdateGold();

        private void UpdateGold()
        {
            if (goldText != null && Wallet != null)
                goldText.text = Wallet.Gold.ToString();
        }

        /// <summary>
        /// Живые бойцы всех отрядов.
        ///
        /// Считается по событию, а не каждый кадр: раньше здесь был
        /// FindObjectsByType, сканировавший всю сцену 60 раз в секунду.
        /// </summary>
        private void UpdateArmyCount()
        {
            if (armyText != null)
                armyText.text = SquadRegistry.TotalAliveUnits.ToString();
        }

        // ---------- Волна ----------

        private void OnWaveChanged(int _) => UpdateWaveNumber();

        private void UpdateWaveNumber()
        {
            if (waveCountText != null && waveRunner != null)
                waveCountText.text = $"Волна {waveRunner.CurrentWaveNumber}";
        }

        private void UpdateWaveLine()
        {
            if (nextWaveCountText == null || waveRunner == null)
                return;

            nextWaveCountText.text = waveRunner.IsBreak
                ? $"До следующей волны: {FormatTime(waveRunner.BreakTimeLeft)}"
                : $"Волна {waveRunner.CurrentWaveNumber} из {waveRunner.TotalWaves}";
        }

        private static string FormatTime(float seconds)
        {
            int total = Mathf.Max(0, Mathf.CeilToInt(seconds));

            return $"{total / 60}:{total % 60:00}";
        }

        private void UpdateWaveProgress()
        {
            if (waveProgressSlider != null && waveRunner != null)
                waveProgressSlider.value = waveRunner.WaveProgress;
        }

        // ---------- Король ----------

        private void UpdateKingName()
        {
            if (kingNameText == null)
                return;

            Transform king = SceneContext.Current?.King;

            if (king == null)
                return;

            var component = king.GetComponent<King.King>();

            if (component != null && component.Definition != null)
                kingNameText.text = component.Definition.displayName;
        }

        // ---------- Тревога ----------

        private void OnTownHallDamaged(float amount)
        {
            _alarmTimer = alarmHoldTime;
        }

        private void UpdateAlarm()
        {
            if (damageVignette == null)
                return;

            _alarmTimer -= Time.deltaTime;

            float target = _alarmTimer > 0f ? 1f : 0f;

            damageVignette.alpha = Mathf.MoveTowards(
                damageVignette.alpha, target, alarmFadeSpeed * Time.deltaTime);

            UpdateAlarmText();
        }

        /// <summary>
        /// Цифры HP показываем только во время тревоги: постоянный индикатор
        /// висел бы фоном и перестал бы читаться.
        /// </summary>
        private void UpdateAlarmText()
        {
            if (townHallText == null)
                return;

            bool visible = damageVignette.alpha > 0.05f;

            if (visible && Hall != null && Hall.Health != null)
                townHallText.text = $"{Hall.Health.Current:F0} / {Hall.Health.Max:F0}";

            SetActiveIfNeeded(townHallText.gameObject, visible);
        }

        private void HideAlarmInstantly()
        {
            if (damageVignette != null)
                damageVignette.alpha = 0f;

            if (townHallText != null)
                SetActiveIfNeeded(townHallText.gameObject, false);
        }

        // ---------- Модальные экраны ----------

        private void UpdateModals()
        {
            GameState state = GameState.Current;

            if (state == null)
                return;

            SetActiveIfNeeded(gameOverRoot, state.Phase == GamePhase.Defeat);
            SetActiveIfNeeded(victoryRoot, state.Phase == GamePhase.Victory);
        }

        private static void SetActiveIfNeeded(GameObject target, bool visible)
        {
            if (target != null && target.activeSelf != visible)
                target.SetActive(visible);
        }
    }
}
