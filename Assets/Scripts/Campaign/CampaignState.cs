using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroDefense.Campaign
{
    /// <summary>
    /// Состояние кампании: всё, что переживает бой и живёт между
    /// владениями.
    ///
    /// Это фундамент структуры. Карта его показывает, забег меняет,
    /// лагерь создаёт заново. Поэтому здесь нет ни одной ссылки на сцену,
    /// на UI или на боевые классы — только данные, которые можно записать
    /// в файл и прочитать обратно.
    ///
    /// Сериализуемый класс, а не статика: кампанию нужно уметь сохранить
    /// посередине (при трёх часах это обязательно) и загрузить снова.
    /// Доступ к текущей — через <see cref="CampaignRun"/>.
    /// </summary>
    [Serializable]
    public sealed class CampaignState
    {
        [Tooltip("Имя ассета карты, по которой идёт кампания.")]
        public string mapId;

        [Tooltip("Индекс узла, где игрок стоит сейчас. -1 — путь ещё не начат.")]
        public int currentNode = -1;

        [Tooltip("Узлы, которые уже пройдены. Нужны для отрисовки карты.")]
        public List<int> visitedNodes = new();

        [Tooltip("Узлы, где бой проигран. Владение потеряно, но путь продолжается.")]
        public List<int> lostNodes = new();

        [Tooltip("Отряды армии. Слот — это клавиша 1–9.")]
        public List<SquadRecord> squads = new();

        [Tooltip("Командиры без отряда: получены в награду, но ещё не назначены.")]
        public List<string> freeCommanders = new();

        [Tooltip("Типы построек, доступные в колоде этой кампании. " +
                 "Имена ассетов: сохранение не хранит ссылок.")]
        public List<string> deck = new();

        [Tooltip("Собранная колода: по одному имени на карточку. " +
                 "Повтор означает вторую карточку того же типа.")]
        public List<string> deckCards = new();

        [Min(0)]
        [Tooltip("Сколько владений пройдено. Для показа прогресса и для сложности.")]
        public int holdingsCleared;

        [Tooltip("Кампания завершена: последняя битва позади или армия кончилась.")]
        public bool finished;

        [Tooltip("Идёт последняя битва. Она не узел карты — путь к ней уже пройден.")]
        public bool inFinalBattle;

        [Tooltip("Кампания выиграна. Читается только когда finished.")]
        public bool victory;

        // ---------- Армия ----------

        /// <summary>Сколько отрядов ещё в строю.</summary>
        public int AliveSquadCount
        {
            get
            {
                int count = 0;

                foreach (SquadRecord squad in squads)
                {
                    if (squad != null && !squad.wipedOut)
                        count++;
                }

                return count;
            }
        }

        /// <summary>
        /// Кампания продолжается, пока есть кому воевать. Потеря армии —
        /// один из двух способов её закончить; второй — последняя битва.
        /// </summary>
        public bool HasArmy => AliveSquadCount > 0;

        /// <summary>Отряд по номеру слота. Null, если такого нет.</summary>
        public SquadRecord GetSquad(int slot)
        {
            foreach (SquadRecord squad in squads)
            {
                if (squad != null && squad.slot == slot)
                    return squad;
            }

            return null;
        }

        /// <summary>
        /// Завести новый отряд. Слот берётся первый свободный, начиная
        /// с единицы: слоты — это клавиши, и дырки в них игроку не нужны.
        /// </summary>
        public SquadRecord AddSquad(int size)
        {
            int slot = 1;

            while (GetSquad(slot) != null)
                slot++;

            var record = new SquadRecord { slot = slot, size = Mathf.Max(1, size) };

            squads.Add(record);

            return record;
        }

        // ---------- Колода ----------

        /// <summary>
        /// Собрана ли колода. Пустая означает, что игрок ещё не выходил
        /// в поход: без карточек строить в бою нечего.
        /// </summary>
        public bool HasDeck => deckCards.Count > 0;

        /// <summary>Сколько карточек этого типа взято.</summary>
        public int CountCards(string buildingId)
        {
            int count = 0;

            foreach (string card in deckCards)
            {
                if (card == buildingId)
                    count++;
            }

            return count;
        }

        // ---------- Путь ----------

        /// <summary>Владение пройдено: запоминаем и двигаем счётчик.</summary>
        public void MarkCleared(int node)
        {
            if (!visitedNodes.Contains(node))
                visitedNodes.Add(node);

            holdingsCleared++;
            currentNode = node;
        }

        /// <summary>
        /// Владение потеряно. Путь всё равно продолжается: проигранный
        /// бой стоит награды и, возможно, отряда, но не кампании.
        /// </summary>
        public void MarkLost(int node)
        {
            if (!visitedNodes.Contains(node))
                visitedNodes.Add(node);

            if (!lostNodes.Contains(node))
                lostNodes.Add(node);

            currentNode = node;
        }

        /// <summary>Был ли здесь игрок.</summary>
        public bool IsVisited(int node) => visitedNodes.Contains(node);

        /// <summary>Проигран ли бой в этом узле.</summary>
        public bool IsLost(int node) => lostNodes.Contains(node);

        /// <summary>
        /// Итог похода одной строкой. Нужен экрану лагеря: игрок
        /// возвращается туда и должен сразу понять, чем всё кончилось.
        /// </summary>
        public string DescribeOutcome()
        {
            if (!finished)
                return string.Empty;

            if (victory)
                return "Королевство отстояно";

            return HasArmy ? "Поход прерван" : "Армия потеряна";
        }
    }
}
