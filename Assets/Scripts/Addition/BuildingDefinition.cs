using UnityEngine;

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
        public string displayName = "Башня";

        [TextArea(2, 4)]
        [Tooltip("Короткая подсказка на карточке: чем эта постройка полезна.")]
        public string description;

        [Tooltip("Иконка для карточки в панели строительства.")]
        public Sprite icon;

        [Header("Стоимость")]
        [Tooltip("Цена в золоте. По правилу баланса выражается в долях " +
                 "среднего дохода за волну: башня ≈ 2–3 дохода.")]
        public int cost = 50;

        [Header("Что ставится")]
        public GameObject prefab;

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
    }

    public enum BuildingCategory
    {
        Tower,
        Economy,
        Military
    }
}
