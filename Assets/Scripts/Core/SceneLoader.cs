using System;
using System.Collections;
using UnityEngine;
using UnityEngine.SceneManagement;

namespace HeroDefense.App
{
    /// <summary>
    /// Какие сцены есть в игре.
    ///
    /// Перечисление вместо строк: опечатка в названии сцены выясняется
    /// только в рантайме и выглядит как «ничего не произошло».
    /// </summary>
    public enum GameScene
    {
        /// <summary>Инициализация. Поднимает сервисы и уходит дальше.</summary>
        Boot,

        /// <summary>Главное меню: новая игра, настройки, выход.</summary>
        MainMenu,

        /// <summary>Замок: сборы перед забегом, прокачка, найм отрядов.</summary>
        Castle,

        /// <summary>Собственно бой.</summary>
        Battle
    }

    /// <summary>
    /// Загрузчик сцен. Живёт между сценами и знает, как из одной попасть
    /// в другую.
    ///
    /// Зачем отдельный класс: переходы разбросанные по кнопкам меню
    /// превращаются в паутину, где непонятно, кто куда ведёт. Здесь всё
    /// в одном месте, и добавить экран загрузки потом можно, не трогая
    /// вызывающий код.
    /// </summary>
    [DefaultExecutionOrder(-2000)]
    public sealed class SceneLoader : MonoBehaviour
    {
        [Header("Имена сцен")]
        [Tooltip("Должны совпадать с именами в Build Settings.")]
        [SerializeField] private string bootScene = "Boot";
        [SerializeField] private string mainMenuScene = "MainMenu";
        [SerializeField] private string castleScene = "Castle";
        [SerializeField] private string battleScene = "Battle";

        [Header("Переход")]
        [Tooltip("Минимальная длительность перехода. Без неё загрузка лёгкой " +
                 "сцены выглядит как мигание, и игрок не понимает, что произошло.")]
        [SerializeField] private float minimumTransitionTime = 0.4f;

        private static SceneLoader _instance;

        public static SceneLoader Instance => _instance;

        /// <summary>Загрузка началась. Для показа экрана перехода.</summary>
        public static event Action<GameScene> LoadStarted;

        /// <summary>Загрузка завершилась.</summary>
        public static event Action<GameScene> LoadCompleted;

        /// <summary>Прогресс загрузки, 0..1. Для полоски.</summary>
        public float Progress { get; private set; }

        public bool IsLoading { get; private set; }

        private void Awake()
        {
            if (_instance != null && _instance != this)
            {
                Destroy(gameObject);
                return;
            }

            _instance = this;
            DontDestroyOnLoad(gameObject);
        }

        private void OnDestroy()
        {
            if (_instance == this)
                _instance = null;
        }

        public void Load(GameScene scene)
        {
            if (IsLoading)
                return;

            StartCoroutine(LoadRoutine(scene));
        }

        private IEnumerator LoadRoutine(GameScene scene)
        {
            IsLoading = true;
            Progress = 0f;

            LoadStarted?.Invoke(scene);

            // Время всегда нормальное на переходе: если предыдущая сцена
            // ушла на паузе с timeScale = 0, корутина никогда не завершится.
            Time.timeScale = 1f;

            float startedAt = Time.unscaledTime;

            AsyncOperation operation = SceneManager.LoadSceneAsync(ResolveName(scene));

            // Держим сцену неактивированной, пока не пройдёт минимальное
            // время перехода — иначе мигнёт и всё.
            operation.allowSceneActivation = false;

            while (operation.progress < 0.9f)
            {
                Progress = operation.progress / 0.9f;
                yield return null;
            }

            Progress = 1f;

            float elapsed = Time.unscaledTime - startedAt;

            if (elapsed < minimumTransitionTime)
                yield return new WaitForSecondsRealtime(minimumTransitionTime - elapsed);

            operation.allowSceneActivation = true;

            yield return operation;

            IsLoading = false;
            LoadCompleted?.Invoke(scene);
        }

        private string ResolveName(GameScene scene)
        {
            return scene switch
            {
                GameScene.Boot => bootScene,
                GameScene.MainMenu => mainMenuScene,
                GameScene.Castle => castleScene,
                GameScene.Battle => battleScene,
                _ => mainMenuScene
            };
        }

        // ---------- Удобные обёртки ----------

        public void GoToMainMenu() => Load(GameScene.MainMenu);
        public void GoToCastle() => Load(GameScene.Castle);
        public void GoToBattle() => Load(GameScene.Battle);
    }
}
