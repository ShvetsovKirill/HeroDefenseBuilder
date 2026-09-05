using UnityEngine;
using HeroDefense.Localization;

namespace HeroDefense.Waves
{
    /// <summary>
    /// Условие, которое игрок сам накладывает на следующую волну:
    /// врагов больше, но и золота больше (D125, разбор референсов).
    ///
    /// Зачем: волна, которую оборона держит сама, перестаёт быть приговором
    /// баланса и становится решением игрока — «мне сейчас нечего делать,
    /// значит беру потяжелее и зарабатываю». Скука лечится не числами
    /// в ассете уровня, а возможностью повысить ставку.
    ///
    /// Конструктор волн этим не трогается. Модификатор — множители ПОВЕРХ
    /// уже собранной волны, а не правка её данных: собранные уровни
    /// продолжают работать как были.
    /// </summary>
    [CreateAssetMenu(fileName = "WaveModifier", menuName = "HeroDefense/Условие волны")]
    public sealed class WaveModifier : ScriptableObject
    {
        [Header("Описание")]
        [Tooltip("Название на случай, если ключ перевода не проставлен.")]
        public string displayName = "Условие";

        [TextArea(2, 3)]
        [Tooltip("Что игрок получает и чем платит. Пиши последствие, а не флавор: " +
                 "выбор делается по этой строке за пару секунд.")]
        public string description;

        [Tooltip("Ключ названия в таблице переводов. Пусто — текст из поля выше.")]
        public string nameKey;

        [Tooltip("Ключ описания. Пусто — текст из поля описания.")]
        public string descriptionKey;

        [Tooltip("Иконка карточки. Необязательна.")]
        public Sprite icon;

        [Header("Сколько врагов")]
        [Min(0.1f)]
        [Tooltip("Множитель числа врагов в каждой группе. 1.5 — в полтора раза больше.")]
        public float enemyCount = 1f;

        [Min(0.1f)]
        [Tooltip("Множитель интервала между появлениями. МЕНЬШЕ единицы — плотнее.\n\n" +
                 "Главная ручка ощущения (D55): та же волна, пущенная вдвое плотнее, " +
                 "требует совсем другой обороны.")]
        public float spawnInterval = 1f;

        [Header("Какие враги")]
        [Min(0.1f)]
        [Tooltip("Множитель здоровья каждого врага.")]
        public float enemyHealth = 1f;

        [Min(0.1f)]
        [Tooltip("Множитель скорости. Быстрые враги проскакивают мимо башен " +
                 "и добегают до ратуши — это не то же самое, что просто крепкие.")]
        public float enemySpeed = 1f;

        [Header("Награда")]
        [Min(1f)]
        [Tooltip("Множитель золота за убийство. Плата за риск: без неё " +
                 "тяжёлое условие никто не возьмёт.")]
        public float goldReward = 1f;

        /// <summary>Название для игрока: перевод по ключу, иначе текст из ассета.</summary>
        public string DisplayName => Loc.GetOrFallback(nameKey, displayName);

        /// <summary>Описание для игрока.</summary>
        public string Description => Loc.GetOrFallback(descriptionKey, description);

        /// <summary>
        /// Грубая оценка тяжести: во сколько раз волна опаснее обычной.
        ///
        /// Нужна не для баланса, а для порядка карточек на экране: самое
        /// безобидное слева, самое злое справа. Игрок должен видеть шкалу,
        /// а не гадать, что из трёх страшнее.
        /// </summary>
        public float Danger => enemyCount * enemyHealth * enemySpeed / Mathf.Max(0.1f, spawnInterval);

        /// <summary>Ничего не меняет: волна идёт как задумана в ассете уровня.</summary>
        public bool IsNeutral =>
            Mathf.Approximately(enemyCount, 1f)
            && Mathf.Approximately(spawnInterval, 1f)
            && Mathf.Approximately(enemyHealth, 1f)
            && Mathf.Approximately(enemySpeed, 1f);
    }
}
