using System;
using UnityEngine;
using HeroDefense.Core;

namespace HeroDefense.Enemies
{
    /// <summary>
    /// Один враг. Держит состояние, умеет получать урон и бить цель.
    ///
    /// ВАЖНО: у врага НЕТ своего Update. И движение, и тик атаки выполняет
    /// EnemyManager одним циклом на всю толпу — при сотнях юнитов вызов
    /// Update на каждом стоит заметно дороже, чем проход по списку.
    /// </summary>
    public sealed class Enemy : MonoBehaviour, HeroDefense.Visuals.IAnimatedActor
    {
        public enum State
        {
            /// <summary>Идёт к цели.</summary>
            Moving,

            /// <summary>Дошёл и бьёт цель. Больше не двигается.</summary>
            Attacking
        }

        /// <summary>Вызывается при смерти. Аргумент — сам враг, чтобы менеджер вернул его в пул.</summary>
        public event Action<Enemy> Died;

        public float MoveSpeed { get; private set; }
        public bool IsAlive => _currentHealth > 0f;

        /// <summary>
        /// Для анимации: идущий враг бежит, осаждающий стоит и бьёт.
        /// Точное значение не нужно — важен только переход idle/walk.
        /// </summary>
        public float NormalizedSpeed => CurrentState == State.Moving ? 1f : 0f;
        public State CurrentState { get; private set; }

        /// <summary>
        /// Поколение. Растёт при каждом выходе из пула.
        ///
        /// Зачем: тот, кто держит ссылку на врага (например, башня),
        /// должен уметь понять, что "его" враг умер, а объект уже переиспользован
        /// под другого. Сравнение поколений отвечает на это точно,
        /// в отличие от проверки живости.
        /// </summary>
        public int Version { get; private set; }

        /// <summary>Что враг сейчас бьёт. Null, пока идёт.</summary>
        public Health AttackTarget { get; private set; }

        /// <summary>
        /// Когда врагу следующий раз смотреть, что у него вокруг.
        /// Владеет этим EnemyManager: осмотр стоит два физических запроса,
        /// и делать его каждый кадр на каждом враге незачем.
        ///
        /// Поле живёт здесь, а не списком в менеджере, потому что список
        /// живых переставляется при смерти (последний переезжает на место
        /// убитого) — параллельный массив разъехался бы с ним.
        /// </summary>
        public float NextLookAt { get; set; }

        /// <summary>
        /// Из какого ассета сделан этот враг.
        ///
        /// Нужен, чтобы награда и вес угрозы брались из данных, а не были
        /// одинаковыми для всех: раньше Spawn принимал только HP и скорость,
        /// поэтому за бугая давали столько же, сколько за роевого.
        /// </summary>
        public HeroDefense.Waves.EnemyDefinition Definition { get; private set; }

        /// <summary>Золото за убийство. Из ассета, с запасным значением.</summary>
        public int GoldReward => Definition != null ? Definition.goldReward : 1;

        /// <summary>
        /// С какого расстояния враг атакует. Свойство типа, а не общая
        /// настройка: иначе лучник не отличался бы от мечника.
        /// </summary>
        public float AttackRange => Definition != null ? Definition.attackRange : 2.5f;

        /// <summary>
        /// Кто последним нанёс урон. Нужно для замеров: убийство
        /// записывается тому, чей выстрел оказался последним.
        ///
        /// Не идеально — добивший получает всё, — но для оценки
        /// «кто вообще воюет» точности достаточно.
        /// </summary>
        public HeroDefense.Diagnostics.DamageSource LastDamageSource { get; set; }

        [Header("Бой")]
        [SerializeField] private float attackDamage = 10f;

        [Tooltip("Ударов в секунду по постройке.")]
        [SerializeField] private float attackRate = 0.5f;

        [Tooltip("На каком расстоянии враг отвечает тому, кто его бьёт.\n\n" +
                 "Небольшой намеренно: на дальнобойных врагов не отвлекаемся, " +
                 "иначе король и башни стягивали бы на себя всю толпу через " +
                 "полкарты, и осада перестала бы существовать.")]
        [SerializeField] private float retaliationRadius = 2f;

        private float _maxHealth;
        private float _currentHealth;
        private float _attackCooldown;
        private HeroDefense.Visuals.ActorAnimator _animator;

        private Health _ownHealth;

        private void Awake()
        {
            _animator = GetComponent<HeroDefense.Visuals.ActorAnimator>();

            // Своё здоровье нужно как источник урона: боец, которого
            // подстрелили, должен знать, кто это сделал.
            _ownHealth = GetComponent<Health>();
        }

