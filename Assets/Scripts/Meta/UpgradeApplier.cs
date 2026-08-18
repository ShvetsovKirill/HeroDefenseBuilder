using System.Text;
using UnityEngine;
using HeroDefense.Core;
using HeroDefense.Combat;
using HeroDefense.King;
using HeroDefense.Squads;
using KingCharacter = HeroDefense.King.King;

namespace HeroDefense.Meta
{
    /// <summary>
    /// Заливает купленную прокачку в забег (D92).
    ///
    /// Бонусы приходят из двух источников: уровень лежит в
    /// <see cref="PlayerProgress"/>, правила и числа — в ассетах
    /// <see cref="UpgradeDefinition"/>. Здесь только раздача.
    ///
    /// Раздаётся двумя способами, потому что цели рождаются в разное время:
    ///
    /// 1. Король и его статы существуют с начала сцены — им бонус
    ///    выдаётся напрямую через <see cref="KingStats"/>.
    /// 2. Бойцы и кошелёк спрашивают сами. Боец рождается на десятой волне,
    ///    когда раздавать уже поздно, а кошелёк считает стартовое золото
    ///    в своём Awake, и порядок Awake между объектами Unity не гарантирует.
    ///    Поэтому бонусы лежат в статических свойствах, и потребитель
    ///    забирает их в момент, когда ему удобно.
    ///
    /// Порядок выполнения -900: после <c>SceneContext</c> (-1000), чей
    /// <c>Current</c> нужен для поиска короля, но раньше всех, кто эти
    /// бонусы читает.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class UpgradeApplier : MonoBehaviour
    {
        [Header("Ветки прокачки")]
        [Tooltip("Все ассеты улучшений. Незаполненный список — не ошибка: " +
                 "забег просто пройдёт без бонусов, как до появления прокачки.")]
        [SerializeField] private UpgradeDefinition[] upgrades = new UpgradeDefinition[0];

        [Header("Отладка")]
        [Tooltip("Печатать в консоль, что именно применилось.")]
        [SerializeField] private bool logApplied = true;

        /// <summary>Прибавка к стартовому золоту. Читает <c>Wallet</c>.</summary>
        public static int StartingGoldBonus { get; private set; }

        /// <summary>Прибавка к максимальному HP бойца. Читает <c>Barracks</c> при спавне.</summary>
        public static float SquadHealthBonus { get; private set; }

        /// <summary>Прибавка к урону бойца. Читает <c>Barracks</c> при спавне.</summary>
        public static float SquadDamageBonus { get; private set; }

        /// <summary>
        /// Обнуление статики при запуске игры.
        ///
        /// Статические поля переживают перезагрузку сцены, а при выключенном
        /// Domain Reload — и выход из Play Mode. Без сброса забег без
        /// компонента в сцене унаследовал бы бонусы предыдущего.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            StartingGoldBonus = 0;
            SquadHealthBonus = 0f;
            SquadDamageBonus = 0f;
        }

        private void Awake()
        {
            Collect();
        }

        private void Start()
        {
            // Королю раздаём в Start, а не в Awake: KingStats создаётся
            // в Awake короля, и на -900 его ещё может не быть.
            ApplyToKing();
        }

        // ---------- Сбор ----------

        /// <summary>
        /// Посчитать бонусы по купленным уровням и разложить по свойствам.
        /// </summary>
        private void Collect()
        {
            ResetStatics();

            if (upgrades == null)
                return;

            foreach (UpgradeDefinition upgrade in upgrades)
            {
                if (upgrade == null)
                    continue;

                float bonus = upgrade.CurrentBonus;

                if (Mathf.Approximately(bonus, 0f))
                    continue;

                switch (upgrade.Target)
                {
                    case UpgradeTarget.StartingGold:
                        StartingGoldBonus += Mathf.RoundToInt(bonus);
                        break;

                    case UpgradeTarget.SquadHealth:
                        SquadHealthBonus += bonus;
                        break;

                    case UpgradeTarget.SquadDamage:
                        SquadDamageBonus += bonus;
                        break;

                    // Королевские ветки раздаются в ApplyToKing,
                    // размер колоды читает RunLoadout напрямую.
                }
            }
        }

        // ---------- Король ----------

        private void ApplyToKing()
        {
            KingCharacter king = FindKing();

            if (king == null || king.Stats == null)
            {
                if (logApplied)
                    Debug.LogWarning("[Прокачка] Король не найден — бонусы короля не применены.", this);

                return;
            }

            var applied = new StringBuilder();

            foreach (UpgradeDefinition upgrade in upgrades)
            {
                if (upgrade == null)
                    continue;

                float bonus = upgrade.CurrentBonus;

                if (Mathf.Approximately(bonus, 0f))
                    continue;

                switch (upgrade.Target)
                {
                    case UpgradeTarget.KingDamage:
                        king.Stats.AddFlat(KingStat.AttackDamage, bonus);
                        applied.Append($" урон +{bonus:0.##};");
                        break;

                    case UpgradeTarget.KingFireRate:
                        king.Stats.AddFlat(KingStat.FireRate, bonus);
                        applied.Append($" скорость атаки +{bonus:0.##};");
                        break;
                }
            }

            if (!logApplied)
                return;

            string kingPart = applied.Length > 0 ? applied.ToString() : " ничего;";

            Debug.Log($"[Прокачка] Королю:{kingPart} " +
                      $"Бойцам: HP +{SquadHealthBonus:0.##}, урон +{SquadDamageBonus:0.##}. " +
                      $"Стартовое золото +{StartingGoldBonus}.", this);
        }

        /// <summary>
        /// Король берётся из <c>SceneContext</c> — инвариант «общие системы
        /// только через контекст». Контекст хранит Transform, а не компонент,
        /// поэтому нужен GetComponent.
        /// </summary>
        private static KingCharacter FindKing()
        {
            SceneContext context = SceneContext.Current;

            if (context == null || context.King == null)
                return null;

            return context.King.GetComponent<KingCharacter>();
        }

        // ---------- Бойцы ----------

        /// <summary>
        /// Выдать бонусы только что рождённому бойцу.
        /// Вызывается из <c>Barracks</c> сразу после Instantiate.
        ///
        /// HP поднимается с долечиванием: боец выходит из казармы целым,
        /// а не с прежним количеством очков на увеличенной шкале.
        /// </summary>
        public static void ApplyToUnit(SquadUnit unit)
        {
            if (unit == null)
                return;

            if (SquadHealthBonus > 0f && unit.Health != null)
                unit.Health.SetMaxHealth(unit.Health.Max + SquadHealthBonus, refill: true);

            if (SquadDamageBonus > 0f)
            {
                var attacker = unit.GetComponent<AutoAttacker>();

                if (attacker != null)
                    attacker.AddDamage(SquadDamageBonus);
            }
        }

        // ---------- Отладка ----------

        /// <summary>Сводка применённого. Для отладочного HUD.</summary>
        public string Describe()
        {
            var text = new StringBuilder();

            text.AppendLine($"Стартовое золото: +{StartingGoldBonus}");
            text.AppendLine($"HP бойцов: +{SquadHealthBonus:0.##}");
            text.AppendLine($"Урон бойцов: +{SquadDamageBonus:0.##}");

            foreach (UpgradeDefinition upgrade in upgrades)
            {
                if (upgrade != null)
                    text.AppendLine(upgrade.Describe());
            }

            return text.ToString();
        }
    }
}
