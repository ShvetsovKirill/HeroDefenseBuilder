using System.Collections.Generic;
using UnityEngine;
using HeroDefense.App;
using HeroDefense.Meta;

namespace HeroDefense.Campaign
{
    /// <summary>
    /// Ход кампании: выбор владения, запуск боя, разбор итога, возврат
    /// на карту.
    ///
    /// Один класс на всю логику переходов — потому что иначе она
    /// расползётся между картой, боевой сценой и экраном итогов, и
    /// «кто отвечает за награды» станет неизвестно.
    ///
    /// Сам ничего не рисует и не грузит сцены руками: сцены грузит
    /// <see cref="SceneLoader"/>, экраны показывают себя сами.
    /// </summary>
    public static class CampaignFlow
    {
        // Буфер только для внутренних проверок. Наружу он не отдаётся:
        // общий список, розданный вызывающим, затирался бы вложенным
        // вызовом прямо во время их перебора.
        private static readonly List<int> NextBuffer = new();

        /// <summary>Карта текущей кампании. Null, если правила не заведены.</summary>
        public static CampaignMapDefinition Map => CampaignRules.Current != null
            ? CampaignRules.Current.map
            : null;

        /// <summary>Владение, в которое игрок вошёл. Null вне боя.</summary>
        public static HoldingDefinition CurrentHolding
        {
            get
            {
                CampaignMapDefinition map = Map;

                if (map == null)
                    return null;

                // Последняя битва не узел карты: путь к ней уже пройден,
                // и на карте её нет — она то, что за картой.
                if (CampaignRun.State.inFinalBattle)
                    return map.finalBattle;

                return map.GetNode(CampaignRun.State.currentNode)?.holding;
            }
        }

        /// <summary>
        /// Дошёл ли игрок до конца пути. Дороги кончились — значит впереди
        /// только последняя битва.
        /// </summary>
        public static bool IsAtFinalBattle =>
            CampaignRun.IsActive
            && Map != null
            && Map.finalBattle != null
            && CountAvailableNodes() == 0;

        // ---------- Начало ----------

        /// <summary>Начать новую кампанию и уйти на карту.</summary>
        public static void BeginCampaign()
        {
            CampaignRules rules = CampaignRules.Current;

            if (rules == null || rules.map == null)
            {
                Debug.LogError("[Кампания] Нет правил или карты — начинать нечего.");
                return;
            }

            CampaignRun.Begin(rules.map, rules);
        }

        /// <summary>
        /// Куда можно пойти сейчас.
        ///
        /// В начале пути — точки входа карты, дальше — только вперёд
        /// из текущего узла. Назад дороги нет: свернул на развилке —
        /// вторая ветка потеряна.
        /// </summary>
        public static void FillAvailableNodes(List<int> results)
        {
            if (results == null)
                return;

            results.Clear();

            CampaignMapDefinition map = Map;

            if (map == null)
                return;

            CampaignState state = CampaignRun.State;

            if (state.currentNode < 0)
            {
                foreach (int entry in map.entryNodes)
                {
                    if (map.GetNode(entry) != null)
                        results.Add(entry);
                }

                return;
            }

            map.GetNextNodes(state.currentNode, results);
        }

        /// <summary>Сколько дорог ведёт вперёд. Ноль — путь кончился.</summary>
        public static int CountAvailableNodes()
        {
            FillAvailableNodes(NextBuffer);

            return NextBuffer.Count;
        }

        // ---------- Владение ----------

        /// <summary>
        /// Войти во владение: запомнить выбор и уйти в бой.
        ///
        /// Выбор запоминается ДО боя, а не после: если игрок закроет игру
        /// посреди забега, кампания должна помнить, где он был.
        /// </summary>
        public static void EnterHolding(int node)
        {
            CampaignMapDefinition map = Map;

            if (map == null || map.GetNode(node) == null)
            {
                Debug.LogError($"[Кампания] Узла {node} нет на карте.");
                return;
            }

            CampaignRun.State.currentNode = node;
            CampaignRun.State.inFinalBattle = false;
            CampaignRun.Save();

            SceneLoader.Instance?.GoToBattle();
        }

        /// <summary>
        /// Войти в последнюю битву. Она решает исход всей кампании:
        /// победа здесь — единственная победа, которая у игры есть.
        /// </summary>
        public static void EnterFinalBattle()
        {
            if (Map == null || Map.finalBattle == null)
            {
                Debug.LogError("[Кампания] Последняя битва не назначена в карте.");
                return;
            }

            CampaignRun.State.inFinalBattle = true;
            CampaignRun.Save();

            SceneLoader.Instance?.GoToBattle();
        }

