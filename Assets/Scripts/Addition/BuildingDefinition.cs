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

        [Header("Стоимость")]
        [Tooltip("Цена постройки в золоте. По правилу баланса выражается " +
                 "в долях среднего дохода за волну: башня ≈ 2–3 дохода.")]
        public int cost = 50;

        [Header("Что ставится")]
        public GameObject prefab;
    }
}
