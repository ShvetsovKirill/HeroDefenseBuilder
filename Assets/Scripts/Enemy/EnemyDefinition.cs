using UnityEngine;

namespace HeroDefense.Waves
{
    /// <summary>
    /// Тип врага. Три базовых архетипа: swarm, bruiser, ranged.
    ///
    /// Всё, что отличает одного врага от другого, живёт здесь —
    /// в коде нет ни одного зашитого числа про конкретный тип.
    /// </summary>
    [CreateAssetMenu(fileName = "Enemy", menuName = "HeroDefense/Enemy Definition")]
    public sealed class EnemyDefinition : ScriptableObject
    {
        [Header("Описание")]
        public string displayName = "Бандит";

        [Header("Характеристики")]
        public float maxHealth = 40f;
        public float moveSpeed = 2.5f;

        [Header("Награда")]
        [Tooltip("Золото за убийство. Чем опаснее враг, тем больше.")]
        public int goldReward = 2;

        [Header("Вес угрозы")]
        [Tooltip("Условная стоимость врага для оценки волны. " +
                 "Нужна для превью и автогенерации: 10 роевых и 2 бугая " +
                 "могут весить одинаково.")]
        public float threatCost = 1f;

        [Header("Внешний вид")]
        [Tooltip("Префаб с компонентом Enemy. Если пусто — берётся префаб из пула.")]
        public GameObject prefabOverride;
    }
}
