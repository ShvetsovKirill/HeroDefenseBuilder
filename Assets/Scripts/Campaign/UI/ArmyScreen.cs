using System.Collections.Generic;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HeroDefense.Localization;
using HeroDefense.UI;

namespace HeroDefense.Campaign.UI
{
    /// <summary>
    /// Армия в лагере: кто у тебя есть и что с ними делать.
    ///
    /// Закрывает дыру в цикле: командиров выдают за баронства, но до сих
    /// пор их некуда было девать — награда копилась в списке и пропадала
    /// впустую.
    ///
    /// Здесь принимаются два решения, ради которых лагерь и существует:
    /// кому отдать свободного командира и что делать с отрядом, чей
    /// командир погиб — найти нового или разжаловать в ополчение.
    ///
    /// Собран кодом, как остальные служебные экраны. Каркас под арт:
    /// портреты уже настоящие, рамки и фон встанут позже.
    /// </summary>
    public sealed class ArmyScreen : MonoBehaviour
    {
        private const int SortingOrder = 650;

        /// <summary>Ширина карточки отряда.</summary>
        private const float CardWidth = 300f;

        /// <summary>Высота карточки.</summary>
        private const float CardHeight = 260f;

        private static ArmyScreen _open;

        private Transform _squadRow;
        private Transform _commanderRow;
        private TMP_Text _hint;

        /// <summary>Отряд, которому сейчас ищут командира. -1 — никому.</summary>
        private int _pendingSlot = -1;

        /// <summary>Показать экран. Повторный вызов при открытом ничего не делает.</summary>
        public static void Show()
        {
            if (_open != null)
                return;

            var host = new GameObject("ArmyScreen");

            _open = host.AddComponent<ArmyScreen>();
        }

        private void Awake()
        {
            _open = this;
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

            // Поля панели считаются от рамки: она нарисована крупно,
            // и текст, прижатый к краю, ложится прямо на резьбу.
            RectTransform panel = RuntimeUi.CreatePanel(
                root, 1120f, new RectOffset(56, 56, 44, 44), 16f);

            RuntimeUi.CreateText(panel, Loc.GetOrFallback("army.title", "Войско"),
                                 46f, RuntimeUi.AccentColor, TextAlignmentOptions.Center);

            _hint = RuntimeUi.CreateText(panel, string.Empty, 24f,
                                         RuntimeUi.TextColor, TextAlignmentOptions.Center);

            _squadRow = CreateRow(panel);

            RuntimeUi.CreateText(panel, Loc.GetOrFallback("army.free", "Командиры без отряда"),
                                 30f, RuntimeUi.AccentColor, TextAlignmentOptions.Center);

            _commanderRow = CreateRow(panel);

            Button close = RuntimeUi.CreateButton(
                panel, Loc.GetOrFallback("common.back", "Назад"), 60f);

            close.onClick.AddListener(() => Destroy(gameObject));
            close.gameObject.AddComponent<Audio.UiClickSound>();
        }

        private static Transform CreateRow(Transform parent)
        {
            var host = new GameObject("Row", typeof(RectTransform));

            host.transform.SetParent(parent, false);

            var layout = host.AddComponent<HorizontalLayoutGroup>();

            layout.spacing = 16f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandWidth = false;
            layout.childForceExpandHeight = false;

            return host.transform;
        }

        // ---------- Содержимое ----------

        private void Refresh()
        {
            Clear(_squadRow);
            Clear(_commanderRow);

            CampaignState state = CampaignRun.State;

            _hint.text = _pendingSlot >= 0
                ? Loc.GetOrFallback("army.pick", "Выбери командира для отряда {0}")
                : Loc.GetOrFallback("army.hint", "Отрядов: {0}   Свободных командиров: {1}");

            _hint.text = _pendingSlot >= 0
                ? string.Format(_hint.text, _pendingSlot)
                : string.Format(_hint.text, state.AliveSquadCount, state.freeCommanders.Count);

            foreach (SquadRecord squad in state.squads)
                CreateSquadCard(squad);

            foreach (string commanderId in state.freeCommanders)
                CreateCommanderCard(commanderId);

            if (state.freeCommanders.Count == 0)
            {
                RuntimeUi.CreateText(_commanderRow,
                                     Loc.GetOrFallback("army.none", "Нет. Их дают за баронства."),
                                     24f, RuntimeUi.TextColor, TextAlignmentOptions.Center);
            }
        }

