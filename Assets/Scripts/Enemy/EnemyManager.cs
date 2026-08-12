using System.Collections.Generic;
using UnityEngine;

namespace HeroDefense.Enemies
{
    /// <summary>
    /// Центральная точка для всех живых врагов.
    ///
    /// Двигает толпу одним циклом (см. комментарий в Enemy — почему не Update на каждом),
    /// раздаёт врагов спавнеру и возвращает их в пул после смерти.
    ///
    /// Позже сюда же придёт flow field: вместо прямого вектора к цели
    /// направление будет браться из поля. Точка замены — GetDirection().
    /// </summary>
    public sealed class EnemyManager : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private EnemyPool pool;

        [Tooltip("Куда идут враги. Позже — Town Hall.")]
        [SerializeField] private Transform target;

        [Header("Поведение")]
        [Tooltip("Дистанция до цели, на которой враг считается дошедшим.")]
        [SerializeField] private float reachDistance = 2f;

        private readonly List<Enemy> _alive = new();

        /// <summary>Сколько врагов сейчас живо. Нужно волнам, чтобы понимать состояние боя.</summary>
        public int AliveCount => _alive.Count;

        /// <summary>Враг дошёл до цели — здесь потом будет урон по ратуше.</summary>
        public event System.Action<Enemy> ReachedTarget;

        /// <summary>Враг убит — здесь потом будет начисление золота.</summary>
        public event System.Action<Enemy> EnemyKilled;

        private void Update()
        {
            MoveAll();
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

            _alive.Remove(enemy);
            pool.Return(enemy);
        }

        /// <summary>Убрать всех врагов — например, между волнами или при рестарте.</summary>
        public void ClearAll()
        {
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                _alive[i].Died -= OnEnemyDied;
                pool.Return(_alive[i]);
            }

            _alive.Clear();
        }

        // ---------- Движение ----------

        private void MoveAll()
        {
            if (target == null)
                return;

            Vector3 goal = target.position;
            float deltaTime = Time.deltaTime;
            float reachSqr = reachDistance * reachDistance;

            // Идём с конца: дошедшие удаляются из списка прямо в цикле.
            for (int i = _alive.Count - 1; i >= 0; i--)
            {
                Enemy enemy = _alive[i];
                Vector3 toGoal = goal - enemy.transform.position;
                toGoal.y = 0f;

                if (toGoal.sqrMagnitude <= reachSqr)
                {
                    HandleReachedTarget(enemy);
                    continue;
                }

                MoveOne(enemy, toGoal.normalized, deltaTime);
            }
        }

        private static void MoveOne(Enemy enemy, Vector3 direction, float deltaTime)
        {
            Transform t = enemy.transform;

            t.position += direction * (enemy.MoveSpeed * deltaTime);
            t.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private void HandleReachedTarget(Enemy enemy)
        {
            ReachedTarget?.Invoke(enemy);
            Despawn(enemy);
        }
    }
}
