using UnityEngine;
using HeroDefense.Core;
using HeroDefense.Enemies;

namespace HeroDefense.Combat
{
    /// <summary>
    /// Автоматическая атака по ближайшему врагу.
    ///
    /// Один компонент и для героя, и для башен, и для бойцов отряда —
    /// логика одинаковая, отличаются только числа. Значит и апгрейды урона
    /// со скорострельностью будут работать для всех одинаково.
    ///
    /// Два режима поиска цели:
    ///   • сам ищет ближайшего (герой, башни);
    ///   • получает цель извне через SetTargetProvider (бойцы отряда).
    /// Второй нужен, чтобы боец не бежал к одному врагу, стреляя в другого.
    ///
    /// Урон мгновенный (хитскан). Снаряды с полётом добавим, если окажется,
    /// что без них хуже читается — пока это лишние объекты в сцене.
    /// </summary>
    public sealed class AutoAttacker : MonoBehaviour
    {
        [Header("Параметры атаки")]
        [SerializeField] private float damage = 10f;

        [Tooltip("Выстрелов в секунду.")]
        [SerializeField] private float fireRate = 3f;

        [SerializeField] private float range = 10f;

        [Header("Поиск цели")]
        [Tooltip("Как часто искать новую цель. Каждый кадр не нужно: " +
                 "перебор стоит дорого, а цель меняется редко.")]
        [SerializeField] private float retargetInterval = 0.2f;

        [Tooltip("Искать цель самостоятельно. Герой и башни — да. " +
                 "Боец отряда — нет: SquadUnit отключает это в Awake, " +
                 "чтобы юнит не бежал к одному врагу, стреляя в другого.")]
        [SerializeField] private bool searchOwnTarget = true;

        [Header("Визуал")]
        [Tooltip("Откуда идёт выстрел. Пусто = центр объекта.")]
        [SerializeField] private Transform muzzle;

        [Tooltip("Линия выстрела. Необязательно — без неё стрельба невидима.")]
        [SerializeField] private LineRenderer shotLine;

        [SerializeField] private float shotLineDuration = 0.05f;

        private Enemy _target;

        /// <summary>
        /// Поколение цели на момент захвата.
        ///
        /// Враг живёт в пуле: умер — вернулся — выдан заново другим юнитом.
        /// Ссылка остаётся валидной, поэтому без сверки поколений мы бы
        /// продолжили стрелять «в того же врага», который на деле новый.
        /// </summary>
        private int _targetVersion;

        private float _retargetTimer;
        private float _cooldownTimer;
        private float _shotLineTimer;

        /// <summary>Текущая цель — для поворота модели в сторону стрельбы.</summary>
        public Enemy CurrentTarget => _target;

        /// <summary>Дальность — нужна снаружи, например для отрисовки радиуса башни.</summary>
        public float Range => range;

        /// <summary>
        /// Задать параметры извне. Используется KingCombatBinder:
        /// у короля числа живут в KingStats, потому что их меняет прокачка.
        /// Башни и бойцы отряда настраиваются полями в инспекторе и это не зовут.
        /// </summary>
        public void Configure(float newDamage, float newFireRate, float newRange)
        {
            damage = newDamage;
            fireRate = Mathf.Max(0.01f, newFireRate);
            range = Mathf.Max(0f, newRange);
        }

        private void Awake()
        {
            if (shotLine != null)
                shotLine.enabled = false;
        }

        /// <summary>
        /// Перевести в режим внешнего управления целью.
        /// Вызывает SquadUnit: боец сам решает, кого бить,
        /// чтобы движение и стрельба смотрели в одну сторону.
        /// </summary>
        public void TakeTargetControl()
        {
            searchOwnTarget = false;
            _target = null;
            _targetVersion = 0;
        }

        /// <summary>Назначить цель извне. Работает только после TakeTargetControl.</summary>
        public void SetTarget(Enemy target)
        {
            _target = target;
            _targetVersion = target != null ? target.Version : 0;
        }

        private void Update()
        {
            if (!IsGameRunning)
                return;

            if (searchOwnTarget)
                UpdateOwnTarget();

            UpdateFiring();
            UpdateShotLine();
        }

        private static bool IsGameRunning =>
            GameState.Current == null || GameState.Current.IsPlaying;

        // ---------- Цель ----------

        private void UpdateOwnTarget()
        {
            _retargetTimer -= Time.deltaTime;

            if (IsTargetValid() && _retargetTimer > 0f)
                return;

            _retargetTimer = retargetInterval;

            EnemyManager manager = SceneContext.Current != null
                ? SceneContext.Current.EnemyManager
                : null;

            if (manager == null)
                return;

            _target = manager.FindNearest(transform.position, range);
            _targetVersion = _target != null ? _target.Version : 0;
        }

        private bool IsTargetValid()
        {
            if (_target == null || !_target.IsAlive)
                return false;

            // Объект тот же, но из пула его выдали под другого врага.
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
