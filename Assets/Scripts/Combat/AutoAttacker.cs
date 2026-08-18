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

        [Tooltip("Может ли враг ответить этому стрелку.\n\n" +
                 "Включено у бойцов отряда: они держат врагов на себе, " +
                 "и это их роль (затычка). Выключено у башен и короля — " +
                 "иначе толпа разворачивалась бы к ним через полкарты " +
                 "и переставала бы осаждать что-либо.")]
        [SerializeField] private bool provokeRetaliation;

        [Header("Статистика")]
        [Tooltip("Кем считать этот источник урона в замерах баланса.\n\n" +
                 "Король / Башня / Отряд — по этим категориям потом видно, " +
                 "работает ли эскалация: если король убивает больше половины, " +
                 "башни и отряды остались декорацией.")]
        [SerializeField] private HeroDefense.Diagnostics.DamageSource statsSource
            = HeroDefense.Diagnostics.DamageSource.Unknown;

        [Header("Снаряд")]
        [Tooltip("Префаб снаряда. Пусто — урон мгновенный (хитскан).\n\n" +
                 "Снаряд нужен там, где важно видеть, кто в кого стреляет: " +
                 "при десятках юнитов хитскан читается как «все умирают сами».")]
        [SerializeField] private Projectile projectilePrefab;

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

        /// <summary>
        /// Прибавить к урону. Зовёт UpgradeApplier при рождении бойца:
        /// у бойцов, в отличие от короля, нет своего слоя статов —
        /// числа лежат прямо на префабе, и прокачке некуда их положить,
        /// кроме как в уже созданный компонент.
        /// </summary>
        public void AddDamage(float amount)
        {
            if (amount <= 0f)
                return;

            damage += amount;
        }

        private Health _ownHealth;
        private HeroDefense.Visuals.ActorAnimator _animator;

        private void Awake()
        {
            _ownHealth = GetComponentInParent<Health>();
            _animator = GetComponentInParent<HeroDefense.Visuals.ActorAnimator>();

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

        /// <summary>
        /// Должен ли враг отвечать этому стрелку. Включают бойцы отряда,
        /// башни и король оставляют выключенным.
        /// </summary>
        public void SetProvokeRetaliation(bool value)
        {
            provokeRetaliation = value;
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
            // Анимация дёргается всегда: даже если урон мгновенный,
            // замах должен быть виден.
            if (_animator != null)
                _animator.PlayAttack();

            // Источник передаём, только если этот стрелок должен провоцировать
            // ответ: башни и король бьют «безнаказанно» осознанно.
            Health source = provokeRetaliation ? _ownHealth : null;

            HeroDefense.Diagnostics.BattleStats.RegisterDamage(statsSource, damage);

            // Помечаем врага: когда он умрёт, убийство запишется на нас.
            if (_target != null)
                _target.LastDamageSource = statsSource;

            if (TryLaunchProjectile(source))
                return;

            _target.TakeDamage(damage, source);
            ShowShotLine();
        }

        /// <summary>
        /// Выпустить снаряд, если он задан и пул существует.
        ///
        /// Возвращает false, когда снаряда нет — тогда работает хитскан.
        /// Так один компонент обслуживает и мечника (мгновенный удар),
        /// и лучника (летящая стрела), и башню.
        /// </summary>
        private bool TryLaunchProjectile(Health source)
        {
            if (projectilePrefab == null || ProjectilePool.Current == null)
                return false;

            ProjectilePool.Current.Launch(
                projectilePrefab, MuzzlePosition, _target, damage, source);

            return true;
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
