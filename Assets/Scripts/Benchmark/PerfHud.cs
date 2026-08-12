using UnityEngine;

namespace HeroDefense.Benchmark
{
    /// <summary>
    /// Экранный счётчик для замеров. Специально на OnGUI —
    /// это не продакшн-код, а инструмент, и он не должен тянуть за собой uGUI.
    ///
    /// ВАЖНО: смотреть цифры только в браузерном билде.
    /// Редактор врёт в разы — там свои накладные расходы и другой рендер-путь.
    /// </summary>
    public sealed class PerfHud : MonoBehaviour
    {
        [Tooltip("Окно усреднения FPS в секундах.")]
        [SerializeField] private float sampleWindow = 0.5f;

        [Tooltip("Через сколько секунд после старта начинать считать худший кадр. " +
                 "Первые кадры всегда просадочные из-за прогрева.")]
        [SerializeField] private float warmupTime = 2f;

        private float _accumulatedTime;
        private int _frameCount;
        private float _displayFps;
        private float _worstFrameMs;
        private float _elapsed;

        /// <summary>Сколько агентов сейчас в сцене. Проставляется извне.</summary>
        public int AgentCount { get; set; }

        private void Update()
        {
            _elapsed += Time.unscaledDeltaTime;

            UpdateAverageFps();
            UpdateWorstFrame();
        }

        private void UpdateAverageFps()
        {
            _accumulatedTime += Time.unscaledDeltaTime;
            _frameCount++;

            if (_accumulatedTime < sampleWindow)
                return;

            _displayFps = _frameCount / _accumulatedTime;
            _accumulatedTime = 0f;
            _frameCount = 0;
        }

        private void UpdateWorstFrame()
        {
            if (_elapsed < warmupTime)
                return;

            float frameMs = Time.unscaledDeltaTime * 1000f;

            if (frameMs > _worstFrameMs)
                _worstFrameMs = frameMs;
        }

        /// <summary>Сбросить худший кадр — вызывать после смены количества агентов.</summary>
        public void ResetWorstFrame()
        {
            _worstFrameMs = 0f;
            _elapsed = 0f;
        }

        private void OnGUI()
        {
            const int padding = 10;
            const int lineHeight = 26;

            var style = new GUIStyle(GUI.skin.label)
            {
                fontSize = 20,
                normal = { textColor = Color.white }
            };

            GUI.Box(new Rect(padding, padding, 260, lineHeight * 3 + padding), GUIContent.none);

            GUI.Label(new Rect(padding * 2, padding, 250, lineHeight),
                $"FPS: {_displayFps:F1}", style);

            GUI.Label(new Rect(padding * 2, padding + lineHeight, 250, lineHeight),
                $"Худший кадр: {_worstFrameMs:F1} мс", style);

            GUI.Label(new Rect(padding * 2, padding + lineHeight * 2, 250, lineHeight),
                $"Агентов: {AgentCount}", style);
        }
    }
}
