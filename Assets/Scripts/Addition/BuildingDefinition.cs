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
        [Tooltip("Для сортировки карточек и будущих ограничений на слоты. " +
                 "Сейчас все типы идут в любой слот (D4 — единый пул).")]
        public BuildingCategory category = BuildingCategory.Tower;
    }

    public enum BuildingCategory
    {
        Tower,
        Economy,
        Military
    }
}
