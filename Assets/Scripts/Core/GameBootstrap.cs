using UnityEngine;

namespace HeroDefense.Core
{
    /// <summary>
    /// Composition Root. Единственная точка входа сцены: здесь и только здесь
    /// системы связываются друг с другом. Ничто в проекте не должно искать зависимости
    /// через FindObjectOfType или синглтоны — всё приходит отсюда.
    ///
    /// В Epic 0 связывать почти нечего. Класс существует, чтобы порядок инициализации
    /// был явным с самого начала и не пришлось вводить его задним числом.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    [DisallowMultipleComponent]
    public class GameBootstrap : MonoBehaviour
    {
        [Header("Services")]
        [SerializeField] private InputReader inputReader;
        [SerializeField] private CameraRig cameraRig;

        [Header("Scene")]
        [Tooltip("За чем следит камера на старте. Позже сюда встанет герой, пока — placeholder.")]
        [SerializeField] private Transform initialCameraTarget;

        [Header("Профилирование")]
        [Tooltip("ВКЛЮЧАТЬ ТОЛЬКО НА ВРЕМЯ ЗАМЕРОВ В EPIC 1.\n\n" +
                 "Снимает ограничение частоты кадров и разрешает работу в фоне — без этого " +
                 "не видно, есть ли запас производительности.\n\n" +
                 "В релизной сборке ОБЯЗАТЕЛЬНО выключить: Яндекс Игры требуют, чтобы при " +
                 "сворачивании страницы игра и звук останавливались.")]
        [SerializeField] private bool profilingMode;

        private void Awake()
        {
            ApplyFrameSettings();

            if (inputReader == null)
                Debug.LogError("[GameBootstrap] InputReader не назначен — ввода не будет.", this);
            else
                inputReader.Enable();

            if (cameraRig == null)
                Debug.LogError("[GameBootstrap] CameraRig не назначен.", this);
            else if (initialCameraTarget != null)
                cameraRig.SetTarget(initialCameraTarget);
        }

        private void ApplyFrameSettings()
        {
            // vSync маскирует реальную производительность: кадр всегда «успевает» к развёртке.
            QualitySettings.vSyncCount = 0;

            if (profilingMode)
            {
                Application.targetFrameRate = -1;
                Application.runInBackground = true;
            }
            else
            {
                Application.targetFrameRate = 60;

                // Требование площадки: свёрнутая вкладка не должна продолжать играть и шуметь.
                // Дублирует галку Player Settings → Resolution and Presentation → Run In Background,
                // которая тоже должна быть снята.
                Application.runInBackground = false;
            }
        }

        private void OnDestroy()
        {
            if (inputReader != null) inputReader.Disable();
        }
    }
}
