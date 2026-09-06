using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HeroDefense.Building;
using HeroDefense.Localization;
using HeroDefense.Meta;
using HeroDefense.UI;

namespace HeroDefense.Campaign.UI
{
    /// <summary>
    /// Сборка колоды перед походом (D121–D124).
    ///
    /// Здесь принимается решение, ради которого колода вообще существует:
    /// <b>карточек меньше, чем хочется поставить</b>. Взял четыре башни —
    /// не взял ферму, и всю дорогу будешь считать золото.
    ///
    /// До этого экрана механика была написана целиком, но недостижима:
    /// <see cref="RunLoadout"/> никто не заполнял, и бой молча работал
    /// по лимитам из ассетов, то есть без всякого выбора.
    ///
    /// Колода собирается один раз на поход, а не на каждый бой: поход
    /// длинный, и переспрашивать перед каждым владением значило бы
    /// превратить решение в рутину.
    /// </summary>
    public sealed class DeckScreen : MonoBehaviour
    {
        private const int SortingOrder = 680;

        /// <summary>Ширина карточки типа постройки.</summary>
        private const float CardWidth = 260f;

        /// <summary>Высота карточки.</summary>
        private const float CardHeight = 300f;

        private static DeckScreen _open;

        private readonly List<BuildingDefinition> _available = new();

        private Transform _row;
        private TMP_Text _counter;
        private System.Action _onDone;

        /// <summary>
        /// Показать экран. <paramref name="onDone"/> зовётся, когда игрок
        /// закончил сборку, — лагерь на этом уводит его на карту.
        /// </summary>
        public static void Show(System.Action onDone)
        {
            if (_open != null)
                return;

            var host = new GameObject("DeckScreen");

            _open = host.AddComponent<DeckScreen>();
            _open._onDone = onDone;
        }

        private void Awake()
        {
            _open = this;

            CampaignRules rules = CampaignRules.Current;

            if (rules != null)
                rules.FillAvailableBuildings(CampaignRun.State, _available);

            // Уже собранная колода не теряется при повторном заходе:
            // экран правит её, а не начинает с нуля.
            CampaignBuildDeck.RestoreFromState();

            Build();
            Refresh();
        }

        private void OnDestroy()
        {
            if (_open == this)
                _open = null;
        }

        // ---------- Сборка ----------

        private void Build()
        {
            RuntimeUi.CreateCanvas(gameObject, SortingOrder);

            RectTransform root = RuntimeUi.CreateStretched("Root", transform);

            RuntimeUi.CreateFill("Overlay", root, RuntimeUi.OverlayColor);

            RectTransform panel = RuntimeUi.CreatePanel(
                root, 1120f, new RectOffset(56, 56, 44, 44), 16f);

            RuntimeUi.CreateText(panel, Loc.GetOrFallback("deck.title", "Что берём в поход"),
                                 46f, RuntimeUi.AccentColor, TextAlignmentOptions.Center);

            _counter = RuntimeUi.CreateText(panel, string.Empty, 26f,
                                            RuntimeUi.TextColor, TextAlignmentOptions.Center);

            var row = new GameObject("Cards", typeof(RectTransform));

            row.transform.SetParent(panel, false);

            var layout = row.AddComponent<HorizontalLayoutGroup>();

            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            _row = row.transform;

            Button ready = RuntimeUi.CreateButton(
                panel, Loc.GetOrFallback("deck.ready", "В поход"), 60f);

            ready.onClick.AddListener(Finish);
            ready.gameObject.AddComponent<Audio.UiClickSound>();
        }

        // ---------- Содержимое ----------

        private void Refresh()
        {
            for (int i = _row.childCount - 1; i >= 0; i--)
                Destroy(_row.GetChild(i).gameObject);

            _counter.text = string.Format(
                Loc.GetOrFallback("deck.counter", "Карточек: {0} из {1}"),
                RunLoadout.TotalCards, RunLoadout.MaxCards);

            if (_available.Count == 0)
            {
                RuntimeUi.CreateText(_row, Loc.GetOrFallback("deck.empty", "Строить нечего."),
                                     26f, RuntimeUi.TextColor, TextAlignmentOptions.Center);
                return;
            }

            foreach (BuildingDefinition building in _available)
                CreateCard(building);
        }

        private void CreateCard(BuildingDefinition building)
        {
            var host = new GameObject("Card_" + building.name, typeof(RectTransform));

            host.transform.SetParent(_row, false);

            var background = host.AddComponent<Image>();

            RuntimeUi.ApplyCardLook(background);

            var element = host.AddComponent<LayoutElement>();

            element.preferredWidth = CardWidth;
            element.preferredHeight = CardHeight;
            element.flexibleWidth = 0f;

            var layout = host.AddComponent<VerticalLayoutGroup>();

            layout.padding = new RectOffset(14, 14, 12, 12);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            if (building.icon != null)
                CreateIcon(host.transform, building.icon);

            RuntimeUi.CreateText(host.transform, building.DisplayName, 26f,
                                 RuntimeUi.AccentColor, TextAlignmentOptions.Center);

            RuntimeUi.CreateText(host.transform,
                                 Loc.GetOrFallback("deck.cost", "цена") + ": " + building.cost,
                                 22f, RuntimeUi.TextColor, TextAlignmentOptions.Center);

            RuntimeUi.CreateText(host.transform, "x " + RunLoadout.GetCount(building), 34f,
                                 RuntimeUi.TextColor, TextAlignmentOptions.Center);

            CreateStepper(host.transform, building);
        }

        /// <summary>
        /// Пара кнопок «убрать» и «добавить». Не поле ввода и не ползунок:
        /// карточек единицы, и щёлкать по ним быстрее, чем набирать число.
        /// </summary>
        private void CreateStepper(Transform parent, BuildingDefinition building)
        {
            var host = new GameObject("Stepper", typeof(RectTransform));

            host.transform.SetParent(parent, false);

            var layout = host.AddComponent<HorizontalLayoutGroup>();

            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.MiddleCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            var element = host.AddComponent<LayoutElement>();

            element.preferredHeight = 56f;

            Button minus = RuntimeUi.CreateButton(host.transform, "-", 52f);
            Button plus = RuntimeUi.CreateButton(host.transform, "+", 52f);

            minus.onClick.AddListener(() =>
            {
                Audio.Sfx.Play(Audio.SoundId.UiClick);
                RunLoadout.TryRemoveCard(building);
                Refresh();
            });

            plus.onClick.AddListener(() =>
            {
                Audio.Sfx.Play(Audio.SoundId.UiClick);
                RunLoadout.TryAddCard(building);
                Refresh();
            });
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

            element.preferredHeight = 84f;
            element.preferredWidth = 84f;
        }

        // ---------- Итог ----------

        /// <summary>
        /// Сборка закончена. Пустую колоду добираем сами: игрок, который
        /// просто нажал «в поход», должен получить рабочий набор, а не
        /// забег, в котором нельзя построить ничего.
        /// </summary>
        private void Finish()
        {
            Audio.Sfx.Play(Audio.SoundId.UiClick);

            if (RunLoadout.TotalCards == 0)
                RunLoadout.FillWithDefaults(_available);

            CampaignBuildDeck.SaveToState();

            System.Action done = _onDone;

            Destroy(gameObject);

            done?.Invoke();
        }
    }
}
