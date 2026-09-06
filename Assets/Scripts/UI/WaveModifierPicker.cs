using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HeroDefense.Core;
using HeroDefense.Localization;
using HeroDefense.Waves;

namespace HeroDefense.UI
{
    /// <summary>
    /// Выбор условия следующей волны в перерыве между волнами (D125).
    ///
    /// Отвечает на главную беду прототипа: с какого-то момента оборона
    /// держит сама и игроку нечего делать. Здесь безделье превращается
    /// в решение — «раз меня не трогают, беру потяжелее и зарабатываю».
    ///
    /// Игра при этом не останавливается: перерыв и так короткий, а пауза
    /// посреди боя рвёт темп. Не выбрал — волна идёт обычной, и это
    /// нормальный исход, а не пропущенное действие.
    /// </summary>
    [DefaultExecutionOrder(-600)]
    public sealed class WaveModifierPicker : MonoBehaviour
    {
        /// <summary>Над HUD, но ниже паузы: пауза должна перекрывать выбор.</summary>
        private const int SortingOrder = 450;

        /// <summary>Ширина одной карточки.</summary>
        private const float CardWidth = 380f;

        /// <summary>Высота карточки. Хватает на иконку, название и две строки описания.</summary>
        private const float CardHeight = 300f;

        private readonly List<WaveModifier> _choices = new();

        private WaveRunner _runner;
        private GameObject _root;
        private Transform _row;
        private GameObject _banner;
        private TMP_Text _bannerText;

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

            var host = new GameObject("WaveModifierPicker");

            host.AddComponent<WaveModifierPicker>();
        }

        private void Start()
        {
            _runner = FindFirstObjectByType<WaveRunner>();

            // Нет раннера или нет набора условий — экрану нечего показывать.
            if (_runner == null || WaveModifiers.Set == null)
            {
                Destroy(gameObject);
                return;
            }

            Build();
            Hide();

            _runner.WaveCleared += OnWaveCleared;
            _runner.WaveStarted += OnWaveStarted;
            WaveModifiers.Changed += OnModifierChanged;
        }

        private void OnDestroy()
        {
            if (_runner == null)
                return;

            _runner.WaveCleared -= OnWaveCleared;
            _runner.WaveStarted -= OnWaveStarted;
            WaveModifiers.Changed -= OnModifierChanged;
        }

        /// <summary>
        /// Показать, под каким условием идёт волна.
        ///
        /// Без этого игрок через полминуты боя уже не помнит, что выбрал,
        /// и не может связать «врагов вдвое больше» со своим же решением.
        /// </summary>
        private void OnModifierChanged(WaveModifier modifier)
        {
            if (_banner == null)
                return;

            bool visible = modifier != null;

            if (visible)
                _bannerText.text = $"{modifier.DisplayName}   {FormatEffect(modifier)}";

            if (_banner.activeSelf != visible)
                _banner.SetActive(visible);
        }

        // ---------- Показ ----------

        private void OnWaveCleared(int number)
        {
            int next = number + 1;

            // Последняя волна уже отбита — предлагать нечего.
            if (next > _runner.TotalWaves || !WaveModifiers.Set.ShouldOffer(next))
                return;

            if (!WaveModifiers.TryPick(WaveModifiers.Set.choicesPerWave, _choices))
                return;

            FillRow();
            _root.SetActive(true);
        }

        /// <summary>
        /// Волна началась — выбирать поздно. Прячем в любом случае: панель,
        /// висящая во время боя, закрывает поле и сбивает с толку.
        /// </summary>
        private void OnWaveStarted(int number) => Hide();

        private void Hide()
        {
            if (_root != null)
                _root.SetActive(false);
        }

        private void Choose(WaveModifier modifier)
        {
            WaveModifiers.Apply(modifier);
            Audio.Sfx.Play(Audio.SoundId.UiClick);

            Hide();
        }

        // ---------- Сборка ----------

        private void Build()
        {
            RuntimeUi.CreateCanvas(gameObject, SortingOrder);

            _root = RuntimeUi.CreateStretched("Root", transform).gameObject;

            var column = new GameObject("Choices", typeof(RectTransform));

            var rect = column.GetComponent<RectTransform>();

            rect.SetParent(_root.transform, false);

            // Верх по центру: середина экрана занята боем, а перерыв —
            // единственное время, когда игрок смотрит вверх.
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -150f);
            rect.sizeDelta = new Vector2(CardWidth * 3f + 80f, 0f);

            var layout = column.AddComponent<VerticalLayoutGroup>();

            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = column.AddComponent<ContentSizeFitter>();

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RuntimeUi.CreateText(column.transform,
                                 Loc.GetOrFallback("wave.choose", "Выбери условие следующей волны"),
                                 36f, RuntimeUi.AccentColor, TextAlignmentOptions.Center);

            var row = new GameObject("Row", typeof(RectTransform));

            row.transform.SetParent(column.transform, false);

            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();

            rowLayout.spacing = 20f;
            rowLayout.childAlignment = TextAnchor.UpperCenter;
            rowLayout.childControlHeight = true;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            _row = row.transform;

