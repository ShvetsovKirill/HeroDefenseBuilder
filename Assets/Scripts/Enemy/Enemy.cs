using System;
using UnityEngine;

namespace HeroDefense.Enemies
{
    /// <summary>
    /// Один враг. Держит состояние и умеет получать урон.
    ///
    /// ВАЖНО: у врага НЕТ своего Update. Движение выполняет EnemyManager
    /// одним циклом на всю толпу — при сотнях юнитов вызов Update на каждом
    /// стоит заметно дороже, чем один проход по массиву.
    /// </summary>
    public sealed class Enemy : MonoBehaviour
    {
        /// <summary>Вызывается при смерти. Аргумент — сам враг, чтобы менеджер вернул его в пул.</summary>
        public event Action<Enemy> Died;

        public float MoveSpeed { get; private set; }
        public bool IsAlive => _currentHealth > 0f;

        private float _maxHealth;
        private float _currentHealth;

        /// <summary>
        /// Подготовка врага к выходу из пула. Заменяет конструктор:
        /// объект переиспользуется, поэтому состояние сбрасывается здесь.
        /// </summary>
        public void Initialize(float maxHealth, float moveSpeed, Vector3 position)
        {
            _maxHealth = maxHealth;
            _currentHealth = maxHealth;
            MoveSpeed = moveSpeed;

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

        /// <summary>Убрать со сцены без события смерти — например, при очистке волны.</summary>
        public void Deactivate()
        {
            _currentHealth = 0f;
            gameObject.SetActive(false);
        }

        /// <summary>Доля оставшегося здоровья, 0..1. Понадобится для полоски HP позже.</summary>
        public float HealthFraction => _maxHealth > 0f ? _currentHealth / _maxHealth : 0f;
    }
}
