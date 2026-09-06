using System;
using System.Collections;
using System.Collections.Generic;
using UnityEngine;
using HeroDefense.Enemies;

namespace HeroDefense.Waves
{
    /// <summary>
    /// Проигрывает карту: волна за волной, группа за группой.
    ///
    /// Ничего не решает про баланс — только исполняет то, что задано
    /// в LevelDefinition. Вся настройка сложности живёт в ассетах.
    /// </summary>
    public sealed class WaveRunner : MonoBehaviour
    {
        [Header("Что проигрываем")]
        [SerializeField] private LevelDefinition level;

        [Header("Ссылки")]
        [SerializeField] private EnemyManager enemyManager;

        [Tooltip("Точки входа врагов — тропы из леса. " +
                 "Порядок важен: на них ссылается spawnPointIndex в группах.")]
        [SerializeField] private Transform[] spawnPoints;

        [Header("Спавн")]
        [Tooltip("Разброс вокруг точки, чтобы враги не выходили строго в линию.")]
        [SerializeField] private float spawnSpread = 2f;

        [Header("Отладка")]
        [SerializeField] private bool autoStart = true;

        /// <summary>Номер текущей волны, с единицы. 0 — ещё не началось.</summary>
        public int CurrentWaveNumber { get; private set; }

        public int TotalWaves => level != null && level.waves != null ? level.waves.Length : 0;

        /// <summary>Идёт ли сейчас пауза между волнами.</summary>
        public bool IsBreak { get; private set; }

        /// <summary>Сколько секунд осталось до следующей волны. Для HUD.</summary>
        public float BreakTimeLeft { get; private set; }

        /// <summary>
        /// Прогресс текущей волны, 0..1. Для слайдера в HUD.
        ///
        /// Считается по убитым врагам от общего числа в волне, а не по времени:
        /// игрок должен видеть, сколько осталось перебить, а не сколько
        /// осталось ждать. Волна с двумя бугаями и волна с двадцатью роевыми
        /// идут разное время, но полоска в обоих случаях читается одинаково.
        /// </summary>
        public float WaveProgress
        {
            get
            {
                if (_currentWaveTotal <= 0)
                    return IsBreak ? 1f : 0f;

                return Mathf.Clamp01(_currentWaveKilled / (float)_currentWaveTotal);
            }
        }

        /// <summary>Сколько врагов в текущей волне всего. Для счётчика в HUD.</summary>
        public int CurrentWaveTotal => _currentWaveTotal;

        /// <summary>Сколько уже убито в текущей волне.</summary>
        public int CurrentWaveKilled => _currentWaveKilled;

        private int _currentWaveTotal;
        private int _currentWaveKilled;

        /// <summary>Началась новая волна. Аргумент — её номер с единицы.</summary>
        public event Action<int> WaveStarted;

        /// <summary>Волна отбита.</summary>
        public event Action<int> WaveCleared;

        /// <summary>Все волны карты пройдены.</summary>
        public event Action LevelCompleted;

        private Coroutine _routine;

        private void Start()
        {
            if (autoStart)
                StartLevel();
        }

        private void OnEnable()
        {
            if (enemyManager != null)
                enemyManager.EnemyKilled += OnEnemyKilled;
        }

        private void OnDisable()
        {
            if (enemyManager != null)
                enemyManager.EnemyKilled -= OnEnemyKilled;
        }

        private void OnEnemyKilled(Enemy _)
        {
            _currentWaveKilled++;
        }

        /// <summary>
        /// Подменить уровень до старта. Нужно кампании: какое владение
        /// играется, решает карта, а не поле в инспекторе боевой сцены.
        ///
        /// После старта менять нельзя — волны уже идут, и подмена дала бы
        /// половину одного уровня и половину другого.
        /// </summary>
        public void SetLevel(LevelDefinition newLevel)
        {
            if (_routine != null)
            {
                Debug.LogWarning("[WaveRunner] Уровень нельзя менять на ходу.", this);
                return;
            }

            level = newLevel;
        }

        public void StartLevel()
        {
            if (!IsSetupValid())
                return;

            Stop();
            _routine = StartCoroutine(RunLevel());
        }

