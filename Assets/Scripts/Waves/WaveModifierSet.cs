using System;
using UnityEngine;

namespace HeroDefense.Waves
{
    /// <summary>
    /// Набор условий, из которых игроку предлагается выбор между волнами.
    ///
    /// Отдельный ассет, а не список в <see cref="LevelDefinition"/>: условия
    /// общие для всех карт, и держать их копию в каждом уровне значило бы
    /// править десять ассетов ради одной правки баланса.
    ///
    /// Лежит в Resources под именем, которое ждёт <see cref="WaveModifiers"/>.
    /// Нет ассета — выбора между волнами просто не будет, всё остальное
    /// работает как раньше.
    /// </summary>
    [CreateAssetMenu(fileName = "WaveModifierSet", menuName = "HeroDefense/Набор условий волн")]
    public sealed class WaveModifierSet : ScriptableObject
    {
        [Tooltip("Все условия игры. Из них случайно набираются варианты для показа.")]
        public WaveModifier[] modifiers = Array.Empty<WaveModifier>();

        [Min(2)]
        [Tooltip("Сколько вариантов показывать. Три — предел, на котором выбор " +
                 "делается за пару секунд; больше превращает перерыв в чтение.")]
        public int choicesPerWave = 3;

        [Min(1)]
        [Tooltip("С какой волны начинать предлагать условия.\n\n" +
                 "Не с первой: игрок ещё не знает, что такое обычная волна, " +
                 "и не может оценить, во что ввязывается.")]
        public int firstWave = 3;

        [Tooltip("Предлагать ли выбор перед каждой волной. Выключено — только " +
                 "по кратным номерам, см. поле ниже.")]
        public bool everyWave = true;

        [Min(1)]
        [Tooltip("Если выбор не перед каждой волной — предлагать раз во столько волн.")]
        public int wavePeriod = 3;

        /// <summary>Предлагать ли выбор перед волной с этим номером.</summary>
        public bool ShouldOffer(int waveNumber)
        {
            if (modifiers == null || modifiers.Length == 0 || waveNumber < firstWave)
                return false;

            return everyWave || (waveNumber - firstWave) % Mathf.Max(1, wavePeriod) == 0;
        }
    }
}
