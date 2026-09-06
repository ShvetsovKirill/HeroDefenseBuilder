using System.Collections.Generic;
using UnityEngine;
using HeroDefense.Building;
using HeroDefense.Meta;

namespace HeroDefense.Campaign
{
    /// <summary>
    /// Перекладывает колоду между походом и боем.
    ///
    /// Зачем отдельный класс: колода живёт в двух видах и ни один
    /// не годится вместо другого. В походе это список имён в сохранении —
    /// иначе трёхчасовая кампания теряла бы колоду при выходе из игры.
    /// В бою это <see cref="RunLoadout"/> со ссылками на ассеты — искать
    /// постройку по имени в каждом кадре нельзя.
    ///
    /// Перевод между ними нужен в двух местах — на экране сборки и перед
    /// боем, — и написан он здесь один раз, чтобы две копии однажды
    /// не разошлись.
    /// </summary>
    public static class CampaignBuildDeck
    {
        private static readonly List<BuildingDefinition> Buffer = new();

        /// <summary>
        /// Разложить сохранённую колоду по ассетам и отдать её бою.
        /// Ничего не делает вне похода: боевую сцену запускают напрямую
        /// из редактора, и там колода берётся из ассетов, как раньше.
        /// </summary>
        public static void RestoreFromState()
        {
            CampaignRules rules = CampaignRules.Current;
            CampaignState state = CampaignRun.State;

            if (rules == null)
                return;

            // Колода прошлого похода не должна протечь в новый: статика
            // переживает смену сцены, а поход — нет.
            if (!state.HasDeck)
            {
                if (CampaignRun.IsActive)
                    RunLoadout.Clear();

                return;
            }

            Buffer.Clear();

            foreach (string id in state.deckCards)
            {
                BuildingDefinition building = rules.FindBuilding(id);

                if (building != null)
                    Buffer.Add(building);
                else
                    Debug.LogWarning($"[Кампания] Постройка «{id}» из колоды не найдена — карточка пропала.");
            }

            RunLoadout.Restore(Buffer);
        }

        /// <summary>
        /// Записать собранную колоду в поход. Имена, а не ссылки:
        /// сохранение не должно зависеть от внутренних идентификаторов
        /// Unity.
        /// </summary>
        public static void SaveToState()
        {
            CampaignState state = CampaignRun.State;

            state.deckCards.Clear();

            foreach (RunLoadout.DeckEntry entry in RunLoadout.Entries)
            {
                if (entry.definition == null)
                    continue;

                for (int i = 0; i < entry.count; i++)
                    state.deckCards.Add(entry.definition.name);
            }

            CampaignRun.Save();
        }
    }
}
