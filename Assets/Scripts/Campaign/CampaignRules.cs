using System;
using UnityEngine;
using HeroDefense.Building;

namespace HeroDefense.Campaign
{
    /// <summary>
    /// Числа кампании: с чем король выходит в поход и до чего может
    /// вырасти.
    ///
    /// Всё это — предметы спора с балансом, поэтому лежит в ассете (D48),
    /// а не в коде. Ассет один на игру и грузится из Resources.
    ///
    /// Главное правило записано здесь же: **потолок не выдаётся сразу.**
    /// Девять отрядов и большой отряд — то, к чему игрок идёт за престиж,
    /// а начинает он с горсткой людей.
    /// </summary>
    [CreateAssetMenu(fileName = "CampaignRules", menuName = "HeroDefense/Правила кампании")]
    public sealed class CampaignRules : ScriptableObject
    {
        [Header("Стартовая армия")]
        [Range(1, 9)]
        [Tooltip("Сколько отрядов у короля в самом начале, без улучшений.")]
        public int startingSquads = 2;

        [Min(1)]
        [Tooltip("Сколько бойцов в отряде на старте.")]
        public int startingSquadSize = 6;

        [Header("Потолки")]
        [Range(1, 9)]
        [Tooltip("Предел числа отрядов. Девять — по клавишам 1–9, дальше " +
                 "управлять нечем.")]
        public int maxSquads = 9;

        [Min(1)]
        [Tooltip("Предел размера отряда.")]
        public int maxSquadSize = 12;

        [Header("Колода на старте")]
        [Tooltip("Типы построек, с которыми начинается любая кампания. " +
                 "Остальное добывается по дороге.")]
        public BuildingDefinition[] startingDeck = Array.Empty<BuildingDefinition>();

        [Header("Карта")]
        [Tooltip("Карта, по которой идёт кампания. Пока одна на игру.")]
        public CampaignMapDefinition map;

        [Header("Командиры")]
        [Tooltip("Из кого набираются командиры в награду за баронства. " +
                 "Порядок неважен: выбор случайный среди ещё не выданных.")]
        public CommanderDefinition[] commanderPool = Array.Empty<CommanderDefinition>();

        /// <summary>
        /// Собрать стартовую армию и колоду для новой кампании.
        ///
        /// Отряды выдаются милицией: командиров король добудет в походе,
        /// и первое баронство должно ощущаться как приобретение.
        /// </summary>
        public void FillStartingArmy(CampaignState state)
        {
            if (state == null)
                return;

            state.squads.Clear();
            state.deck.Clear();

            int count = Mathf.Clamp(startingSquads, 1, maxSquads);

            for (int i = 0; i < count; i++)
                state.AddSquad(startingSquadSize);

            foreach (BuildingDefinition building in startingDeck)
            {
                if (building != null)
                    state.deck.Add(building.name);
            }
        }

        /// <summary>
        /// Постройка по имени ассета.
        ///
        /// Сохранение хранит имена, а не ссылки, — иначе файл сохранения
        /// зависел бы от внутренних идентификаторов Unity. Искать
        /// приходится в двух местах: стартовая колода и чертежи, которые
        /// раздают владения. Отдельного каталога нет намеренно: его
        /// пришлось бы заполнять руками, и он однажды разошёлся бы
        /// с наградами.
        /// </summary>
        public BuildingDefinition FindBuilding(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            foreach (BuildingDefinition building in startingDeck)
            {
                if (building != null && building.name == id)
                    return building;
            }

            if (map == null)
                return null;

            foreach (CampaignMapDefinition.Node node in map.nodes)
            {
                BuildingDefinition found = FindInRewards(node.holding, id);

                if (found != null)
                    return found;
            }

            return FindInRewards(map.finalBattle, id);
        }

        private static BuildingDefinition FindInRewards(HoldingDefinition holding, string id)
        {
            if (holding == null || holding.rewards == null)
                return null;

            foreach (HoldingReward reward in holding.rewards)
            {
                if (reward != null && reward.buildingCard != null && reward.buildingCard.name == id)
                    return reward.buildingCard;
            }

            return null;
        }

        /// <summary>
        /// Типы построек, из которых игрок собирает колоду в этом походе:
        /// стартовые плюс чертежи, добытые по дороге.
        /// </summary>
        public void FillAvailableBuildings(CampaignState state, System.Collections.Generic.List<BuildingDefinition> result)
        {
            result.Clear();

            if (state == null)
                return;

            foreach (string id in state.deck)
            {
                BuildingDefinition building = FindBuilding(id);

                if (building != null && !result.Contains(building))
                    result.Add(building);
            }
        }

        /// <summary>Командир по имени ассета. Null, если такого нет в пуле.</summary>
        public CommanderDefinition FindCommander(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            foreach (CommanderDefinition commander in commanderPool)
            {
                if (commander != null && commander.name == id)
                    return commander;
            }

            return null;
        }

        // ---------- Доступ ----------

        /// <summary>Имя ассета правил внутри Resources.</summary>
        private const string ResourcePath = "CampaignRules";

        private static CampaignRules _current;
        private static bool _loaded;

        /// <summary>
        /// Правила игры. Null означает, что ассет не создан — тогда
        /// кампанию начать нельзя, и об этом надо сказать вслух.
        /// </summary>
        public static CampaignRules Current
        {
            get
            {
                if (_loaded)
                    return _current;

                _loaded = true;
                _current = Resources.Load<CampaignRules>(ResourcePath);

                if (_current == null)
                {
                    Debug.LogError($"[Кампания] Не найден ассет Resources/{ResourcePath}. " +
                                   "Кампанию начать нельзя: неизвестно, с чем выходить в поход.");
                }

                return _current;
            }
        }

#if UNITY_EDITOR
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _current = null;
            _loaded = false;
        }
#endif
    }
}
