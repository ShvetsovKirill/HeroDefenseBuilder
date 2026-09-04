using System.Collections;
using UnityEngine;

namespace HeroDefense.App
{
    /// <summary>
    /// Точка входа в игру. Живёт на сцене Boot.
    ///
    /// Зачем отдельная сцена: сервисы, которые должны пережить все переходы
    /// (загрузчик сцен, настройки, сохранения), нужно создать один раз
    /// и до того, как игрок что-то увидит. Если делать это в главном меню,
    /// они пересоздадутся при каждом возврате туда.
    ///
    /// Сцена Boot должна быть первой в Build Settings.
    /// </summary>
    public sealed class AppBootstrap : MonoBehaviour
    {
        [Header("Сервисы")]
        [Tooltip("Префаб с SceneLoader и прочим, что живёт всю игру. " +
                 "Создаётся один раз и помечается DontDestroyOnLoad.")]
        [SerializeField] private GameObject persistentServicesPrefab;

        [Header("Настройки приложения")]
        [Tooltip("Ограничение кадров. -1 — без ограничения.")]
        [SerializeField] private int targetFrameRate = 60;

        [Tooltip("Не гасить экран во время игры.")]
        [SerializeField] private bool preventScreenSleep = true;

        [Header("Отладка")]
        [Tooltip("Задержка перед переходом в меню. Ноль — сразу. " +
                 "Полезна, чтобы увидеть логи инициализации.")]
        [SerializeField] private float delayBeforeMenu;

        private void Start()
        {
            ApplyApplicationSettings();
            CreateServices();

            StartCoroutine(GoToMenuRoutine());
        }

        private void ApplyApplicationSettings()
        {
            Application.targetFrameRate = targetFrameRate;

            Screen.sleepTimeout = preventScreenSleep
                ? SleepTimeout.NeverSleep
                : SleepTimeout.SystemSetting;
        }

        /// <summary>
        /// Создаём сервисы, если их ещё нет. Проверка нужна на случай
        /// повторного захода на Boot — иначе получим два загрузчика сцен.
        /// </summary>
        private void CreateServices()
        {
            if (SceneLoader.Instance != null)
                return;

            if (persistentServicesPrefab == null)
            {
                Debug.LogError("[AppBootstrap] Не назначен префаб сервисов.", this);
                return;
            }

            Instantiate(persistentServicesPrefab);
        }

        private IEnumerator GoToMenuRoutine()
        {
            if (delayBeforeMenu > 0f)
                yield return new WaitForSecondsRealtime(delayBeforeMenu);

            if (SceneLoader.Instance == null)
            {
                Debug.LogError("[AppBootstrap] SceneLoader не создан, переход невозможен.", this);
                yield break;
            }

            SceneLoader.Instance.GoToMainMenu();
        }
    }
}
