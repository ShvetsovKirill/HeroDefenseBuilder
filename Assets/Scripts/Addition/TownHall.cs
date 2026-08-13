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
    [RequireComponent(typeof(Health))]
    public sealed class TownHall : MonoBehaviour
    {
        [Header("Пассивный доход")]
        [Tooltip("Сколько золота приносит ратуша за один тик (D45). " +
                 "Это нижний порог дохода, чтобы игрок не застревал в нуле.")]
        [SerializeField] private int goldPerTick = 2;

        [Tooltip("Интервал между начислениями, секунды.")]
        [SerializeField] private float incomeInterval = 15f;

        private Health _health;
        private float _incomeTimer;

        public Health Health => _health;

        /// <summary>Ратуша разрушена — конец игры.</summary>
        public event Action Destroyed;

        /// <summary>Пассивный доход начислен. Аргумент — сумма.</summary>
        public event Action<int> IncomeGenerated;

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

        private void Update()
        {
            TickIncome(Time.deltaTime);
        }

        /// <summary>
        /// Пассивный доход идёт, даже когда всё остальное разрушено (D44).
        /// Это предохранитель от спирали поражения: потерял постройки —
        /// всё ещё есть на что отстроиться.
        /// </summary>
        private void TickIncome(float deltaTime)
        {
            if (!_health.IsAlive)
                return;

            _incomeTimer += deltaTime;

            if (_incomeTimer < incomeInterval)
                return;

            _incomeTimer = 0f;
            IncomeGenerated?.Invoke(goldPerTick);
        }

        private void OnHealthDied()
        {
            Destroyed?.Invoke();
        }
    }
}
