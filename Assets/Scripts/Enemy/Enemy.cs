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
    public sealed class Enemy : MonoBehaviour
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

        [Header("Бой")]
        [SerializeField] private float attackDamage = 10f;

        [Tooltip("Ударов в секунду по постройке.")]
        [SerializeField] private float attackRate = 0.5f;

        private float _maxHealth;
        private float _currentHealth;
        private float _attackCooldown;

        /// <summary>
        /// Подготовка врага к выходу из пула. Заменяет конструктор:
        /// объект переиспользуется, поэтому всё состояние сбрасывается здесь.
        /// </summary>
        public void Initialize(float maxHealth, float moveSpeed, Vector3 position)
        {
            _maxHealth = maxHealth;
            _currentHealth = maxHealth;
            MoveSpeed = moveSpeed;

            CurrentState = State.Moving;
            AttackTarget = null;
            _attackCooldown = 0f;

            Version++;

            transform.position = position;
            gameObject.SetActive(true);
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive)
                return;

            _currentHealth -= amount;

            if (_currentHealth <= 0f)
                Die();
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

            AttackTarget.TakeDamage(attackDamage);
            _attackCooldown = AttackInterval;

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
