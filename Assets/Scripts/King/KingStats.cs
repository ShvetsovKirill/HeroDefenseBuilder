using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroDefense.King
{
    /// <summary>
    /// Какой параметр модифицируется. Строки здесь были бы источником опечаток.
    /// </summary>
    public enum KingStat
    {
        MoveSpeed,
        Acceleration,
        TurnSpeed,
        AttackDamage,
        FireRate,
        AttackRange,
        Endurance,
        RespawnDelay
    }

    /// <summary>
    /// Действующие характеристики короля: база из KingDefinition
    /// плюс модификаторы прокачки.
    ///
    /// Зачем отдельный слой: прокачка бывает двух видов —
    /// постоянная между забегами и временная внутри забега (D66).
    /// Оба вида должны складываться, и ни один не должен портить
    /// исходный ассет: ScriptableObject в редакторе изменяется
    /// НАВСЕГДА, включая изменения из играющей сцены.
    ///
    /// Порядок применения: (база + плоские бонусы) * множители.
    /// Плоские первыми — иначе +5 к урону усиливался бы процентами,
    /// и порядок покупки апгрейдов начал бы влиять на результат.
    /// </summary>
    public sealed class KingStats
    {
        private readonly KingDefinition _definition;

        private readonly Dictionary<KingStat, float> _flatBonuses = new();
        private readonly Dictionary<KingStat, float> _multipliers = new();

        /// <summary>Характеристики пересчитаны — компоненты должны обновить кеш.</summary>
        public event Action Changed;

        public KingStats(KingDefinition definition)
        {
            _definition = definition != null
                ? definition
                : throw new ArgumentNullException(nameof(definition));
        }

        public KingDefinition Definition => _definition;

        // ---------- Чтение ----------

        public float MoveSpeed => Get(KingStat.MoveSpeed, _definition.moveSpeed);
        public float Acceleration => Get(KingStat.Acceleration, _definition.acceleration);
        public float TurnSpeedDeg => Get(KingStat.TurnSpeed, _definition.turnSpeedDeg);
        public float AttackDamage => Get(KingStat.AttackDamage, _definition.attackDamage);
        public float FireRate => Get(KingStat.FireRate, _definition.fireRate);
        public float AttackRange => Get(KingStat.AttackRange, _definition.attackRange);
        public float Endurance => Get(KingStat.Endurance, _definition.enduranceThreshold);
        public float RespawnDelay => Get(KingStat.RespawnDelay, _definition.respawnDelay);

        private float Get(KingStat stat, float baseValue)
        {
            float flat = _flatBonuses.TryGetValue(stat, out float f) ? f : 0f;
            float multiplier = _multipliers.TryGetValue(stat, out float m) ? m : 1f;

            return (baseValue + flat) * multiplier;
        }

        // ---------- Модификаторы ----------

        /// <summary>Плоская прибавка: +5 к урону.</summary>
        public void AddFlat(KingStat stat, float amount)
        {
            _flatBonuses.TryGetValue(stat, out float current);
            _flatBonuses[stat] = current + amount;

            Changed?.Invoke();
        }

        /// <summary>
        /// Множитель: 0.2 означает +20%.
        /// Множители складываются, а не перемножаются — иначе три бонуса
        /// по 50% дали бы не +150%, а +237%, и баланс поехал бы незаметно.
        /// </summary>
        public void AddMultiplier(KingStat stat, float fraction)
        {
            _multipliers.TryGetValue(stat, out float current);

            if (current <= 0f)
                current = 1f;

            _multipliers[stat] = current + fraction;

            Changed?.Invoke();
        }

        /// <summary>Сбросить всё временное — при старте нового забега.</summary>
        public void ResetModifiers()
        {
            _flatBonuses.Clear();
            _multipliers.Clear();

            Changed?.Invoke();
        }
    }
}
