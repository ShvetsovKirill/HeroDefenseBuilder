namespace HeroDefense.Audio
{
    /// <summary>
    /// Звуковые события игры.
    ///
    /// Перечисление, а не строки: опечатка в строке молча даёт тишину,
    /// и найти её можно только на слух. Здесь же несуществующий звук
    /// не компилируется.
    ///
    /// Значения в сохранение не пишутся, поэтому порядок менять можно.
    /// </summary>
    public enum SoundId
    {
        /// <summary>Пусто. Значение по умолчанию: поле, которое забыли заполнить, молчит.</summary>
        None = 0,

        // ---------- Бой ----------

        /// <summary>Удар короля.</summary>
        KingAttack,

        /// <summary>Удар бойца отряда.</summary>
        UnitAttack,

        /// <summary>Выстрел башни.</summary>
        TowerShot,

        /// <summary>Враг умер.</summary>
        EnemyDeath,

        /// <summary>Боец отряда умер.</summary>
        UnitDeath,

        /// <summary>Враги начали ломать ратушу. Тревога.</summary>
        TownHallAlarm,

        // ---------- Волны ----------

        /// <summary>Началась новая волна.</summary>
        WaveStart,

        /// <summary>Волна перебита.</summary>
        WaveCleared,

        // ---------- Строительство и экономика ----------

        /// <summary>Постройка поставлена.</summary>
        BuildingPlaced,

        /// <summary>Покупка не прошла: не хватает золота или лимит исчерпан.</summary>
        BuildRejected,

        /// <summary>Отряд пополнился новобранцами.</summary>
        SquadReinforced,

        /// <summary>Флаг переставлен.</summary>
        FlagPlanted,

        // ---------- Итог забега ----------

        /// <summary>Ратуша пала.</summary>
        Defeat,

        /// <summary>Все волны пройдены.</summary>
        Victory,

        // ---------- Интерфейс ----------

        /// <summary>Нажатие на кнопку.</summary>
        UiClick,

        /// <summary>Куплено улучшение в замке.</summary>
        UpgradePurchased
    }
}
