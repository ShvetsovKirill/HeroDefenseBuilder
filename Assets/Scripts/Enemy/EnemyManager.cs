using System;
using System.Collections.Generic;
using UnityEngine;
using HeroDefense.Core;

namespace HeroDefense.Enemies
{
    /// <summary>
    /// Центральная точка для всех живых врагов.
    ///
    /// Один цикл на всю толпу (см. комментарий в Enemy — почему не Update на каждом):
    /// расталкивание, движение, осада.
    ///
    /// Позже сюда придёт flow field: вместо прямого вектора к цели
    /// направление будет браться из поля. Точка замены — ResolveDirection().
    /// </summary>
    public sealed class EnemyManager : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private EnemyPool pool;

        [Tooltip("Главная цель врагов — ратуша.")]
        [SerializeField] private Health mainTarget;

        [Header("Поведение")]
        [Tooltip("На какой дистанции враг останавливается и начинает бить цель.")]
        [SerializeField] private float attackDistance = 2.5f;

        [Header("Расталкивание")]
        [Tooltip("Радиус личного пространства. Ближе этого враги отталкивают друг друга. " +
                 "Примерно равен ширине модели: слишком мало — слипаются, " +
                 "слишком много — толпа рыхлая и не выглядит плотной.")]
        [SerializeField] private float separationRadius = 1f;

        [Tooltip("Сила расталкивания относительно скорости движения. " +
                 "Больше 1 — враги активно расползаются и хуже держат строй.")]
        [SerializeField] private float separationStrength = 0.6f;

        [Tooltip("Максимум соседей, учитываемых для одного врага. " +
                 "Ограничение бережёт производительность в плотной куче: " +
                 "первых нескольких достаточно, чтобы вытолкнуть агента.")]
        [SerializeField] private int maxNeighbours = 8;

        private readonly List<Enemy> _alive = new();

        // Переиспользуемые буферы: пересоздавать их каждый кадр
        // означало бы мусор и работу сборщику, что в WebGL особенно заметно.
        private readonly List<int> _neighbourBuffer = new();
        private readonly List<Vector3> _separationOffsets = new();

        private SpatialGrid _grid;

        /// <summary>Сколько врагов сейчас живо.</summary>
        public int AliveCount => _alive.Count;

        /// <summary>Враг начал осаду цели. Для звука и VFX.</summary>
        public event Action<Enemy> StartedSiege;

        /// <summary>Враг убит — начисление золота.</summary>
        public event Action<Enemy> EnemyKilled;

        private void Awake()
        {
            _grid = new SpatialGrid(separationRadius);
        }

        private void Update()
        {
            float deltaTime = Time.deltaTime;

            RebuildGrid();
            CalculateSeparation();
            ApplyBehaviour(deltaTime);
        }

        // ---------- Расталкивание ----------

        private void RebuildGrid()
        {
            _grid.Clear();

            for (int i = 0; i < _alive.Count; i++)
                _grid.Insert(i, _alive[i].transform.position);
        }

        /// <summary>
        /// Считаем смещения ОТДЕЛЬНО от применения.
        /// Иначе первый обработанный враг уже сдвинулся бы, и остальные
        /// считали бы расталкивание от новой позиции — толпа кренилась бы
        /// в сторону порядка обхода списка.
        /// </summary>
        private void CalculateSeparation()
        {
            EnsureOffsetCapacity();

            float radiusSqr = separationRadius * separationRadius;

            for (int i = 0; i < _alive.Count; i++)
                _separationOffsets[i] = ComputeSeparationFor(i, radiusSqr);
        }

        private Vector3 ComputeSeparationFor(int index, float radiusSqr)
        {
            Vector3 self = _alive[index].transform.position;

            _neighbourBuffer.Clear();
            _grid.QueryNeighbours(self, _neighbourBuffer);

            Vector3 push = Vector3.zero;
            int counted = 0;

            for (int n = 0; n < _neighbourBuffer.Count && counted < maxNeighbours; n++)
            {
                int other = _neighbourBuffer[n];

                if (other == index)
                    continue;

                Vector3 delta = self - _alive[other].transform.position;
                delta.y = 0f;

                float distanceSqr = delta.sqrMagnitude;

                if (distanceSqr >= radiusSqr)
                    continue;

                // Совпали точка в точку — расталкиваем в случайную сторону,
                // иначе нормализация даст ноль и они останутся слипшимися.
                if (distanceSqr < 0.0001f)
                {
                    push += RandomHorizontalDirection();
                    counted++;
                    continue;
                }

                // Чем ближе сосед, тем сильнее толчок.
                float distance = Mathf.Sqrt(distanceSqr);
                push += delta / distance * (1f - distance / separationRadius);
                counted++;
            }

            return push;
        }