        public void Stop()
        {
            if (_routine != null)
                StopCoroutine(_routine);

            _routine = null;
            IsBreak = false;
        }

        private bool IsSetupValid()
        {
            if (level == null || level.waves == null || level.waves.Length == 0)
            {
                Debug.LogError("[WaveRunner] Не задан уровень или в нём нет волн.", this);
                return false;
            }

            if (enemyManager == null)
            {
                Debug.LogError("[WaveRunner] Не назначен EnemyManager.", this);
                return false;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("[WaveRunner] Не назначены точки спавна.", this);
                return false;
            }

            return true;
        }

        // ---------- Проигрывание ----------

        private IEnumerator RunLevel()
        {
            yield return Wait(level.startDelay);

            for (int i = 0; i < level.waves.Length; i++)
            {
                WaveDefinition wave = level.waves[i];

                if (wave == null)
                    continue;

                CurrentWaveNumber = i + 1;

                // Счётчик волны считает по факту, с учётом условия: иначе
                // полоска прогресса дошла бы до конца на середине боя.
                _currentWaveTotal = Mathf.Max(1,
                    Mathf.RoundToInt(wave.TotalEnemies * WaveModifiers.EnemyCount));

                _currentWaveKilled = 0;

                WaveStarted?.Invoke(CurrentWaveNumber);

                yield return RunWave(wave);

                if (wave.waitForClear)
                    yield return WaitUntilCleared();

                WaveCleared?.Invoke(CurrentWaveNumber);

                // Условие живёт ровно одну волну: снимаем сразу после неё,
                // чтобы выбор на пятой не действовал молча на двадцатой.
                WaveModifiers.Clear();

                yield return RunBreak(wave.breakAfter);
            }

            LevelCompleted?.Invoke();
        }

        /// <summary>Все группы волны запускаются параллельно, каждая со своей задержкой.</summary>
        private IEnumerator RunWave(WaveDefinition wave)
        {
            var running = new List<Coroutine>();

            foreach (SpawnGroup group in wave.groups)
            {
                if (group != null && group.enemy != null)
                    running.Add(StartCoroutine(RunGroup(group)));
            }

            foreach (Coroutine routine in running)
                yield return routine;
        }

        private IEnumerator RunGroup(SpawnGroup group)
        {
            yield return Wait(group.startDelay);

            // Условие волны множит количество и плотность поверх группы.
            // Сама группа не меняется: собранные уровни остаются рабочими.
            int total = Mathf.Max(1, Mathf.RoundToInt(group.count * WaveModifiers.EnemyCount));
            float interval = group.interval * WaveModifiers.SpawnInterval;

            int spawned = 0;

            while (spawned < total)
            {
                int burst = Mathf.Min(group.burstSize, total - spawned);

                for (int i = 0; i < burst; i++)
                    SpawnOne(group);

                spawned += burst;

                if (spawned < total)
                    yield return Wait(interval);
            }
        }

        private void SpawnOne(SpawnGroup group)
        {
            EnemyDefinition definition = group.enemy;
            Vector3 position = ResolveSpawnPosition(group.spawnPointIndex);

            enemyManager.Spawn(definition, position);
        }

        private Vector3 ResolveSpawnPosition(int index)
        {
            Transform point = index >= 0 && index < spawnPoints.Length
                ? spawnPoints[index]
                : spawnPoints[UnityEngine.Random.Range(0, spawnPoints.Length)];

            Vector2 offset = UnityEngine.Random.insideUnitCircle * spawnSpread;

            return point.position + new Vector3(offset.x, 0f, offset.y);
        }

        // ---------- Ожидания ----------

        private IEnumerator WaitUntilCleared()
        {
            while (enemyManager.AliveCount > 0)
                yield return null;
        }

        private IEnumerator RunBreak(float duration)
        {
            if (duration <= 0f)
                yield break;

            IsBreak = true;
            BreakTimeLeft = duration;

            while (BreakTimeLeft > 0f)
            {
                BreakTimeLeft -= Time.deltaTime;
                yield return null;
            }

            IsBreak = false;
            BreakTimeLeft = 0f;
        }

        private static IEnumerator Wait(float seconds)
        {
            if (seconds > 0f)
                yield return new WaitForSeconds(seconds);
        }
    }
}
