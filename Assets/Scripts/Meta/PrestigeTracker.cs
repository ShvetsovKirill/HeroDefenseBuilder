using System.Collections.Generic;
using UnityEngine;
using HeroDefense.Base;
using HeroDefense.Core;
using HeroDefense.Waves;

namespace HeroDefense.Meta
{
    /// <summary>
    /// Начисление престижа за забег (D92, D118–D120).
    ///
    /// Две группы наград, и это не случайность:
    ///
    /// По ходу — за отбитые волны и пройденные акты. Копится и не сгорает
    /// при поражении: провал часть цикла, а не поломка (D79, D118).
    ///
    /// В конце — за то, что УЦЕЛЕЛО: ратуша, постройки, домики. Начислять
    /// за факт постройки нельзя: снос возвращает 50% (D43), и игрок качал бы
    /// престиж циклом «построил — снёс — построил» вместо того чтобы воевать.
    /// Награда за удержание такой дыры не имеет (D119).
    ///
    /// Итог передаётся в PlayerProgress, который переживает смену сцены.
    /// </summary>
    public sealed class PrestigeTracker : MonoBehaviour
    {
        [Header("Ссылки")]
        [Tooltip("Раннер волн этой карты. Остальное берётся из SceneContext.")]
        [SerializeField] private WaveRunner waveRunner;

        [Header("Награды по ходу")]
        [SerializeField] private int perWave = 5;

        [Tooltip("Сколько волн составляют один акт. При 24 волнах и трёх актах — 8.")]
        [SerializeField] private int wavesPerAct = 8;

        [SerializeField] private int perAct = 25;

        [Header("Награды в конце")]
        [Tooltip("За полностью пройденный забег.")]
        [SerializeField] private int runCompleted = 50;

        [Tooltip("За уцелевшую ратушу.")]
        [SerializeField] private int townHallSurvived = 100;

        [Tooltip("За каждую уцелевшую постройку.")]
        [SerializeField] private int perBuilding = 10;

        [Tooltip("За каждый уцелевший домик (D112). " +
                 "Дороже постройки намеренно: постройка работает на тебя " +
                 "весь бой, домик только просит защиты — иначе его невыгодно защищать.")]
        [SerializeField] private int perHouse = 15;

        [Header("Возврат в замок")]
        [Tooltip("Через сколько секунд после конца забега уйти в замок. " +
                 "Ноль — не уходить автоматически: экран итогов сам решит, " +
                 "когда игрок насмотрелся.")]
        [SerializeField] private float returnToCastleDelay = 4f;

        [Header("Отладка")]
        [SerializeField] private bool logRewards = true;

        /// <summary>Сколько престижа набрано за этот забег. Для экрана итогов.</summary>
        public int EarnedThisRun { get; private set; }

        /// <summary>Расшифровка начислений: строка — за что и сколько.</summary>
        public IReadOnlyList<string> Breakdown => _breakdown;

        private readonly List<string> _breakdown = new();

        private bool _finalized;

        private static GameState State => GameState.Current;
        private static TownHall Hall => SceneContext.Current?.TownHall;

        private void OnEnable()
        {
            if (waveRunner != null)
                waveRunner.WaveCleared += OnWaveCleared;

            if (State != null)
                State.PhaseChanged += OnPhaseChanged;
        }

        private void OnDisable()
        {
            if (waveRunner != null)
                waveRunner.WaveCleared -= OnWaveCleared;

            if (State != null)
                State.PhaseChanged -= OnPhaseChanged;
        }

        // ---------- По ходу забега ----------

        private void OnWaveCleared(int waveNumber)
        {
            Award(perWave, $"Волна {waveNumber} отбита");

            // Акт засчитан, когда пройдена его последняя волна.
            if (wavesPerAct > 0 && waveNumber % wavesPerAct == 0)
                Award(perAct, $"Акт {waveNumber / wavesPerAct} пройден");
        }

        // ---------- Конец забега ----------

        private void OnPhaseChanged(GamePhase phase)
        {
            if (phase is GamePhase.Victory or GamePhase.Defeat)
                Finalize(phase == GamePhase.Victory);
        }

