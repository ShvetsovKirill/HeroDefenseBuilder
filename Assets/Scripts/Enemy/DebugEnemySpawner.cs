using UnityEngine;
using UnityEngine.InputSystem;
using HeroDefense.Enemies;

namespace HeroDefense.Debugging
{
    /// <summary>
    /// ВРЕМЕННЫЙ спавнер для проверки Enemy Core.
    /// Удаляется, как только подключим настоящую Wave System.
    ///
    /// Спавнит врагов пачками с трёх направлений (D1 — мультилейн),
    /// чтобы сразу видеть картинку, к которой идём.
    ///
    /// Клавиши:
    ///   Пробел — спавн пачки
    ///   K      — убить всех живых (проверка возврата в пул)
    ///   T      — включить/выключить автоспавн
    /// </summary>
    public sealed class DebugEnemySpawner : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private EnemyManager enemyManager;

        [Header("Точки входа")]
        [Tooltip("Направления, откуда приходят враги. Позже — тропы из леса.")]
        [SerializeField] private Transform[] spawnPoints;

        [Tooltip("Разброс вокруг точки спавна, чтобы враги не выходили в одну линию.")]
        [SerializeField] private float spawnSpread = 3f;

        [Header("Параметры врага")]
        [SerializeField] private float enemyHealth = 30f;
        [SerializeField] private float enemySpeed = 2.5f;

        [Header("Пачка")]
        [SerializeField] private int batchSize = 20;

        [Header("Автоспавн")]
        [SerializeField] private bool autoSpawn = true;
        [SerializeField] private float autoSpawnInterval = 1.5f;

        private float _timer;

        private void Update()
        {
            HandleHotkeys();
            HandleAutoSpawn();
        }

        private void HandleHotkeys()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null)
                return;

            if (kb.spaceKey.wasPressedThisFrame)
                SpawnBatch();

            if (kb.kKey.wasPressedThisFrame)
                enemyManager.ClearAll();

            if (kb.tKey.wasPressedThisFrame)
                autoSpawn = !autoSpawn;
        }

        private void HandleAutoSpawn()
        {
            if (!autoSpawn)
                return;

            _timer += Time.deltaTime;

            if (_timer < autoSpawnInterval)
                return;

            _timer = 0f;
            SpawnBatch();
        }

        private void SpawnBatch()
        {
            if (!HasValidSetup())
                return;

            for (int i = 0; i < batchSize; i++)
                enemyManager.Spawn(enemyHealth, enemySpeed, RandomSpawnPosition());
        }

        private bool HasValidSetup()
        {
            if (enemyManager == null)
            {
                Debug.LogError("[DebugSpawner] Не назначен EnemyManager.", this);
                enabled = false;
                return false;
            }

            if (spawnPoints == null || spawnPoints.Length == 0)
            {
                Debug.LogError("[DebugSpawner] Не назначены точки спавна.", this);
                enabled = false;
                return false;
            }

            return true;
        }

        private Vector3 RandomSpawnPosition()
        {
            Transform point = spawnPoints[Random.Range(0, spawnPoints.Length)];

            Vector2 offset = Random.insideUnitCircle * spawnSpread;

            return point.position + new Vector3(offset.x, 0f, offset.y);
        }
    }
}
