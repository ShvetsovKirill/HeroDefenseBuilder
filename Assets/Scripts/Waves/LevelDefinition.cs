using UnityEngine;

namespace HeroDefense.Waves
{
    /// <summary>
    /// Карта — последовательность волн. По плану: 5–8 волн, 5–6 минут.
    ///
    /// Дизайнер собирает наполнение здесь, не трогая код:
    /// порядок волн, стартовая пауза, финальный босс.
    /// </summary>
    [CreateAssetMenu(fileName = "Level", menuName = "HeroDefense/Level Definition")]
    public sealed class LevelDefinition : ScriptableObject
    {
        [Header("Описание")]
        public string displayName = "Поляна";

        [Header("Волны")]
        [Tooltip("Идут по порядку сверху вниз.")]
        public WaveDefinition[] waves = new WaveDefinition[0];

        [Header("Тайминги")]
        [Min(0f)]
        [Tooltip("Пауза перед первой волной: осмотреться и поставить первое здание.")]
        public float startDelay = 10f;

        [Header("Экономика")]
        [Tooltip("Стартовое золото на этой карте.")]
        public int startingGold = 100;

        /// <summary>Сводка для дизайнера. Показывается кнопкой превью в инспекторе.</summary>
        public string BuildPreview()
        {
            if (waves == null || waves.Length == 0)
                return "Волны не заданы.";

            var text = new System.Text.StringBuilder();

            text.AppendLine($"=== {displayName} ===");
            text.AppendLine($"Волн: {waves.Length}, стартовая пауза {startDelay:F0} сек");
            text.AppendLine();

            int totalEnemies = 0;
            float totalThreat = 0f;
            float totalTime = startDelay;

            for (int i = 0; i < waves.Length; i++)
            {
                WaveDefinition wave = waves[i];

                if (wave == null)
                {
                    text.AppendLine($"{i + 1,2}. — пусто —");
                    continue;
                }

                totalEnemies += wave.TotalEnemies;
                totalThreat += wave.TotalThreat;
                totalTime += wave.SpawnDuration + wave.breakAfter;

                text.AppendLine(
                    $"{i + 1,2}. {wave.displayName,-16} " +
                    $"врагов {wave.TotalEnemies,3} | " +
                    $"угроза {wave.TotalThreat,6:F1} | " +
                    $"направлений {wave.DirectionCount} | " +
                    $"выпуск {wave.SpawnDuration,4:F1}с | " +
                    $"пауза {wave.breakAfter,3:F0}с");
            }

            text.AppendLine();
            text.AppendLine($"Итого врагов: {totalEnemies}");
            text.AppendLine($"Итого угрозы: {totalThreat:F1}");
            text.AppendLine($"Примерная длительность: {totalTime / 60f:F1} мин");

            return text.ToString();
        }
    }
}
