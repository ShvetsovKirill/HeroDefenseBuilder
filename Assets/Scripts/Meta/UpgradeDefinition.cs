using UnityEngine;

namespace HeroDefense.Meta
{
    /// <summary>
    /// Что именно поднимает ветка прокачки.
    ///
    /// Перечисление, а не строка: применяет бонус <c>UpgradeApplier</c>
    /// через switch, и опечатка в строке молча дала бы ветку, которая
    /// покупается, но ни на что не влияет — самый дорогой вид бага,
    /// потому что виден он только по ощущению от баланса.
    /// </summary>
    public enum UpgradeTarget
    {
        /// <summary>Плоская прибавка к <c>KingStat.AttackDamage</c>.</summary>
        KingDamage,

        /// <summary>Плоская прибавка к <c>KingStat.FireRate</c> (ударов в секунду).</summary>
        KingFireRate,

        /// <summary>Прибавка к максимальному HP каждого бойца отряда.</summary>
        SquadHealth,

        /// <summary>Прибавка к урону каждого бойца отряда.</summary>
        SquadDamage,

        /// <summary>Прибавка к золоту, с которым начинается забег.</summary>
        StartingGold,

        /// <summary>Прибавка к числу карточек в колоде застройки.</summary>
        DeckSize
    }

    /// <summary>
    /// Одна ветка постоянной прокачки: что улучшает, насколько за уровень,
    /// сколько уровней и почём (D92).
    ///
    /// Ассет описывает только правила. Купленный уровень лежит в
    /// <see cref="PlayerProgress"/>, бонус в бой заливает <c>UpgradeApplier</c>.
    /// Разделение не ради красоты: ScriptableObject в редакторе Unity
    /// изменяется НАВСЕГДА, включая изменения из играющей сцены — держи
    /// ассет прогресс в себе, и первый же тестовый забег переписал бы
    /// исходные числа без права отката.
    ///
    /// Цена растёт линейно: baseCost + costStep * (уровень - 1). Не
    /// геометрически — при пяти уровнях геометрия либо упирается в потолок
    /// на втором уровне, либо улетает за тысячу на пятом, а вилку 60-150
    /// престижа за покупку держать надо.
    /// </summary>
    [CreateAssetMenu(fileName = "Upgrade", menuName = "HeroDefense/Upgrade Definition")]
    public sealed class UpgradeDefinition : ScriptableObject
    {
        /// <summary>
        /// Идентификатор ветки размера колоды. Захардкожен в
        /// <see cref="RunLoadout"/>, поэтому ассет с целью
        /// <see cref="UpgradeTarget.DeckSize"/> обязан носить именно его.
        /// </summary>
        public const string DeckSizeId = "deck.size";

        // ---------- Опознание ----------

        [Header("Опознание")]
        [Tooltip("Ключ сохранения. Менять после релиза нельзя: сменишь — у всех игроков " +
                 "эта ветка обнулится, а потраченный престиж не вернётся. " +
                 "Формат: область.параметр, например king.damage.")]
        [SerializeField] private string id = "king.damage";

        [Tooltip("Заголовок карточки в замке. Видит игрок.")]
        [SerializeField] private string displayName = "Улучшение";

        [Tooltip("Строка под заголовком. Пиши эффект, а не флавор: игрок решает, " +
                 "куда деть 150 престижа, по этому тексту.")]
        [TextArea(2, 4)]
        [SerializeField] private string description = string.Empty;

        [Tooltip("Иконка карточки. Пусто — карточка покажется без картинки, это не ошибка.")]
        [SerializeField] private Sprite icon;

        // ---------- Что улучшает ----------

        [Header("Что улучшает")]
        [Tooltip("Куда UpgradeApplier зальёт бонус перед боем.")]
        [SerializeField] private UpgradeTarget target = UpgradeTarget.KingDamage;

        [Tooltip("Сколько добавляется за КАЖДЫЙ уровень, в единицах параметра. " +
                 "Для урона короля 2 означает +2 урона за уровень, а не +2%.")]
        [SerializeField] private float valuePerLevel = 2f;

        [Tooltip("Потолок ветки. Дальше карточка показывается как «максимум» и не покупается.")]
        [SerializeField] private int maxLevel = 5;

        // ---------- Цена ----------

        [Header("Цена в престиже")]
        [Tooltip("Сколько стоит первый уровень.")]
        [SerializeField] private int baseCost = 60;

        [Tooltip("На сколько дорожает каждый следующий уровень. " +
                 "Цена уровня N = baseCost + costStep * (N-1).")]
        [SerializeField] private int costStep = 20;

        // ---------- Чтение ----------

        /// <summary>Ключ сохранения этой ветки.</summary>
        public string Id => id;

        /// <summary>Заголовок карточки для экрана замка.</summary>
        public string DisplayName => displayName;

        /// <summary>Описание эффекта для экрана замка.</summary>
        public string Description => description;

        /// <summary>Иконка карточки, может быть пустой.</summary>
        public Sprite Icon => icon;