        private static void Clear(Transform row)
        {
            for (int i = row.childCount - 1; i >= 0; i--)
                Destroy(row.GetChild(i).gameObject);
        }

        private void CreateSquadCard(SquadRecord squad)
        {
            (GameObject host, Button button) = CreateCard(_squadRow);

            CommanderDefinition commander = CampaignRules.Current != null
                ? CampaignRules.Current.FindCommander(squad.commanderId)
                : null;

            if (commander != null && commander.portrait != null)
                CreatePortrait(host.transform, commander.portrait);

            string title = commander != null
                ? commander.commanderName
                : Loc.GetOrFallback("army.militia", "Ополчение");

            RuntimeUi.CreateText(host.transform, $"{squad.slot}. {title}", 26f,
                                 RuntimeUi.AccentColor, TextAlignmentOptions.Center);

            RuntimeUi.CreateText(host.transform, DescribeSpecialization(squad), 22f,
                                 RuntimeUi.TextColor, TextAlignmentOptions.Center);

            RuntimeUi.CreateText(host.transform,
                                 $"{Loc.GetOrFallback("army.size", "бойцов")}: {squad.size}",
                                 22f, RuntimeUi.TextColor, TextAlignmentOptions.Center);

            // Потерянный отряд показываем, а не скрываем: слот остаётся
            // в армии, и игрок должен видеть, что его больше нет, — иначе
            // пропажа читается как ошибка интерфейса.
            if (squad.wipedOut)
            {
                RuntimeUi.CreateText(host.transform,
                                     Loc.GetOrFallback("army.wiped", "Отряд потерян"),
                                     22f, new Color(0.75f, 0.3f, 0.3f), TextAlignmentOptions.Center);
            }
            // Осиротевший отряд — единственный, у кого есть выбор из двух:
            // дать нового командира или снять специализацию совсем.
            else if (squad.IsOrphaned)
            {
                RuntimeUi.CreateText(host.transform,
                                     Loc.GetOrFallback("army.orphan", "Командир погиб"),
                                     22f, new Color(0.9f, 0.5f, 0.4f), TextAlignmentOptions.Center);
            }

            button.onClick.AddListener(() => OnSquadClicked(squad));
        }

        private void CreateCommanderCard(string commanderId)
        {
            CommanderDefinition commander = CampaignRules.Current != null
                ? CampaignRules.Current.FindCommander(commanderId)
                : null;

            if (commander == null)
                return;

            (GameObject host, Button button) = CreateCard(_commanderRow);

            if (commander.portrait != null)
                CreatePortrait(host.transform, commander.portrait);

            RuntimeUi.CreateText(host.transform, commander.commanderName, 26f,
                                 RuntimeUi.AccentColor, TextAlignmentOptions.Center);

            RuntimeUi.CreateText(host.transform,
                                 $"+{commander.damageBonus:0} {Loc.GetOrFallback("army.damage", "урона")}   " +
                                 $"+{commander.healthBonus:0} {Loc.GetOrFallback("army.health", "здоровья")}",
                                 22f, RuntimeUi.TextColor, TextAlignmentOptions.Center);

            button.onClick.AddListener(() => OnCommanderClicked(commander));
        }

