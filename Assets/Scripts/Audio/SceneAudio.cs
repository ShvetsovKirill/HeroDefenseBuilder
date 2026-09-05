using UnityEngine;
using UnityEngine.SceneManagement;
using HeroDefense.Base;
using HeroDefense.Building;
using HeroDefense.Core;
using HeroDefense.Enemies;
using HeroDefense.Waves;

namespace HeroDefense.Audio
{
    /// <summary>
    /// Звуковое сопровождение сцены: включает нужный трек и озвучивает
    /// события боя.
    ///
    /// Отдельный слушатель, а не вызовы из геймплея: враг не должен знать,
    /// что его смерть звучит. События для этого уже есть — их слушает
    /// и <c>GameHud</c>, здесь тот же приём.
    ///
    /// Объект создаётся сам при загрузке каждой сцены. Ставить его руками
    /// в четыре сцены — значит однажды забыть в одной и получить тишину,
    /// неотличимую от отсутствия файлов.
    /// </summary>
    [DefaultExecutionOrder(-700)]
    public sealed class SceneAudio : MonoBehaviour
    {
        private WaveRunner _waveRunner;
        private BuildController _buildController;
        private EnemyManager _enemyManager;
        private TownHall _townHall;
        private GameState _state;

        /// <summary>
        /// Подписка на загрузку сцен. Ставится один раз за запуск игры
        /// и живёт до его конца — отписываться не от чего.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Additive-сцена не своя: у неё нет ни своей музыки, ни своего боя.
            if (mode != LoadSceneMode.Single)
                return;

            var host = new GameObject("SceneAudio");

            host.AddComponent<SceneAudio>();
        }

        // ---------- Жизненный цикл ----------

        // Start, а не Awake: SceneContext и системы карты должны успеть проснуться.
        private void Start()
        {
            FindSystems();
            Subscribe();
            StartMusic();
        }

        private void OnDestroy()
        {
            Unsubscribe();
        }

        /// <summary>
        /// Поиск систем сцены.
        ///
        /// FindFirstObjectByType здесь допустим: он выполняется один раз
        /// при загрузке карты, а не в кадровом цикле. Ссылки в инспекторе
        /// потребовали бы ставить объект руками — ровно то, чего этот
        /// класс избегает.
        /// </summary>
        private void FindSystems()
        {
            SceneContext context = SceneContext.Current;

            _enemyManager = context?.EnemyManager;
            _townHall = context?.TownHall;
            _state = GameState.Current;

            _waveRunner = FindFirstObjectByType<WaveRunner>();
            _buildController = FindFirstObjectByType<BuildController>();
        }

        private void Subscribe()
        {
            if (_enemyManager != null)
            {
                _enemyManager.EnemyKilled += OnEnemyKilled;
                _enemyManager.StartedSiege += OnSiegeStarted;
            }

            if (_waveRunner != null)
            {
                _waveRunner.WaveStarted += OnWaveStarted;
                _waveRunner.WaveCleared += OnWaveCleared;
            }

            if (_buildController != null)
                _buildController.BuildingPlaced += OnBuildingPlaced;

            if (_state != null)
                _state.PhaseChanged += OnPhaseChanged;
        }

        private void Unsubscribe()
        {
            if (_enemyManager != null)
            {
                _enemyManager.EnemyKilled -= OnEnemyKilled;
                _enemyManager.StartedSiege -= OnSiegeStarted;
            }

            if (_waveRunner != null)
            {
                _waveRunner.WaveStarted -= OnWaveStarted;
                _waveRunner.WaveCleared -= OnWaveCleared;
            }

            if (_buildController != null)
                _buildController.BuildingPlaced -= OnBuildingPlaced;

            if (_state != null)
                _state.PhaseChanged -= OnPhaseChanged;
        }

        // ---------- Музыка ----------

        /// <summary>
        /// Боевой трек включается там, где есть карта боя, остальным сценам —
        /// спокойный. Признак — наличие <see cref="SceneContext"/>: он есть
        /// ровно на боевых картах.
        /// </summary>
        private void StartMusic()
        {
            AudioService service = AudioService.Instance;

            if (service == null || service.Catalog == null)
                return;

            bool battle = SceneContext.Current != null;

            service.PlayMusic(battle ? service.Catalog.battleMusic : service.Catalog.menuMusic);
        }

        // ---------- События боя ----------

        private void OnEnemyKilled(Enemy enemy)
        {
            if (enemy != null)
                Sfx.PlayAt(SoundId.EnemyDeath, enemy.transform.position);
        }

        private void OnSiegeStarted(Enemy enemy)
        {
            // Тревога привязана к ратуше, а не к врагу: игрок должен услышать,
            // где беда, даже если сам на другом краю поляны.
            Vector3 position = _townHall != null ? _townHall.transform.position : transform.position;

            Sfx.PlayAt(SoundId.TownHallAlarm, position);
        }

        private void OnWaveStarted(int number) => Sfx.Play(SoundId.WaveStart);

        private void OnWaveCleared(int number) => Sfx.Play(SoundId.WaveCleared);

        private void OnBuildingPlaced(GameObject building)
        {
            if (building != null)
                Sfx.PlayAt(SoundId.BuildingPlaced, building.transform.position);
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            switch (phase)
            {
                case GamePhase.Defeat:
                    Sfx.Play(SoundId.Defeat);
                    break;

                case GamePhase.Victory:
                    Sfx.Play(SoundId.Victory);
                    break;
            }
        }
    }
}
