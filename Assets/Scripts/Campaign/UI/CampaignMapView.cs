using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HeroDefense.App;
using HeroDefense.Localization;
using HeroDefense.UI;

namespace HeroDefense.Campaign.UI
{
    /// <summary>
    /// Карта кампании: куда идти дальше.
    ///
    /// Собрана кодом, как и остальные служебные экраны. Это **каркас
    /// под арт**: узлы, связи и правила уже работают, а вместо кружков
    /// потом встанут нарисованные владения на нарисованной карте.
    ///
    /// Главное правило экрана: **до входа во владение видно, что там** —
    /// тип места, кто нападает, какая награда. Без этого выбор
    /// на развилке превращается в лотерею, а вся необратимость пути
    /// работает вхолостую.
    /// </summary>
    [DefaultExecutionOrder(-600)]
    public sealed class CampaignMapView : MonoBehaviour
    {
        private const int SortingOrder = 200;

        /// <summary>Ширина карточки владения.</summary>
        private const float CardWidth = 320f;

        /// <summary>Высота карточки.</summary>
        private const float CardHeight = 320f;

        private readonly List<int> _available = new();

        private Transform _row;
        private TMP_Text _status;

        private void Start()
        {
            if (CampaignRules.Current == null)
            {
                Debug.LogError("[Карта] Нет правил кампании — показывать нечего.");
                return;
            }

            Build();
            Refresh();
        }

        // ---------- Сборка ----------

        private void Build()
        {
            RuntimeUi.CreateCanvas(gameObject, SortingOrder);

            RectTransform root = RuntimeUi.CreateStretched("Root", transform);

            RuntimeUi.CreateFill("Background", root, new Color(0.10f, 0.12f, 0.16f, 1f));

            var column = new GameObject("Column", typeof(RectTransform));

            var rect = column.GetComponent<RectTransform>();

            rect.SetParent(root, false);
            rect.anchorMin = new Vector2(0.5f, 1f);
            rect.anchorMax = new Vector2(0.5f, 1f);
            rect.pivot = new Vector2(0.5f, 1f);
            rect.anchoredPosition = new Vector2(0f, -80f);
            rect.sizeDelta = new Vector2(1500f, 0f);

            var layout = column.AddComponent<VerticalLayoutGroup>();

            layout.spacing = 24f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = column.AddComponent<ContentSizeFitter>();

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RuntimeUi.CreateText(column.transform,
                                 Loc.GetOrFallback("map.title", "Куда направимся, государь?"),
                                 44f, RuntimeUi.AccentColor, TextAlignmentOptions.Center);

            _status = RuntimeUi.CreateText(column.transform, string.Empty, 26f,
                                           RuntimeUi.TextColor, TextAlignmentOptions.Center);

            var row = new GameObject("Choices", typeof(RectTransform));

            row.transform.SetParent(column.transform, false);

            var rowLayout = row.AddComponent<HorizontalLayoutGroup>();

            rowLayout.spacing = 24f;
            rowLayout.childAlignment = TextAnchor.UpperCenter;
            rowLayout.childControlHeight = true;
            rowLayout.childControlWidth = true;
            rowLayout.childForceExpandWidth = false;
            rowLayout.childForceExpandHeight = false;

            _row = row.transform;

            Button toCamp = RuntimeUi.CreateButton(
                column.transform, Loc.GetOrFallback("map.camp", "В лагерь"), 60f);

            toCamp.onClick.AddListener(() => SceneLoader.Instance?.GoToCastle());
            toCamp.gameObject.AddComponent<Audio.UiClickSound>();
        }

        // ---------- Содержимое ----------

        private void Refresh()
        {
            for (int i = _row.childCount - 1; i >= 0; i--)
                Destroy(_row.GetChild(i).gameObject);

            _available.Clear();
            _available.AddRange(CampaignFlow.GetAvailableNodes());

            CampaignState state = CampaignRun.State;

            _status.text = Loc.GetOrFallback("map.status", "Отрядов в строю: {0}   Владений пройдено: {1}");
            _status.text = string.Format(_status.text, state.AliveSquadCount, state.holdingsCleared);

            // Дороги кончились — впереди последняя битва. Она показывается
            // такой же карточкой, чтобы игрок увидел, кто его там ждёт,
            // и вошёл в неё сам: это решение, а не автоматический переход.
            if (_available.Count == 0)
            {
                HoldingDefinition final = CampaignFlow.Map != null
                    ? CampaignFlow.Map.finalBattle
                    : null;

                if (final == null)
                {
                    RuntimeUi.CreateText(_row, Loc.GetOrFallback("map.done", "Путь пройден до конца."),
                                         30f, RuntimeUi.TextColor, TextAlignmentOptions.Center);
                    return;
                }

                _status.text = Loc.GetOrFallback(
                    "map.final", "Дорог больше нет. Впереди — последняя битва.");

                CreateCard("Final", final, EnterFinal, FinalTitleColor);

                return;
            }

            foreach (int node in _available)
            {
                HoldingDefinition holding = CampaignFlow.Map?.GetNode(node)?.holding;
                int captured = node;

                if (holding != null)
                    CreateCard($"Holding_{node}", holding, () => Choose(captured), RuntimeUi.AccentColor);
            }
        }