        /// <summary>
        /// Подвести итог. Защищено флагом: победа и поражение могут прийти
        /// почти одновременно — волны кончаются ровно в тот момент, когда
        /// ратушу добивают.
        /// </summary>
        private void Finalize(bool victory)
        {
            if (_finalized)
                return;

            _finalized = true;

            if (victory)
            {
                Award(runCompleted, "Забег пройден");
                Award(townHallSurvived, "Ратуша уцелела");
            }

            AwardSurvivors();

            int waveReached = waveRunner != null ? waveRunner.CurrentWaveNumber : 0;

            PlayerProgress.RegisterRunFinished(victory, waveReached);
            PlayerProgress.AddPrestige(EarnedThisRun);

            // Слепок для замка: сцена сейчас выгрузится, и передать итог
            // объектом будет некуда.
            RunResult.Store(victory, waveReached, EarnedThisRun, _breakdown);

            if (logRewards)
                Debug.Log($"[Престиж] Забег окончен. Начислено {EarnedThisRun}.\n{BuildReport()}");

            if (returnToCastleDelay > 0f)
                StartCoroutine(ReturnToCastle());
        }

        /// <summary>
        /// Возврат в замок. Ждём в реальном времени: конец забега ставит
        /// timeScale в ноль, и обычная задержка никогда бы не истекла.
        /// </summary>
        private System.Collections.IEnumerator ReturnToCastle()
        {
            yield return new WaitForSecondsRealtime(returnToCastleDelay);

            // В походе игрока ведёт кампания: она знает, вернуться ли
            // на карту за следующим владением или в лагерь подводить итоги.
            // Иначе оба возврата спорят, и один из них молча отбрасывается
            // загрузчиком как «идёт другая загрузка».
            if (Campaign.CampaignRun.IsActive)
                yield break;

            App.SceneLoader.Instance?.GoToCastle();
        }

        /// <summary>
        /// Считаем уцелевшие постройки и домики.
        ///
        /// Разовый поиск по сцене, а не подписка на каждую постройку:
        /// это происходит один раз за забег, в момент, когда игра уже
        /// остановлена. Держать ради этого реестр живых объектов дороже,
        /// чем один проход.
        ///
        /// Реестр построек (BuildRegistry) здесь не подходит: он считает
        /// по типам и не знает, какие из них ещё стоят на карте.
        /// </summary>
        private void AwardSurvivors()
        {
            HeroDefense.Building.Building[] buildings =
                FindObjectsByType<HeroDefense.Building.Building>(FindObjectsSortMode.None);

            int alive = 0;

            foreach (HeroDefense.Building.Building building in buildings)
            {
                if (building != null && building.Health != null && building.Health.Current > 0f)
                    alive++;
            }

            if (alive > 0)
                Award(perBuilding * alive, $"Уцелевшие постройки: {alive}");

            // Домиков на карте пока нет (D120): компонент появится вместе
            // с ассетами, до тех пор начисление просто нулевое.
            int houses = CountSurvivingHouses();

            if (houses > 0)
                Award(perHouse * houses, $"Уцелевшие домики: {houses}");
        }

        /// <summary>
        /// Домики (D112) — вторая цель защиты: не занимают слоты, не воюют,
        /// уцелевшие дают престиж. Пока их нет в сцене, метод возвращает ноль.
        /// </summary>
        private int CountSurvivingHouses()
        {
            NeutralHouse[] houses = FindObjectsByType<NeutralHouse>(FindObjectsSortMode.None);

            int alive = 0;

            foreach (NeutralHouse house in houses)
            {
                if (house != null && house.IsAlive)
                    alive++;
            }

            return alive;
        }

        // ---------- Служебное ----------

        private void Award(int amount, string reason)
        {
            if (amount <= 0)
                return;

            EarnedThisRun += amount;
            _breakdown.Add($"{reason}: +{amount}");
        }

        /// <summary>Расшифровка для экрана итогов и консоли.</summary>
        public string BuildReport()
        {
            if (_breakdown.Count == 0)
                return "Престиж не начислен.";

            var text = new System.Text.StringBuilder();

            foreach (string line in _breakdown)
                text.AppendLine($"  {line}");

            text.AppendLine($"  ИТОГО: {EarnedThisRun}");

            return text.ToString();
        }
    }
}
