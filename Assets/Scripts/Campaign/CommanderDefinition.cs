using UnityEngine;

namespace HeroDefense.Campaign
{
    /// <summary>
    /// Командир: человек, ради которого игрок идёт в тяжёлое баронство
    /// и о потере которого потом жалеет.
    ///
    /// ScriptableObject, а не сгенерированная запись: у командира есть
    /// портрет и характер, и это ровно то, что превращает отряд из строки
    /// в списке в «копейщиков сэра Родерика». Генерировать такое из букв
    /// можно, но тогда терять их не жалко.
    ///
    /// Бонусы намеренно заметные, а не проценты: если разница между
    /// милицией и именным отрядом не читается в бою, вся система
    /// командиров превращается в украшение.
    /// </summary>
    [CreateAssetMenu(fileName = "Commander", menuName = "HeroDefense/Командир")]
    public sealed class CommanderDefinition : ScriptableObject
    {
        [Header("Личность")]
        [Tooltip("Имя, которое увидит игрок: «сэр Родерик».")]
        public string commanderName = "сэр Родерик";

        [TextArea(2, 3)]
        [Tooltip("Пара строк о человеке. Читается при назначении в лагере.")]
        public string description;

        [Tooltip("Портрет для лагеря и панели отрядов.")]
        public Sprite portrait;

        [Header("Что даёт отряду")]
        [Min(0f)]
        [Tooltip("Прибавка к урону каждого бойца отряда, в единицах урона.")]
        public float damageBonus = 3f;

        [Min(0f)]
        [Tooltip("Прибавка к здоровью каждого бойца.")]
        public float healthBonus = 20f;

        [Min(0f)]
        [Tooltip("Прибавка к скорости отряда.")]
        public float moveSpeedBonus = 0.5f;

        [Header("Ограничения")]
        [Tooltip("Специализации, которые этот командир умеет вести. " +
                 "Пусто — умеет любую.\n\n" +
                 "Ограничение делает выбор командира решением: маг не станет " +
                 "копейщиком, и кого куда поставить — уже задача.")]
        public SquadSpecialization[] allowedSpecializations;

        /// <summary>Может ли этот командир вести такой отряд.</summary>
        public bool CanLead(SquadSpecialization specialization)
        {
            if (specialization == SquadSpecialization.Militia)
                return false;

            if (allowedSpecializations == null || allowedSpecializations.Length == 0)
                return true;

            foreach (SquadSpecialization allowed in allowedSpecializations)
            {
                if (allowed == specialization)
                    return true;
            }

            return false;
        }
    }
}