        /// <summary>
        /// Бой окончен. Обновляем состояние, выдаём награды и решаем,
        /// продолжается ли кампания.
        ///
        /// Поражение не обрывает поход: владение теряется вместе
        /// с наградой, но король идёт дальше, пока есть армия.
        /// </summary>
        public static void FinishHolding(bool victory)
        {
            CampaignState state = CampaignRun.State;

            // Последняя битва подводит черту под всем походом, а не под
            // одним владением: её итог и есть итог кампании.
            if (state.inFinalBattle)
            {
                state.victory = victory;
                state.inFinalBattle = false;

                CampaignRun.Finish();

                Debug.Log(victory
                    ? "[Кампания] Последняя битва выиграна. Королевство отстояно."
                    : "[Кампания] Последняя битва проиграна.");

                return;
            }

            int node = state.currentNode;

            if (victory)
            {
                state.MarkCleared(node);
                GrantRewards(node);
            }
            else
            {
                state.MarkLost(node);
            }

            // Армии не осталось — поход окончен, чем бы ни кончился бой.
            if (!state.HasArmy)
            {
                CampaignRun.Finish();
                Debug.Log("[Кампания] Армия потеряна. Поход окончен.");
                return;
            }

            // Дорог вперёд больше нет — впереди последняя битва.
            // Кампанию не закрываем: игрок сам решит, идти ли в неё.
            CampaignRun.Save();
        }

        // ---------- Награды ----------

        /// <summary>
        /// Выдать награды владения.
        ///
        /// Командир кладётся в список свободных, а не назначается сам:
        /// кому его дать и какую специализацию выбрать — решение игрока,
        /// и принимается оно в лагере, а не на бегу.
        /// </summary>
        private static void GrantRewards(int node)
        {
            HoldingDefinition holding = Map?.GetNode(node)?.holding;

            if (holding == null)
                return;

            CampaignState state = CampaignRun.State;

            foreach (HoldingReward reward in holding.rewards)
            {
                if (reward == null)
                    continue;

                switch (reward.kind)
                {
                    case RewardKind.Prestige:
                        PlayerProgress.AddPrestige(reward.prestige);
                        break;

                    case RewardKind.Commander:
                        string commander = PickFreeCommander();

                        if (!string.IsNullOrEmpty(commander))
                            state.freeCommanders.Add(commander);
                        break;

                    case RewardKind.BuildingCard:
                        GrantBuildingCard(state, reward.buildingCard);
                        break;

                    case RewardKind.Boon:
                        // Усиления ещё не описаны — награда молча пропускается,
                        // чтобы владение не выдавало пустоту как настоящий приз.
                        Debug.Log($"[Кампания] Усиление «{reward.boonId}» пока не реализовано.");
                        break;
                }
            }

            CampaignRun.Save();
        }

        /// <summary>
        /// Выдать чертёж постройки.
        ///
        /// Карточка кладётся в колоду сразу и сверх её потолка. Иначе
        /// награда не работала бы вовсе: колода собрана перед походом,
        /// и добытый в бою чертёж просто ждал бы следующей кампании —
        /// то есть был бы не наградой, а обещанием.
        /// </summary>
        private static void GrantBuildingCard(CampaignState state, Building.BuildingDefinition card)
        {
            if (card == null)
                return;

            if (!state.deck.Contains(card.name))
                state.deck.Add(card.name);

            state.deckCards.Add(card.name);

            Debug.Log($"[Кампания] Чертёж «{card.DisplayName}» добавлен в колоду.");
        }

        /// <summary>
        /// Командир, которого ещё не выдавали. Пусто — пул кончился,
        /// и это повод его расширить, а не ошибка.
        /// </summary>
        private static string PickFreeCommander()
        {
            CampaignRules rules = CampaignRules.Current;

            if (rules == null)
                return string.Empty;

            CampaignState state = CampaignRun.State;
            var candidates = new List<string>();

            foreach (CommanderDefinition commander in rules.commanderPool)
            {
                if (commander == null || state.freeCommanders.Contains(commander.name))
                    continue;

                if (!IsCommanderInService(state, commander.name))
                    candidates.Add(commander.name);
            }

            if (candidates.Count == 0)
            {
                Debug.LogWarning("[Кампания] Пул командиров исчерпан — награда пропала впустую.");
                return string.Empty;
            }

            return candidates[Random.Range(0, candidates.Count)];
        }

        private static bool IsCommanderInService(CampaignState state, string id)
        {
            foreach (SquadRecord squad in state.squads)
            {
                if (squad != null && squad.commanderId == id)
                    return true;
            }

            return false;
        }
    }
}
