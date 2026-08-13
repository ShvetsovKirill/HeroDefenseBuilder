using System;
using UnityEngine;

namespace HeroDefense.Waves
{
    /// <summary>
    /// Группа спавна — минимальный кирпичик волны.
    /// «12 роевых из северной тропы, по 2 штуки каждые 0.4 сек, начать через 3 сек».
    /// </summary>
    [Serializable]
    public sealed class SpawnGroup
    {
        [Tooltip("Кого спавним.")]
        public EnemyDefinition enemy;

        [Min(1)]
        [Tooltip("Сколько всего врагов в группе.")]
        public int count = 6;

        [Tooltip("Индекс точки спавна. -1 = случайная. " +
                 "Так собираются атаки с одной, двух или трёх сторон.")]
        public int spawnPointIndex = -1;

        [Min(0f)]
        [Tooltip("Задержка перед началом группы, от старта волны.")]
        public float startDelay;

        [Min(0f)]
        [Tooltip("Пауза между отдельными врагами внутри группы. " +
                 "Маленькая — плотная колонна, большая — растянутая цепочка. " +
                 "Это главная ручка плотности.")]
        public float interval = 0.4f;

        [Min(1)]
        [Tooltip("Сколько врагов выходит за раз.")]
        public int burstSize = 1;

        /// <summary>Суммарный вес угрозы группы. Для превью.</summary>
        public float TotalThreat => enemy != null ? enemy.threatCost * count : 0f;

        /// <summary>Сколько секунд займёт выпуск всей группы.</summary>
        public float Duration
        {
            get
            {
                int bursts = Mathf.CeilToInt(count / (float)Mathf.Max(1, burstSize));

                return startDelay + Mathf.Max(0, bursts - 1) * interval;
            }
        }
    }

    /// <summary>
    /// Волна — набор групп, идущих параллельно со своими задержками.
    ///
    /// Из этого собирается что угодно: тихая волна с одной тропы,
    /// тройной охват, ложная атака с одной стороны и настоящая с другой
    /// через несколько секунд.
    /// </summary>
    [CreateAssetMenu(fileName = "Wave", menuName = "HeroDefense/Wave Definition")]
    public sealed class WaveDefinition : ScriptableObject
    {
        [Header("Описание")]
        public string displayName = "Волна";

        [Header("Состав")]
        public SpawnGroup[] groups = Array.Empty<SpawnGroup>();

        [Header("После волны")]
        [Min(0f)]
        [Tooltip("Окно между волнами: потратить золото, поставить здание, " +
                 "глянуть отряды. Короткое — иначе темп рассыпается.")]
        public float breakAfter = 6f;

        [Tooltip("Ждать ли уничтожения всех врагов перед следующей волной. " +
                 "Выключено — волны могут накладываться, давление растёт.")]
        public bool waitForClear = true;

        /// <summary>Сколько всего врагов в волне.</summary>
        public int TotalEnemies
        {
            get
            {
                int total = 0;

                foreach (SpawnGroup group in groups)
                    total += group.count;

                return total;
            }
        }

        /// <summary>Суммарный вес угрозы. Главное число для сравнения волн между собой.</summary>
        public float TotalThreat
        {
            get
            {
                float total = 0f;

                foreach (SpawnGroup group in groups)
                    total += group.TotalThreat;

                return total;
            }
        }

        /// <summary>Сколько секунд идёт выпуск волны.</summary>
        public float SpawnDuration
        {
            get
            {
                float longest = 0f;

                foreach (SpawnGroup group in groups)
                    longest = Mathf.Max(longest, group.Duration);

                return longest;
            }
        }

        /// <summary>Сколько разных направлений задействовано. Для превью.</summary>
        public int DirectionCount
        {
            get
            {
                var seen = new System.Collections.Generic.HashSet<int>();

                foreach (SpawnGroup group in groups)
                    seen.Add(group.spawnPointIndex);

                return seen.Count;
            }
        }
    }
}
