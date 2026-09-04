using System;
using UnityEngine;
using HeroDefense.Core;

namespace HeroDefense.Base
{
    /// <summary>
    /// Ратуша. Единственное условие поражения в игре (D2 — герой бессмертен).
    ///
    /// Вся логика урона живёт в Health — ратуша отличается от шахты
    /// не механикой, а только последствиями разрушения (D40).
    /// Этот компонент нужен, чтобы врагам и системам было за что зацепиться,
    /// и чтобы отличить "критическое здание" от обычного.
    ///
    /// Урон наносят сами враги, подойдя вплотную (осада) — ратуша
    /// ничего не знает о том, кто её бьёт.
    /// </summary>
    /// <remarks>
    /// Пассивный доход переехал в отдельный компонент GoldIncome:
    /// он нужен и экономическим зданиям, а копировать логику в каждое —
    /// значит чинить её потом в двух местах.
    /// </remarks>
    [RequireComponent(typeof(Health))]
    public sealed class TownHall : MonoBehaviour
    {
        private Health _health;

        public Health Health => _health;

        /// <summary>Ратуша разрушена — конец игры.</summary>
        public event Action Destroyed;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            _health.Died += OnHealthDied;
        }

        private void OnDisable()
        {
            _health.Died -= OnHealthDied;
        }

        private void OnHealthDied()
        {
            Destroyed?.Invoke();
        }
    }
}
