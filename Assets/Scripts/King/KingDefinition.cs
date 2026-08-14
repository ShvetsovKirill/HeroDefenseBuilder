using UnityEngine;

namespace HeroDefense.King
{
    /// <summary>
    /// Король как персонаж (D60): свои статы, оружие, портрет, модель.
    /// Новый король — новый ассет, без единой строчки кода.
    ///
    /// Все числа героя живут здесь, а не в [SerializeField] компонентов.
    /// Причина: прокачка (D66) меняет эти числа в рантайме, и если они
    /// зашиты в HeroMotor и AutoAttacker, каждый бонус потребует
    /// лезть в компоненты руками.
    /// </summary>
    [CreateAssetMenu(fileName = "King", menuName = "HeroDefense/King Definition")]
    public sealed class KingDefinition : ScriptableObject
    {
        [Header("Личность")]
        public string displayName = "Король Эрик";

        [TextArea(2, 4)]
        public string description;

        public Sprite portrait;

        [Header("Внешность")]
        [Tooltip("Префаб визуала: конь и седок отдельными объектами (D63). " +
                 "Логика к нему не привязана — меняется целиком.")]
        public GameObject visualPrefab;

        [Tooltip("Цвет коня. Пока единственная кастомизация (D63).")]
        public Color horseColor = new Color(0.45f, 0.32f, 0.22f);

        [Header("Движение")]
        public float moveSpeed = 6f;

        [Tooltip("Насколько быстро набирает и теряет скорость. " +
                 "Меньше — тяжелее и инертнее (D37).")]
        public float acceleration = 12f;

        [Tooltip("Скорость поворота корпуса, градусов в секунду.")]
        public float turnSpeedDeg = 540f;

        [Header("Бой")]
        public float attackDamage = 10f;

        [Tooltip("Выстрелов в секунду.")]
        public float fireRate = 3f;

        public float attackRange = 6f;

        [Header("Выбывание")]
        [Tooltip("Сколько урона выдерживает до развоплощения (D67). " +
                 "Это не HP в обычном смысле: король не умирает, " +
                 "он временно выбывает из игры.")]
        public float enduranceThreshold = 100f;

        [Tooltip("Сколько секунд длится фаза духа до возвращения к ратуше.")]
        public float respawnDelay = 10f;

        [Tooltip("Скорость полёта духом. Обычно как у живого или чуть выше.")]
        public float spiritMoveSpeed = 7f;
    }
}
