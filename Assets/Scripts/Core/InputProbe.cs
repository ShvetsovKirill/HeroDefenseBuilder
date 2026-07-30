using UnityEngine;

namespace HeroDefense.Core
{
    /// <summary>
    /// ⚠️ ВРЕМЕННЫЙ ОТЛАДОЧНЫЙ КОМПОНЕНТ. УДАЛИТЬ ВМЕСТЕ С ПОЯВЛЕНИЕМ ГЕРОЯ.
    ///
    /// Существует ровно для одного: проверить, что проект запускается, ввод читается,
    /// а камера следует за целью. Это не герой и не его заготовка.
    /// </summary>
    [DisallowMultipleComponent]
    public class InputProbe : MonoBehaviour
    {
        [SerializeField] private InputReader inputReader;
        [SerializeField] private CameraRig cameraRig;
        [SerializeField, Min(0f)] private float speed = 8f;

        [Header("Диагностика")]
        [Tooltip("Печатать в консоль текущее направление ввода.")]
        [SerializeField] private bool logInput;

        private bool _reportedNotEnabled;

        private void Awake()
        {
            ValidateReferences();
        }

        private void Update()
        {
            if (!IsReadyToMove()) return;

            Vector2 input = inputReader.ReadMove();
            if (logInput) Debug.Log($"[InputProbe] Ввод: {input}");

            Move(input);
        }

        // ------------------------------------------------------------------

        private void Move(Vector2 input)
        {
            if (input.sqrMagnitude < 0.0001f) return;

            Vector3 direction = cameraRig.InputToWorld(input);

            // Диагональ с клавиатуры даёт длину √2 — без нормализации
            // движение по диагонали было бы быстрее прямого.
            if (direction.sqrMagnitude > 1f) direction.Normalize();

            transform.position += direction * (speed * Time.deltaTime);
        }

        // ------------------------------------------------------------------
        // Диагностика вынесена из Update: там должно остаться только действие.
        // ------------------------------------------------------------------

        private void ValidateReferences()
        {
            if (inputReader == null)
                Debug.LogError($"[InputProbe] На объекте '{name}' не назначен InputReader.", this);

            if (cameraRig == null)
                Debug.LogError($"[InputProbe] На объекте '{name}' не назначен CameraRig.", this);

            if (speed <= 0f)
                Debug.LogWarning($"[InputProbe] Скорость на '{name}' равна нулю.", this);
        }

        private bool IsReadyToMove()
        {
            if (inputReader == null || cameraRig == null) return false;
            if (inputReader.IsEnabled) return true;

            ReportNotEnabledOnce();
            return false;
        }

        private void ReportNotEnabledOnce()
        {
            if (_reportedNotEnabled) return;
            _reportedNotEnabled = true;

            Debug.LogError(
                "[InputProbe] InputReader не включён — ввод читаться не будет.\n" +
                "1) В сцене нет объекта с GameBootstrap, либо\n" +
                "2) в GameBootstrap назначен другой ассет InputReader.", this);
        }
    }
}
