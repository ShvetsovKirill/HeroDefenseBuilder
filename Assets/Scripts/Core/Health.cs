using System;
using UnityEngine;

namespace HeroDefense.Core
{
    /// <summary>
    /// Здоровье. Один компонент на всё, что можно повредить:
    /// ратуша, башни, казармы, экономика (D40).
    ///
    /// Ратуша отличается от шахты не логикой урона, а только тем,
    /// что её разрушение заканчивает игру. Поэтому здесь нет ничего
    /// про поражение — это знание уровнем выше.
    /// </summary>
    public sealed class Health : MonoBehaviour
    {
        [SerializeField] private float maxHealth = 100f;

        public float Current { get; private set; }
        public float Max => maxHealth;
        public float Fraction => maxHealth > 0f ? Current / maxHealth : 0f;
        public bool IsAlive => Current > 0f;

        /// <summary>Получен урон. Аргумент — размер урона. Для VFX, тряски, звука.</summary>
        public event Action<float> Damaged;

        /// <summary>Здоровье кончилось.</summary>
        public event Action Died;

        private void Awake()
        {
            Current = maxHealth;
        }

        public void TakeDamage(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            Current = Mathf.Max(0f, Current - amount);
            Damaged?.Invoke(amount);

            if (Current <= 0f)
                Died?.Invoke();
        }

        public void Heal(float amount)
        {
            if (!IsAlive || amount <= 0f)
                return;

            Current = Mathf.Min(maxHealth, Current + amount);
        }

        /// <summary>Полный сброс — для рестарта и переиспользования из пула.</summary>
        public void ResetHealth()
        {
            Current = maxHealth;
        }

        /// <summary>Задать максимум извне — понадобится для апгрейдов построек.</summary>
        public void SetMaxHealth(float value, bool refill)
        {
            maxHealth = Mathf.Max(1f, value);

            if (refill)
                Current = maxHealth;
            else
                Current = Mathf.Min(Current, maxHealth);
        }
    }
}