        private void CreateCard(string name, HoldingDefinition holding,
                                System.Action onClick, Color titleColor)
        {
            var host = new GameObject(name, typeof(RectTransform));

            host.transform.SetParent(_row, false);

            var background = host.AddComponent<Image>();

            RuntimeUi.ApplyCardLook(background);

            var button = host.AddComponent<Button>();

            button.targetGraphic = background;
            button.onClick.AddListener(() => onClick());

            var element = host.AddComponent<LayoutElement>();

            element.preferredWidth = CardWidth;
            element.preferredHeight = CardHeight;

            // Без этого одинокая карточка растягивается на всю строку
            // и перестаёт быть карточкой.
            element.flexibleWidth = 0f;

            var layout = host.AddComponent<VerticalLayoutGroup>();

            layout.padding = new RectOffset(20, 20, 16, 16);
            layout.spacing = 8f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            if (holding.icon != null)
                CreateIcon(host.transform, holding.icon);

            RuntimeUi.CreateText(host.transform, holding.DisplayName, 30f,
                                 titleColor, TextAlignmentOptions.Center);

            // То, ради чего экран существует: игрок должен видеть, во что
            // ввязывается, ДО того как свернёт — назад дороги нет.
            RuntimeUi.CreateText(host.transform, DescribeKind(holding), 22f,
                                 RuntimeUi.TextColor, TextAlignmentOptions.Center);

            RuntimeUi.CreateText(host.transform, DescribeFaction(holding), 22f,
                                 RuntimeUi.TextColor, TextAlignmentOptions.Center);

            RuntimeUi.CreateText(host.transform, DescribeRewards(holding), 22f,
                                 RuntimeUi.AccentColor, TextAlignmentOptions.Center);
        }

        /// <summary>
        /// Значок владения. Первое, что видит игрок в карточке: тип места
        /// узнаётся силуэтом быстрее, чем прочитывается название.
        /// </summary>
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

        /// <summary>Цвет заголовка последней битвы: она не рядовое владение.</summary>
        private static readonly Color FinalTitleColor = new(0.92f, 0.42f, 0.32f);

        private void Choose(int node)
        {
            Audio.Sfx.Play(Audio.SoundId.UiClick);
            CampaignFlow.EnterHolding(node);
        }

        private void EnterFinal()
        {
            Audio.Sfx.Play(Audio.SoundId.UiClick);
            CampaignFlow.EnterFinalBattle();
        }

        // ---------- Строки ----------

        private static string DescribeKind(HoldingDefinition holding)
        {
            string kind = holding.kind switch
            {
                HoldingKind.Village => "село",
                HoldingKind.Town => "деревня",
                HoldingKind.Port => "порт",
                HoldingKind.Barony => "баронство",
                HoldingKind.Castle => "замок",
                HoldingKind.WarCamp => "лагерь",
                _ => string.Empty
            };

            return $"{Loc.GetOrFallback($"holding.kind.{holding.kind}", kind)}   " +
                   $"{holding.WaveCount} " + Loc.GetOrFallback("map.waves", "волн");
        }

        private static string DescribeFaction(HoldingDefinition holding)
        {
            string faction = holding.faction switch
            {
                EnemyFaction.Bandits => "бандиты",
                EnemyFaction.Beasts => "звери",
                EnemyFaction.Monsters => "чудища",
                EnemyFaction.Undead => "нежить",
                EnemyFaction.Vikings => "викинги",
                EnemyFaction.RivalLord => "войско соседа",
                _ => string.Empty
            };

            return Loc.GetOrFallback($"faction.{holding.faction}", faction);
        }

        private static string DescribeRewards(HoldingDefinition holding)
        {
            if (holding.rewards == null || holding.rewards.Length == 0)
                return string.Empty;

            var parts = new List<string>();

            foreach (HoldingReward reward in holding.rewards)
            {
                if (reward != null)
                    parts.Add(reward.Describe());
            }

            return string.Join(", ", parts);
        }
    }
}
