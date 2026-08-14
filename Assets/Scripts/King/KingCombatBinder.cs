using UnityEngine;
using HeroDefense.Combat;

namespace HeroDefense.King
{
    /// <summary>
    /// Связывает характеристики короля с его оружием.
    ///
    /// Зачем отдельный компонент, а не поле в AutoAttacker: тот же
    /// AutoAttacker висит на башнях и бойцах отряда, которым про короля
    /// знать незачем. Мостик ставится только на короля.
    ///
    /// Когда появится система оружия (D61, шаг 5), этот компонент станет
    /// местом, где выбирается конкретная реализация выстрела.
    /// </summary>
    [RequireComponent(typeof(King))]
    [RequireComponent(typeof(AutoAttacker))]
    public sealed class KingCombatBinder : MonoBehaviour
    {
        private King _king;
        private AutoAttacker _attacker;

        private void Awake()
        {
            _king = GetComponent<King>();
            _attacker = GetComponent<AutoAttacker>();
        }

        private void OnEnable()
        {
            _king.StatsChanged += ApplyStats;
            ApplyStats();
        }

        private void OnDisable()
        {
            _king.StatsChanged -= ApplyStats;
        }

        /// <summary>
        /// Переливаем действующие числа в стрелка.
        /// Вызывается при старте и после каждого апгрейда.
        /// </summary>
        private void ApplyStats()
        {
            KingStats stats = _king.Stats;

            if (stats == null)
                return;

            _attacker.Configure(stats.AttackDamage, stats.FireRate, stats.AttackRange);
        }
    }
}