            BuildBanner();
        }

        /// <summary>Узкая плашка с действующим условием. Висит всю волну.</summary>
        private void BuildBanner()
        {
            var host = new GameObject("ActiveModifier", typeof(RectTransform));

            var rect = host.GetComponent<RectTransform>();

            rect.SetParent(_root.transform.parent, false);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -20f);
            rect.sizeDelta = new Vector2(700f, 0f);

            var background = host.AddComponent<Image>();

            RuntimeUi.ApplyPanelLook(background, RuntimeUi.Skin != null ? RuntimeUi.Skin.panelCompact : null);

            background.raycastTarget = false;

            var layout = host.AddComponent<VerticalLayoutGroup>();

            layout.padding = new RectOffset(20, 20, 10, 10);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = host.AddComponent<ContentSizeFitter>();

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            _bannerText = RuntimeUi.CreateText(host.transform, string.Empty, 26f,
                                               RuntimeUi.TextColor, TextAlignmentOptions.Center);

            _banner = host;
            _banner.SetActive(false);
        }

        /// <summary>
        /// Пересобрать карточки под новый набор.
        ///
        /// Каждый раз заново, а не переиспользуя: наборов всего три-четыре
        /// за забег, и хранить пул карточек ради этого — сложность на пустом месте.
        /// </summary>
        private void FillRow()
        {
            for (int i = _row.childCount - 1; i >= 0; i--)
                Destroy(_row.GetChild(i).gameObject);

            foreach (WaveModifier modifier in _choices)
                CreateCard(modifier);
        }

        private void CreateCard(WaveModifier modifier)
        {
            var host = new GameObject("Card", typeof(RectTransform));

            host.transform.SetParent(_row, false);

            var background = host.AddComponent<Image>();

            RuntimeUi.ApplyCardLook(background);

            var button = host.AddComponent<Button>();

            button.targetGraphic = background;
            button.onClick.AddListener(() => Choose(modifier));

            var element = host.AddComponent<LayoutElement>();

            element.preferredWidth = CardWidth;
            element.preferredHeight = CardHeight;

            var layout = host.AddComponent<VerticalLayoutGroup>();

            layout.padding = new RectOffset(24, 24, 20, 20);
            layout.spacing = 10f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            if (modifier.icon != null)
                CreateIcon(host.transform, modifier.icon);

            RuntimeUi.CreateText(host.transform, modifier.DisplayName, 30f,
                                 RuntimeUi.AccentColor, TextAlignmentOptions.Center);

            TMP_Text description = RuntimeUi.CreateText(host.transform, modifier.Description, 24f,
                                                        RuntimeUi.TextColor, TextAlignmentOptions.Top);

            description.gameObject.AddComponent<LayoutElement>().flexibleHeight = 1f;

            RuntimeUi.CreateText(host.transform, FormatEffect(modifier), 22f,
                                 RuntimeUi.TextColor, TextAlignmentOptions.Center);
        }

        private static void CreateIcon(Transform parent, Sprite sprite)
        {
            var host = new GameObject("Icon", typeof(RectTransform));

            host.transform.SetParent(parent, false);

            var image = host.AddComponent<Image>();

            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var element = host.AddComponent<LayoutElement>();

            element.preferredHeight = 96f;
            element.preferredWidth = 96f;
        }

        /// <summary>
        /// Сухая строка с числами под описанием.
        ///
        /// Нужна, потому что описание пишет дизайнер словами, а игрок к третьему
        /// забегу считает множители. Показываем только то, что отличается
        /// от единицы: строка «x1 врагов, x1 здоровья» не несёт ничего.
        /// </summary>
        private static string FormatEffect(WaveModifier modifier)
        {
            var parts = new List<string>();

            Append(parts, modifier.enemyCount, Loc.GetOrFallback("wave.mod.count", "врагов"));
            Append(parts, modifier.enemyHealth, Loc.GetOrFallback("wave.mod.health", "здоровья"));
            Append(parts, modifier.enemySpeed, Loc.GetOrFallback("wave.mod.speed", "скорости"));
            Append(parts, modifier.goldReward, Loc.GetOrFallback("wave.mod.gold", "золота"));

            // Плотность обратная: меньший интервал — это чаще, а не реже.
            if (!Mathf.Approximately(modifier.spawnInterval, 1f))
                parts.Add($"{Multiplier(1f / modifier.spawnInterval)} " +
                          Loc.GetOrFallback("wave.mod.density", "плотность"));

            return string.Join("   ", parts);
        }

        private static void Append(List<string> parts, float value, string label)
        {
            if (!Mathf.Approximately(value, 1f))
                parts.Add($"{Multiplier(value)} {label}");
        }

        /// <summary>
        /// Множитель одинаково на любой машине: разделитель дробной части
        /// берётся не из системной локали. Иначе на русской Windows выходит
        /// «×1,6», а на английской «×1.6» — при одном и том же языке игры.
        /// </summary>
        private static string Multiplier(float value) =>
            "<b>×" + value.ToString("0.#", System.Globalization.CultureInfo.InvariantCulture) + "</b>";
    }
}
