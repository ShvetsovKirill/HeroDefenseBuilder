namespace HeroDefense.Localization
{
    /// <summary>
    /// Языки, поддерживаемые игрой.
    ///
    /// Порядок важен: значения пишутся в сохранение числом, поэтому
    /// перестановка сменит язык у тех, кто уже играл. Новые языки
    /// добавляются только в конец.
    /// </summary>
    public enum Language
    {
        /// <summary>Английский. Язык по умолчанию.</summary>
        English = 0,

        /// <summary>Русский.</summary>
        Russian = 1
    }
}
