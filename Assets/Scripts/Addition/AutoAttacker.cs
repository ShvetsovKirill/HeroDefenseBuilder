using UnityEngine;
using HeroDefense.Enemies;

namespace HeroDefense.Combat
{
    /// <summary>
    /// Автоматическая атака по ближайшему врагу.
    ///
    /// Один компонент и для героя, и для башен — логика у них одинаковая,
    /// отличаются только числа. Это заодно значит, что апгрейды урона
    /// и скорострельности позже будут работать для обоих одинаково.
    ///
    /// Урон мгновенный (хитскан). Снаряды с полётом добавим, когда станет
    /// понятно, нужны ли они для читаемости — пока это лишние объекты в сцене.
    /// </summary>
    public sealed class AutoAttacker : MonoBehaviour
    {
        [Header("Параметры атаки")]
        [SerializeField] private float damage = 10f;

        [Tooltip("Выстрелов в секунду.")]
        [SerializeField] private float fireRate = 3f;

        [SerializeField] private float range = 10f;

        [Header("Поиск цели")]
        [Tooltip("Как часто искать новую цель, в секундах. " +
                 "Каждый кадр не нужно: перебор по сотням врагов стоит дорого, " +
                 "а цель не меняется настолько часто.")]
        [SerializeField] private float retargetInterval = 0.2f;

        [Header("Визуал")]
        [Tooltip("Откуда идёт выстрел. Пусто = центр объекта.")]
        [SerializeField] private Transform muzzle;

        [Tooltip("Линия выстрела. Необязательно — без неё стрельба просто невидима.")]
        [SerializeField] private LineRenderer shotLine;

        [SerializeField] private float shotLineDuration = 0.05f;

        [Header("Ссылки")]
        [SerializeField] private EnemyManager enemyManager;

        private Enemy _target;

        /// <summary>
        /// Поколение цели на момент захвата.
        ///
        /// Враг живёт в пуле: умер — вернулся — выдан заново уже как другой юнит.
        /// Ссылка при этом остаётся валидной, и без сверки поколений
        /// мы продолжили бы стрелять "в того же врага", который на деле новый.
        /// </summary>
        private int _targetVersion;

        private float _retargetTimer;
        private float _cooldownTimer;
        private float _shotLineTimer;

        /// <summary>Текущая цель — пригодится для поворота модели в сторону стрельбы.</summary>
        public Enemy CurrentTarget => _target;

        private void Awake()
        {
            if (enemyManager == null)
                enemyManager = FindFirstObjectByType<EnemyManager>();

            if (shotLine != null)
                shotLine.enabled = false;
        }

        private void Update()
        {
            if (enemyManager == null)
                return;

            UpdateTarget();
            UpdateFiring();
            UpdateShotLine();
        }

        // ---------- Цель ----------

        private void UpdateTarget()
        {
            _retargetTimer -= Time.deltaTime;

            if (IsTargetValid() && _retargetTimer > 0f)
                return;

            _retargetTimer = retargetInterval;
            _target = enemyManager.FindNearest(transform.position, range);
            _targetVersion = _target != null ? _target.Version : 0;
        }

        private bool IsTargetValid()
        {
            if (_target == null || !_target.IsAlive)
                return false;

            // Объект тот же, но из пула его уже выдали под другого врага.
            if (_target.Version != _targetVersion)
                return false;

            Vector3 delta = _target.transform.position - transform.position;
            delta.y = 0f;

            return delta.sqrMagnitude <= range * range;
        }

        // ---------- Стрельба ----------

        private void UpdateFiring()
        {
            _cooldownTimer -= Time.deltaTime;

            if (_cooldownTimer > 0f || !IsTargetValid())
                return;

            Fire();
            _cooldownTimer = 1f / Mathf.Max(0.01f, fireRate);
        }

        private void Fire()
        {
            _target.TakeDamage(damage);
            ShowShotLine();
        }

        // ---------- Визуал выстрела ----------

        private void ShowShotLine()
        {
            if (shotLine == null || _target == null)
                return;

            shotLine.enabled = true;
            shotLine.SetPosition(0, MuzzlePosition);
            shotLine.SetPosition(1, _target.transform.position + Vector3.up * 0.5f);

            _shotLineTimer = shotLineDuration;
        }

        private void UpdateShotLine()
        {
            if (shotLine == null || !shotLine.enabled)
                return;

            _shotLineTimer -= Time.deltaTime;

            if (_shotLineTimer <= 0f)
                shotLine.enabled = false;
        }

        private Vector3 MuzzlePosition => muzzle != null ? muzzle.position : transform.position;
    }
}