        private static (GameObject, Button) CreateCard(Transform parent)
        {
            var host = new GameObject("Card", typeof(RectTransform));

            host.transform.SetParent(parent, false);

            var background = host.AddComponent<Image>();

            RuntimeUi.ApplyCardLook(background);

            var button = host.AddComponent<Button>();

            button.targetGraphic = background;

            var element = host.AddComponent<LayoutElement>();

            element.preferredWidth = CardWidth;
            element.preferredHeight = CardHeight;
            element.flexibleWidth = 0f;

            var layout = host.AddComponent<VerticalLayoutGroup>();

            layout.padding = new RectOffset(16, 16, 14, 14);
            layout.spacing = 6f;
            layout.childAlignment = TextAnchor.UpperCenter;
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            return (host, button);
        }

        private static void CreatePortrait(Transform parent, Sprite sprite)
        {
            var host = new GameObject("Portrait", typeof(RectTransform));

            host.transform.SetParent(parent, false);

            var image = host.AddComponent<Image>();

            image.sprite = sprite;
            image.preserveAspect = true;
            image.raycastTarget = false;

            var element = host.AddComponent<LayoutElement>();

            element.preferredHeight = 96f;
            element.preferredWidth = 96f;
        }

        // ---------- Действия ----------

        /// <summary>
        /// Клик по отряду. Осиротевшего разжалуем, остальным — выбираем
        /// командира из свободных.
        /// </summary>
        private void OnSquadClicked(SquadRecord squad)
        {
            Audio.Sfx.Play(Audio.SoundId.UiClick);

            // Потерянному отряду командира не дают: людей в нём нет,
            // в бой он не выйдет, и добытый командир пропал бы впустую.
            if (squad.wipedOut)
            {
                _hint.text = Loc.GetOrFallback("army.wipedhint",
                                               "Этого отряда больше нет. Командира ему не дать.");
                return;
            }

            if (squad.IsOrphaned)
            {
                squad.DemoteToMilitia();
                CampaignRun.Save();
            }
            else if (!squad.HasCommander)
            {
                // Отмечаем отряд и ждём выбора командира: назначение —
                // это пара «кто» и «кому», и щёлкать надо по обоим.
                _pendingSlot = squad.slot;
            }

            Refresh();
        }

        /// <summary>
        /// Клик по свободному командиру. Если отряд уже выбран —
        /// назначаем; если нет, просим сначала выбрать отряд.
        /// </summary>
        private void OnCommanderClicked(CommanderDefinition commander)
        {
            Audio.Sfx.Play(Audio.SoundId.UiClick);

            CampaignState state = CampaignRun.State;
            SquadRecord squad = state.GetSquad(_pendingSlot);

            if (squad == null)
            {
                _hint.text = Loc.GetOrFallback("army.pickfirst", "Сначала выбери отряд без командира.");
                return;
            }

            // Специализация пока берётся первая доступная этому командиру:
            // выбор из трёх появится, когда специализации начнут отличаться
            // в бою. Сейчас разница только в названии, и спрашивать не о чем.
            SquadSpecialization specialization = FirstAllowed(commander);

            squad.Assign(commander.name, specialization);
            state.freeCommanders.Remove(commander.name);

            _pendingSlot = -1;

            CampaignRun.Save();
            Refresh();
        }

        private static SquadSpecialization FirstAllowed(CommanderDefinition commander)
        {
            foreach (SquadSpecialization specialization in new[]
                     {
                         SquadSpecialization.Spearmen,
                         SquadSpecialization.Swordsmen,
                         SquadSpecialization.Archers
                     })
            {
                if (commander.CanLead(specialization))
                    return specialization;
            }

            return SquadSpecialization.Spearmen;
        }

        private static string DescribeSpecialization(SquadRecord squad)
        {
            string name = squad.specialization switch
            {
                SquadSpecialization.Militia => "ополчение",
                SquadSpecialization.Spearmen => "копейщики",
                SquadSpecialization.Swordsmen => "мечники",
                SquadSpecialization.Archers => "лучники",
                SquadSpecialization.BattleMage => "боевой маг",
                SquadSpecialization.HealerMage => "маг-лекарь",
                _ => string.Empty
            };

            return Loc.GetOrFallback($"spec.{squad.specialization}", name);
        }
    }
}
