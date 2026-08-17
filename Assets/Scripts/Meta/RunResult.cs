using System.Collections.Generic;

namespace HeroDefense.Meta
{
    /// <summary>
    /// Итог последнего забега. Мост между боем и замком (D91).
    ///
    /// Сцена Battle выгружается целиком, поэтому передать результат
    /// объектом невозможно — он умрёт вместе со сценой. Статическое
    /// хранилище переживает переход, как PlayerProgress.
    ///
    /// Здесь только слепок для показа. Начисление уже произошло в бою:
    /// престиж копится по ходу и не сгорает при поражении (D118), поэтому
    /// экран итогов ничего не начисляет, а лишь показывает, что случилось.
    /// </summary>
    public static class RunResult
    {
        /// <summary>Есть ли что показывать. Ложь — в замок зашли не из боя.</summary>
        public static bool HasResult { get; private set; }

        /// <summary>Дошёл ли игрок до конца забега.</summary>
        public static bool Victory { get; private set; }

        /// <summary>На какой волне всё закончилось.</summary>
        public static int WaveReached { get; private set; }

        /// <summary>Сколько престижа принёс забег.</summary>
        public static int PrestigeEarned { get; private set; }

        /// <summary>За что именно начислено — строками для экрана итогов.</summary>
        public static IReadOnlyList<string> Breakdown => Lines;

        private static readonly List<string> Lines = new();

        public static void Store(bool victory, int waveReached, int prestige, IEnumerable<string> breakdown)
        {
            Victory = victory;
            WaveReached = waveReached;
            PrestigeEarned = prestige;

            Lines.Clear();

            if (breakdown != null)
                Lines.AddRange(breakdown);

            HasResult = true;
        }

        /// <summary>
        /// Забрать результат и забыть. Вызывается экраном итогов: иначе
        /// он всплывал бы снова при каждом заходе в замок.
        /// </summary>
        public static void Consume()
        {
            HasResult = false;
        }

        public static void Clear()
        {
            HasResult = false;
            Victory = false;
            WaveReached = 0;
            PrestigeEarned = 0;

            Lines.Clear();
        }
    }
}
