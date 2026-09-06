using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HeroDefense.App;
using HeroDefense.Core;
using HeroDefense.Localization;

namespace HeroDefense.UI
{
    /// <summary>
    /// Экран паузы: что нажимать и как выйти из забега.
    ///
    /// До него пауза была невидимой — время просто останавливалось, и игрок
    /// не понимал, игра замерла или сломалась. Здесь же лежит и список
    /// управления: пауза — единственный момент, когда его читают спокойно.
    ///
    /// Панель собирается кодом (см. <see cref="RuntimeUi"/>) и создаётся
    /// сама на каждой боевой карте: вёрстка в сцене потребовала бы
    /// повторения на каждой новой карте.
    /// </summary>
    [DefaultExecutionOrder(-600)]
    public sealed class PauseScreen : MonoBehaviour
    {
        /// <summary>Поверх HUD, но ниже возможного экрана загрузки.</summary>
        private const int SortingOrder = 500;

        private GameState _state;
        private GameObject _root;
        private Transform _controlsRoot;

        /// <summary>
        /// Создаётся при загрузке боевой карты. Признак карты — наличие
        /// <see cref="GameState"/>: паузу нечего показывать там, где нет партии.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single || GameState.Current == null)
                return;

            var host = new GameObject("PauseScreen");

            host.AddComponent<PauseScreen>();
        }

        private void Start()
        {
            _state = GameState.Current;

            if (_state == null)
            {
                Destroy(gameObject);
                return;
            }

            Build();
            _state.PhaseChanged += OnPhaseChanged;
            Loc.LanguageChanged += OnLanguageChanged;

            Hide();
        }

        private void OnDestroy()
        {
            if (_state != null)
                _state.PhaseChanged -= OnPhaseChanged;

            Loc.LanguageChanged -= OnLanguageChanged;
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            if (phase == GamePhase.Paused)
                Show();
            else
                Hide();
        }

        private void OnLanguageChanged(Language language)
        {
            // Панель переживает смену языка прямо на паузе: игрок может
            // открыть настройки, переключить язык и вернуться.
            //
            // Список пересобирается целиком, а не правится построчно:
            // строк пять, а держать ссылки на каждую надпись ради экономии
            // одной пересборки в час — плохая сделка.
            if (_controlsRoot == null)
                return;

            for (int i = _controlsRoot.childCount - 1; i >= 0; i--)
                Destroy(_controlsRoot.GetChild(i).gameObject);

            RuntimeUi.CreateControlsList(_controlsRoot, 30f);
        }

        private void Show() => _root.SetActive(true);

        private void Hide() => _root.SetActive(false);

        // ---------- Сборка ----------

        private void Build()
        {
            RuntimeUi.CreateCanvas(gameObject, SortingOrder);

            _root = RuntimeUi.CreateStretched("Root", transform).gameObject;

            RuntimeUi.CreateFill("Overlay", _root.transform, RuntimeUi.OverlayColor);

            RectTransform panel = RuntimeUi.CreatePanel(
                _root.transform, 720f, new RectOffset(56, 56, 44, 44), 16f);

            RuntimeUi.CreateText(panel, Loc.GetOrFallback("pause.title", "Пауза"),
                                 56f, RuntimeUi.AccentColor, TextAlignmentOptions.Center);

            _controlsRoot = RuntimeUi.CreateColumn(panel, 10f);

            RuntimeUi.CreateControlsList(_controlsRoot, 30f);

            AddButton(panel, "pause.resume", "Продолжить", Resume);
            AddButton(panel, "pause.restart", "Начать заново", Restart);
            AddButton(panel, "pause.castle", "Выйти в замок", GoToCastle);
        }

        private void AddButton(Transform parent, string key, string fallback,
                               UnityEngine.Events.UnityAction action)
        {
            Button button = RuntimeUi.CreateButton(parent, Loc.GetOrFallback(key, fallback), 64f);

            button.onClick.AddListener(action);
            button.gameObject.AddComponent<Audio.UiClickSound>();
        }

        // ---------- Действия ----------

        private void Resume() => _state.Resume();

        private void Restart()
        {
            // Время возвращаем до загрузки: сцена, загруженная при timeScale = 0,
            // встанет намертво вместе с корутинами загрузчика.
            Time.timeScale = 1f;

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }

        private void GoToCastle()
        {
            Time.timeScale = 1f;

            SceneLoader loader = SceneLoader.Instance;

            // Загрузчика может не быть, если боевую сцену запустили напрямую
            // из редактора. Молчать в этом случае хуже, чем сказать почему.
            if (loader != null)
                loader.GoToCastle();
            else
                Debug.LogWarning("[Пауза] Нет SceneLoader — выход в замок недоступен. " +
                                 "Так бывает при запуске боя напрямую из редактора.");
        }
    }
}