        /// <summary>
        /// Подготовка врага к выходу из пула. Заменяет конструктор:
        /// объект переиспользуется, поэтому всё состояние сбрасывается здесь.
        /// </summary>
        public void Initialize(
            HeroDefense.Waves.EnemyDefinition definition,
            float maxHealth,
            float moveSpeed,
            Vector3 position)
        {
            Definition = definition;

            _maxHealth = maxHealth;
            _currentHealth = maxHealth;
            MoveSpeed = moveSpeed;

            CurrentState = State.Moving;
            AttackTarget = null;
            _attackCooldown = 0f;
            LastDamageSource = HeroDefense.Diagnostics.DamageSource.Unknown;

            Version++;

            transform.position = position;
            gameObject.SetActive(true);

            // Сброс обязателен: объект пришёл из пула, и без него
            // новый враг появился бы уже мёртвым.
            if (_animator != null)
                _animator.ResetState();
        }

        /// <summary>
        /// Получить урон. Источник нужен для ответной реакции: враг,
        /// осаждающий постройку, должен отвлечься на того, кто бьёт его
        /// вплотную — иначе он стоит и молча умирает, что выглядит тупо.
        ///
        /// Источник может быть null: башня или король бьют издалека,
        /// и отвлекаться на них враг не должен — иначе вся толпа
        /// разворачивалась бы к королю через полкарты.
        /// </summary>
        public void TakeDamage(float amount, Health attacker = null)
        {
            TryRetaliate(attacker);

            if (!IsAlive)
                return;

            _currentHealth -= amount;

            if (_currentHealth <= 0f)
                Die();
        }

        /// <summary>
        /// Переключиться на атакующего, если он рядом.
        ///
        /// Только вблизи: дальнобойного обидчика игнорируем. Иначе лучники
        /// стягивали бы на себя всю толпу, а осада перестала бы работать.
        /// Заодно это делает разницу между типами отрядов осмысленной —
        /// мечники держат врагов на себе, лучники бьют безнаказанно.
        /// </summary>
        private void TryRetaliate(Health attacker)
        {
            if (attacker == null || !attacker.IsAlive)
                return;

            // Уже бьём этого — незачем пересчитывать.
            if (AttackTarget == attacker)
                return;

            Vector3 delta = attacker.transform.position - transform.position;
            delta.y = 0f;

            if (delta.sqrMagnitude > retaliationRadius * retaliationRadius)
                return;

            BeginAttacking(attacker);
        }

        private void Die()
        {
            _currentHealth = 0f;
            Died?.Invoke(this);
        }

        // ---------- Осада ----------

        /// <summary>
        /// Перейти к атаке цели. Враг останавливается здесь
        /// пока не убьют его или цель.
        /// </summary>
        public void BeginAttacking(Health target)
        {
            AttackTarget = target;
            CurrentState = State.Attacking;

            // Первый удар не мгновенный: иначе вся подошедшая толпа
            // бьёт одновременно в один кадр и урон выглядит как один скачок.
            _attackCooldown = UnityEngine.Random.Range(0f, AttackInterval);
        }

        /// <summary>
        /// Тик атаки. Вызывается менеджером.
        /// Возвращает false, если цель пропала — значит враг должен идти дальше.
        /// </summary>
        public bool TickAttack(float deltaTime)
        {
            if (AttackTarget == null || !AttackTarget.IsAlive)
            {
                AttackTarget = null;
                CurrentState = State.Moving;

                return false;
            }

            _attackCooldown -= deltaTime;

            if (_attackCooldown > 0f)
                return true;

            if (_animator != null)
                _animator.PlayAttack();

            DeliverDamage();
            _attackCooldown = AttackInterval;

            return true;
        }

        /// <summary>
        /// Снаряд или удар вплотную — зависит от типа врага.
        /// У мечников префаб не задан, и урон наносится сразу.
        /// </summary>
        private void DeliverDamage()
        {
            if (TryLaunchProjectile())
                return;

            AttackTarget.TakeDamage(attackDamage);
        }

        private bool TryLaunchProjectile()
        {
            HeroDefense.Combat.Projectile prefab = Definition != null
                ? Definition.projectilePrefab
                : null;

            if (prefab == null || HeroDefense.Combat.ProjectilePool.Current == null)
                return false;

            HeroDefense.Combat.ProjectilePool.Current.LaunchAtHealth(
                prefab,
                transform.position + Vector3.up * 0.6f,
                AttackTarget,
                attackDamage,
                _ownHealth);

            return true;
        }

        private float AttackInterval => 1f / Mathf.Max(0.01f, attackRate);

        /// <summary>Убрать со сцены без события смерти — при очистке волны.</summary>
        public void Deactivate()
        {
            _currentHealth = 0f;
            AttackTarget = null;
            gameObject.SetActive(false);
        }

        /// <summary>Доля оставшегося здоровья, 0..1. Для полоски HP позже.</summary>
        public float HealthFraction => _maxHealth > 0f ? _currentHealth / _maxHealth : 0f;
    }
}