        private static Vector3 RandomHorizontalDirection()
        {
            Vector2 random = UnityEngine.Random.insideUnitCircle.normalized;

            return new Vector3(random.x, 0f, random.y);
        }

        private void EnsureOffsetCapacity()
        {
            while (_separationOffsets.Count < _alive.Count)
                _separationOffsets.Add(Vector3.zero);
        }

        // ---------- Поведение ----------

        private void ApplyBehaviour(float deltaTime)
        {
            // С конца: умершие удаляются прямо в цикле.
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                Enemy enemy = _alive[i];
                Vector3 separation = _separationOffsets[i] * separationStrength;

                if (enemy.CurrentState == Enemy.State.Attacking)
                {
                    TickSiege(enemy, separation, deltaTime);
                    continue;
                }

                MoveTowardsTarget(enemy, separation, deltaTime);
            }
        }

        /// <summary>
        /// Осаждающие не идут к цели, но продолжают расталкиваться —
        /// иначе подошедшие слиплись бы в одну точку у стены вместо кольца.
        /// </summary>
        private void TickSiege(Enemy enemy, Vector3 separation, float deltaTime)
        {
            enemy.TickAttack(deltaTime);

            if (separation.sqrMagnitude > 0.0001f)
                enemy.transform.position += separation * (enemy.MoveSpeed * deltaTime);
        }

        private void MoveTowardsTarget(Enemy enemy, Vector3 separation, float deltaTime)
        {
            if (mainTarget == null)
                return;

            Vector3 toGoal = mainTarget.transform.position - enemy.transform.position;
            toGoal.y = 0f;

            if (toGoal.sqrMagnitude <= attackDistance * attackDistance)
            {
                BeginSiege(enemy);
                return;
            }

            Vector3 direction = ResolveDirection(toGoal) + separation;

            if (direction.sqrMagnitude > 0.0001f)
                direction.Normalize();

            ApplyMovement(enemy, direction, deltaTime);
        }

        /// <summary>
        /// Направление к цели. Сейчас — прямой вектор.
        /// Точка, где появится flow field.
        /// </summary>
        private static Vector3 ResolveDirection(Vector3 toGoal)
        {
            return toGoal.normalized;
        }

        private static void ApplyMovement(Enemy enemy, Vector3 direction, float deltaTime)
        {
            Transform t = enemy.transform;

            t.position += direction * (enemy.MoveSpeed * deltaTime);
            t.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private void BeginSiege(Enemy enemy)
        {
            enemy.BeginAttacking(mainTarget);
            StartedSiege?.Invoke(enemy);
        }

        // ---------- Спавн и смерть ----------

        public Enemy Spawn(float maxHealth, float moveSpeed, Vector3 position)
        {
            Enemy enemy = pool.Rent();

            enemy.Initialize(maxHealth, moveSpeed, position);
            enemy.Died += OnEnemyDied;

            _alive.Add(enemy);

            return enemy;
        }

        private void OnEnemyDied(Enemy enemy)
        {
            EnemyKilled?.Invoke(enemy);
            Despawn(enemy);
        }

        private void Despawn(Enemy enemy)
        {
            enemy.Died -= OnEnemyDied;

            RemoveFromAlive(enemy);
            pool.Return(enemy);
        }

        /// <summary>
        /// Удаление за O(1): на место убитого переезжает последний.
        /// List.Remove искал бы по всему списку — при массовых смертях заметно.
        /// Порядок в списке значения не имеет.
        /// </summary>
        private void RemoveFromAlive(Enemy enemy)
        {
            int index = _alive.IndexOf(enemy);

            if (index < 0)
                return;

            int last = _alive.Count - 1;

            _alive[index] = _alive[last];
            _alive.RemoveAt(last);
        }

        /// <summary>Убрать всех врагов — между картами или при рестарте.</summary>
        public void ClearAll()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                _alive[i].Died -= OnEnemyDied;
                pool.Return(_alive[i]);
            }

            _alive.Clear();
        }

        // ---------- Поиск целей ----------

        /// <summary>
        /// Ближайший живой враг в радиусе. null, если никого нет.
        ///
        /// Простой перебор: вызывается не каждый кадр (см. интервал в AutoAttacker),
        /// поэтому сетка здесь пока избыточна. Если станет узким местом —
        /// переиспользуем _grid.
        /// </summary>
        public Enemy FindNearest(Vector3 from, float maxRange)
        {
            float bestSqr = maxRange * maxRange;
            Enemy best = null;

            for (int i = 0; i < _alive.Count; i++)
            {
                Enemy candidate = _alive[i];

                if (!candidate.IsAlive)
                    continue;

                Vector3 delta = candidate.transform.position - from;
                delta.y = 0f;

                float distanceSqr = delta.sqrMagnitude;

                if (distanceSqr >= bestSqr)
                    continue;

                bestSqr = distanceSqr;
                best = candidate;
            }

            return best;
        }
    }
}
