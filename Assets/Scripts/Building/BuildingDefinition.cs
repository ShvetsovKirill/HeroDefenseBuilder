using UnityEngine;
using HeroDefense.Localization;

namespace HeroDefense.Building
{
    /// <summary>
    /// Описание одного типа постройки.
    ///
    /// ScriptableObject, а не класс в коде: башни, экономика и казармы —
    /// это контент, который будет меняться десятки раз при балансировке.
    /// Дизайнер должен крутить цифры без пересборки проекта.
    /// </summary>
    [CreateAssetMenu(fileName = "Building", menuName = "HeroDefense/Building Definition")]
    public sealed class BuildingDefinition : ScriptableObject
    {
        [Header("Описание")]
        [Tooltip("Название на случай, если ключ перевода не проставлен. " +
                 "Служит и подписью ассета в списках редактора.")]
        public string displayName = "Башня";

        [TextArea(2, 4)]
        [Tooltip("Короткая подсказка на карточке: чем эта постройка полезна.")]
        public string description;

        [Header("Перевод")]
        [Tooltip("Ключ названия в таблице переводов, например building.tower.name.\n\n" +
                 "Пусто — покажется текст из поля выше. Так недопереведённая " +
                 "постройка остаётся читаемой, а не превращается в голый ключ.")]
        public string nameKey;

        [Tooltip("Ключ описания. Пусто — покажется текст из поля описания.")]
        public string descriptionKey;

        [Header("Вид")]
        [Tooltip("Иконка для карточки в панели строительства.")]
        public Sprite icon;

        [Header("Стоимость")]
        [Tooltip("Цена в золоте. По правилу баланса выражается в долях " +
                 "среднего дохода за волну: башня ≈ 2–3 дохода.")]
        public int cost = 50;

        [Header("Что ставится")]
        public GameObject prefab;

        [Header("Подсказка")]
        [Tooltip("Короткая строка под ценой: «+10 / 20 сек» или «12 урона».\n\n" +
                 "Заполняется вручную: считать её из префаба означало бы, " +
                 "что карточка знает про начинку зданий.")]
        public string statLine;

        [Header("Категория")]
        [Tooltip("Для сортировки карточек. Все типы идут в любой слот (D4 — единый пул).")]
        public BuildingCategory category = BuildingCategory.Tower;

        [Header("Лимит")]
        [Min(1)]
        [Tooltip("Сколько таких построек можно иметь за забег (D86).\n\n" +
                 "Зачем: без лимита единственным ограничением остаётся золото — " +
                 "раскрутил экономику и застроил всё, конфликт «жадность против " +
                 "безопасности» исчезает. С лимитом выбор остаётся даже с полным кошельком.\n\n" +
                 "ВАЖНО: сумма лимитов по всем типам должна быть БОЛЬШЕ числа слотов. " +
                 "Иначе игрок просто строит всё разрешённое и выбора нет.\n\n" +
                 "Казармы: не больше 4 — по числу флагов (D87).")]
        public int maxCount = 4;

        [Tooltip("Может ли лимит расти от чертежей из сундуков (D88). " +
                 "Для казарм выключено: больше 4 отрядов нечем командовать.")]
        public bool allowBlueprints = true;

        [Header("Карточка (D121)")]
        [Tooltip("Открыт ли этот тип с самого начала. Базовая башня, ферма " +
                 "и казарма — да, иначе первый забег играть нечем.")]
        public bool unlockedFromStart;

        [Min(0)]
        [Tooltip("Сколько престижа стоит открыть тип НАВСЕГДА (D121).\n\n" +
                 "Открытие, а не покупка расходника: иначе каждый забег " +
                 "начинался бы с восстановления колоды, и престиж стал бы " +
                 "оброком вместо прогресса.")]
        public int unlockCost = 120;

        // ---------- Тексты для игрока ----------

        /// <summary>Название для игрока: перевод по ключу, иначе текст из ассета.</summary>
        public string DisplayName => Loc.GetOrFallback(nameKey, displayName);

        /// <summary>Описание для игрока. Может быть пустым — это не ошибка.</summary>
        public string Description => Loc.GetOrFallback(descriptionKey, description);
    }

    public enum BuildingCategory
    {
        Tower,
        Economy,
        Military
    }
}