        /// <summary>Какой параметр поднимает эта ветка.</summary>
        public UpgradeTarget Target => target;

        /// <summary>Прибавка за один уровень, в единицах параметра.</summary>
        public float ValuePerLevel => valuePerLevel;

        /// <summary>Потолок ветки.</summary>
        public int MaxLevel => maxLevel;

        /// <summary>Сколько уровней уже куплено. Ноль — ветка не тронута.</summary>
        public int CurrentLevel => PlayerProgress.GetUpgradeLevel(id);

        /// <summary>Потолок достигнут, покупать больше нечего.</summary>
        public bool IsMaxed => CurrentLevel >= maxLevel;

        /// <summary>
        /// Цена следующего уровня. На максимуме — <c>int.MaxValue</c>,
        /// а не ноль: ноль означал бы «бесплатно», и кнопка покупки
        /// осталась бы активной.
        /// </summary>
        public int NextCost => IsMaxed ? int.MaxValue : GetCost(CurrentLevel + 1);

        /// <summary>Суммарный бонус на текущем уровне.</summary>
        public float CurrentBonus => GetBonus(CurrentLevel);

        /// <summary>Хватает ли престижа на следующий уровень.</summary>
        public bool CanBuy => !IsMaxed && PlayerProgress.CanAfford(NextCost);

        /// <summary>
        /// Цена указанного уровня. Нумерация с единицы:
        /// первый уровень стоит ровно baseCost.
        /// </summary>
        public int GetCost(int level)
        {
            if (level < 1)
                return 0;

            return baseCost + costStep * (level - 1);
        }

        /// <summary>Суммарный бонус на указанном уровне.</summary>
        public float GetBonus(int level) => valuePerLevel * Mathf.Max(0, level);

        /// <summary>
        /// Сколько престижа стоит выкачать ветку целиком.
        /// Для сверки баланса: сумма по всем веткам, делённая на добычу
        /// за забег, показывает, сколько забегов займёт полная прокачка.
        /// </summary>
        public int TotalCost
        {
            get
            {
                int sum = 0;

                for (int level = 1; level <= maxLevel; level++)
                    sum += GetCost(level);

                return sum;
            }
        }

        // ---------- Покупка ----------

        /// <summary>
        /// Списать престиж и поднять уровень. Возвращает false, если
        /// достигнут потолок или не хватает престижа.
        ///
        /// Покупка живёт здесь, а не в экране замка: списание и повышение
        /// уровня — две операции, и разведи их по разным местам, однажды
        /// пройдёт только одна. Экран вызывает один метод и сам ничего
        /// не считает.
        /// </summary>
        public bool TryBuy()
        {
            if (IsMaxed)
                return false;

            if (!PlayerProgress.TrySpendPrestige(NextCost))
                return false;

            PlayerProgress.RaiseUpgradeLevel(id);

            return true;
        }

        // ---------- Отображение ----------

        /// <summary>
        /// Бонус строкой для карточки: «+6 урона», «+2 карточки».
        /// Целочисленные параметры печатаются без дробной части —
        /// «+50,0 золота» в интерфейсе выглядит как ошибка.
        /// </summary>
        public string FormatBonus(float value)
        {
            bool integer = target == UpgradeTarget.StartingGold
                           || target == UpgradeTarget.DeckSize
                           || target == UpgradeTarget.SquadHealth;

            string number = integer
                ? Mathf.RoundToInt(value).ToString()
                : value.ToString("0.##");

            return value >= 0f ? "+" + number : number;
        }

        /// <summary>Сводка для отладки и подсказок.</summary>
        public string Describe()
        {
            string levels = IsMaxed
                ? $"{CurrentLevel}/{maxLevel} (максимум)"
                : $"{CurrentLevel}/{maxLevel}";

            string cost = IsMaxed ? "—" : NextCost.ToString();

            return $"{displayName}: уровень {levels}, сейчас {FormatBonus(CurrentBonus)}, " +
                   $"следующий за {cost} престижа";
        }

        // ---------- Проверки в редакторе ----------

        private void OnValidate()
        {
            // Значения ниже этих ломают арифметику цены и потолка,
            // а в инспекторе такое набирается случайно.
            maxLevel = Mathf.Max(1, maxLevel);
            baseCost = Mathf.Max(0, baseCost);
            costStep = Mathf.Max(0, costStep);

            id = string.IsNullOrWhiteSpace(id) ? string.Empty : id.Trim();

            // Ловушка, на которую иначе наступают молча: RunLoadout читает
            // размер колоды по жёсткой строке, и ассет с другим id будет
            // покупаться, но колоду не расширит.
            if (target == UpgradeTarget.DeckSize && id != DeckSizeId)
            {
                Debug.LogWarning($"[Прокачка] У ассета {name} цель DeckSize, но id «{id}». " +
                                 $"RunLoadout читает только «{DeckSizeId}» — ветка не сработает.", this);
            }
        }
    }
}
