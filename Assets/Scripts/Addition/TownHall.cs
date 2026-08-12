using System;
using UnityEngine;
using HeroDefense.Enemies;

namespace HeroDefense.Base
{
    /// <summary>
    /// Ратуша. Единственное условие поражения в игре (D2 — герой бессмертен).
    ///
    /// Слушает EnemyManager: враг, дошедший до цели, наносит урон и исчезает.
    /// Позже враги будут не исчезать, а атаковать ратушу стоя рядом —
    /// но для прототипа мгновенный урон достаточен и проще.
    /// </summary>
    public sealed class TownHall : MonoBehaviour
    {
        [Header("Здоровье")]
        [SerializeField] private float maxHealth = 1000f;

        [Tooltip("Урон от одного дошедшего врага. Позже возьмётся из данных врага.")]
        [SerializeField] private float damagePerEnemy = 10f;

        [Header("Ссылки")]
        [SerializeField] private EnemyManager enemyManager;

        /// <summary>Текущее и максимальное здоровье — для полоски и отладки.</summary>
        public float CurrentHealth { get; private set; }
        public float MaxHealth => maxHealth;
        public float HealthFraction => maxHealth > 0f ? CurrentHealth / maxHealth : 0f;
        public bool IsDestroyed => CurrentHealth <= 0f;

        /// <summary>Ратуша получила урон. Для VFX, тряски камеры, звука (D2 — обратная связь громкая).</summary>
        public event Action<float> Damaged;

        /// <summary>Ратуша разрушена — конец игры.</summary>
        public event Action Destroyed;

        private void Awake()
        {
            CurrentHealth = maxHealth;
        }

        private void OnEnable()
        {
            if (enemyManager != null)
                enemyManager.ReachedTarget += OnEnemyReached;
        }

        private void OnDisable()
        {
            if (enemyManager != null)
                enemyManager.ReachedTarget -= OnEnemyReached;
        }

        private void OnEnemyReached(Enemy enemy)
        {
            TakeDamage(damagePerEnemy);
        }

        public void TakeDamage(float amount)
        {
            if (IsDestroyed)
                return;

            CurrentHealth = Mathf.Max(0f, CurrentHealth - amount);
            Damaged?.Invoke(amount);

            if (CurrentHealth <= 0f)
                Destroyed?.Invoke();
        }

        /// <summary>Починка — понадобится для способности ремонта (эпик 2.4).</summary>
        public void Heal(float amount)
        {
            if (IsDestroyed)
                return;

            CurrentHealth = Mathf.Min(maxHealth, CurrentHealth + amount);
        }

        /// <summary>Полный сброс — для рестарта карты.</summary>
        public void ResetHealth()
        {
            CurrentHealth = maxHealth;
        }
    }
}
