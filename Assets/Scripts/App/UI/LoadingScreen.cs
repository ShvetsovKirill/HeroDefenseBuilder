using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HeroDefense.Localization;
using HeroDefense.UI;

namespace HeroDefense.App.UI
{
    /// <summary>
    /// Затемнение с полоской на время перехода между сценами.
    ///
    /// <see cref="SceneLoader"/> давно шлёт <c>LoadStarted</c>, считает
    /// <c>Progress</c> и шлёт <c>LoadCompleted</c>, но слушателя не было:
    /// переход выглядел как зависание игры на полсекунды.
    ///
    /// Живёт между сценами и создаётся сам — иначе его пришлось бы держать
    /// в каждой сцене, откуда возможен переход, то есть во всех.
    /// </summary>
    [DefaultExecutionOrder(-1900)]
    public sealed class LoadingScreen : MonoBehaviour
    {
        /// <summary>Поверх всего: переход должен закрывать и модальные экраны.</summary>
        private const int SortingOrder = 1000;

        /// <summary>Скорость появления и затухания, доля прозрачности в секунду.</summary>
        private const float FadeSpeed = 4f;

        private CanvasGroup _group;
        private RectTransform _bar;
        private TMP_Text _label;
        private bool _visible;

        /// <summary>
        /// Создаётся один раз за запуск игры. AfterSceneLoad, а не Before:
        /// на BeforeSceneLoad ещё нет сцены, куда положить объект.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.AfterSceneLoad)]
        private static void Install()
        {
            var host = new GameObject("LoadingScreen");

            host.AddComponent<LoadingScreen>();
            DontDestroyOnLoad(host);
        }

        private void Awake()
        {
            Build();

            SceneLoader.LoadStarted += OnLoadStarted;
            SceneLoader.LoadCompleted += OnLoadCompleted;
        }

        private void OnDestroy()
        {
            SceneLoader.LoadStarted -= OnLoadStarted;
            SceneLoader.LoadCompleted -= OnLoadCompleted;
        }

        private void OnLoadStarted(GameScene scene)
        {
            _visible = true;

            if (_label != null)
                _label.text = Loc.GetOrFallback("loading.title", "Загрузка");
        }

        private void OnLoadCompleted(GameScene scene) => _visible = false;

        private void Update()
        {
            // Неотмасштабированное время обязательно: переход часто начинается
            // с паузы, где timeScale равен нулю, и обычное время стоит.
            float target = _visible ? 1f : 0f;

            _group.alpha = Mathf.MoveTowards(_group.alpha, target, FadeSpeed * Time.unscaledDeltaTime);
            _group.blocksRaycasts = _group.alpha > 0.01f;

            SceneLoader loader = SceneLoader.Instance;

            if (_bar != null && loader != null)
                _bar.anchorMax = new Vector2(Mathf.Clamp01(loader.Progress), 1f);
        }

        // ---------- Сборка ----------

        private void Build()
        {
            RuntimeUi.CreateCanvas(gameObject, SortingOrder);

            RectTransform root = RuntimeUi.CreateStretched("Root", transform);

            _group = root.gameObject.AddComponent<CanvasGroup>();
            _group.alpha = 0f;

            RuntimeUi.CreateFill("Overlay", root, new Color(0.03f, 0.03f, 0.05f, 1f));

            _label = RuntimeUi.CreateText(root, Loc.GetOrFallback("loading.title", "Загрузка"),
                                          44f, RuntimeUi.TextColor, TextAlignmentOptions.Center);

            RectTransform labelRect = _label.rectTransform;

            labelRect.anchorMin = new Vector2(0.5f, 0.5f);
            labelRect.anchorMax = new Vector2(0.5f, 0.5f);
            labelRect.sizeDelta = new Vector2(600f, 60f);
            labelRect.anchoredPosition = new Vector2(0f, 40f);

            BuildBar(root);
        }

        /// <summary>
        /// Полоска прогресса. Не Slider: ползунок принимает ввод и был бы
        /// схватываемым мышью прямо на переходе.
        ///
        /// Заполнение сделано якорями, а не режимом Filled: тот работает
        /// только со спрайтом, а спрайта у служебной панели нет — вышла бы
        /// молча неподвижная полоска.
        /// </summary>
        private void BuildBar(Transform parent)
        {
            var track = new GameObject("BarTrack", typeof(RectTransform));

            var trackRect = track.GetComponent<RectTransform>();

            trackRect.SetParent(parent, false);
            trackRect.anchorMin = new Vector2(0.5f, 0.5f);
            trackRect.anchorMax = new Vector2(0.5f, 0.5f);
            trackRect.sizeDelta = new Vector2(520f, 10f);
            trackRect.anchoredPosition = new Vector2(0f, -20f);

            var trackImage = track.AddComponent<Image>();

            UiSkin skin = RuntimeUi.Skin;

            if (skin != null && skin.barTrack != null)
            {
                trackImage.sprite = skin.barTrack;
                trackImage.type = Image.Type.Sliced;
                trackImage.color = Color.white;

                // Со спрайтом ложе выше: у него своя рамка и наконечники.
                trackRect.sizeDelta = new Vector2(560f, 44f);
            }
            else
            {
                trackImage.color = new Color(1f, 1f, 1f, 0.12f);
            }

            var fill = new GameObject("BarFill", typeof(RectTransform));

            var fillRect = fill.GetComponent<RectTransform>();

            fillRect.SetParent(trackRect, false);
            fillRect.anchorMin = Vector2.zero;
            fillRect.anchorMax = Vector2.one;
            fillRect.offsetMin = Vector2.zero;
            fillRect.offsetMax = Vector2.zero;

            var fillImage = fill.AddComponent<Image>();

            if (skin != null && skin.barFill != null)
            {
                fillImage.sprite = skin.barFill;
                fillImage.type = Image.Type.Sliced;
                fillImage.color = Color.white;
            }
            else
            {
                fillImage.color = RuntimeUi.AccentColor;
            }

            fillRect.anchorMax = new Vector2(0f, 1f);

            _bar = fillRect;
        }
    }
}
