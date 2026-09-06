namespace HeroDefense.Campaign
{
    /// <summary>
    /// Во что превращается отряд, когда им начинает командовать человек.
    ///
    /// Специализация выбирается один раз — в момент назначения командира.
    /// До этого отряд остаётся милицией: он воюет, но ничем не выделяется,
    /// и терять его не жалко. В этом и смысл — разница между «ополчение»
    /// и «копейщики сэра Родерика» должна ощущаться.
    ///
    /// Значения пишутся в сохранение числом: порядок не менять,
    /// новые специализации только в конец.
    /// </summary>
    public enum SquadSpecialization
    {
        /// <summary>Милиция: базовое оружие, командира нет.</summary>
        Militia = 0,

        /// <summary>Копейщики: держат строй, хороши против натиска.</summary>
        Spearmen = 1,

        /// <summary>Мечники: наступательный отряд ближнего боя.</summary>
        Swordsmen = 2,

        /// <summary>Лучники: бьют издали, беспомощны вблизи.</summary>
        Archers = 3,

        /// <summary>
        /// Боевой маг. Отряд из одного человека: ему нужна не казарма,
        /// а магическая башня.
        /// </summary>
        BattleMage = 4,

        /// <summary>Маг-лекарь. Тоже один человек, но лечит своих.</summary>
        HealerMage = 5
    }

    /// <summary>Помощники по специализациям — чтобы условия не расползались по коду.</summary>
    public static class SpecializationRules
    {
        /// <summary>Отряд из одного человека, которому нужна магическая башня.</summary>
        public static bool IsMage(this SquadSpecialization specialization)
        {
            return specialization is SquadSpecialization.BattleMage
                or SquadSpecialization.HealerMage;
        }

        /// <summary>Есть ли у отряда командир. Милиция — единственный случай, когда нет.</summary>
        public static bool HasCommander(this SquadSpecialization specialization)
        {
            return specialization != SquadSpecialization.Militia;
        }
    }
}
