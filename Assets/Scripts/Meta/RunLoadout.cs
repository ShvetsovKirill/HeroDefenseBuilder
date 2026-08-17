using System.Collections.Generic;
using UnityEngine;
using HeroDefense.Building;

namespace HeroDefense.Meta
{
    /// <summary>
    /// Колода карточек застройки на забег (D121–D124).
    ///
    /// Два разных понятия, которые легко перепутать:
    ///
    /// ОТКРЫТИЕ типа — навсегда, за престиж. Хранится в PlayerProgress
    /// и переживает выход из игры. Открыл второй тип башни — он твой.
    ///
    /// КОЛОДА — что взято именно в этот забег. Живёт только до конца забега
    /// и собирается заново перед следующим. Не сохраняется намеренно: иначе
    /// каждый забег начинался бы с восстановления колоды, и престиж стал бы
    /// оброком вместо прогресса.
    ///
    /// Карточка даёт ПРАВО построить экземпляр, золото остаётся ценой (D122).
    ///
    /// Статический класс, как PlayerProgress: колода собирается в замке,
    /// а читается в бою — то есть переживает смену сцены.
    /// </summary>
    public static class RunLoadout
    {
        /// <summary>
        /// Сколько карточек можно взять. Больше числа слотов намеренно (D124):
        /// если колода равна девяти слотам, игрок просто ставит всё взятое,
        /// и выбор «башня или экономика» уезжает из боя в меню.
        /// </summary>
        public const int DefaultDeckSize = 12;

        private const string UnlockPrefix = "unlock.building.";

        public sealed class DeckEntry
        {
            public BuildingDefinition definition;
            public int count;
        }

        private static readonly List<DeckEntry> Deck = new();

        /// <summary>Что взято в забег. Пусто — сцена запущена напрямую.</summary>
        public static IReadOnlyList<DeckEntry> Entries => Deck;

        /// <summary>Сколько карточек уже набрано.</summary>
        public static int TotalCards
        {
            get
            {
                int total = 0;

                foreach (DeckEntry entry in Deck)
                    total += entry.count;

                return total;
            }
        }

        public static int MaxCards => DefaultDeckSize + PlayerProgress.GetUpgradeLevel("deck.size");

        public static int RemainingSlots => Mathf.Max(0, MaxCards - TotalCards);

        /// <summary>
        /// Собрана ли колода. Ложь означает, что игрок запустил сцену Battle
        /// напрямую из редактора — тогда лимиты берутся из ассетов, как раньше,
        /// и тестировать бой можно без похода через замок.
        /// </summary>
        public static bool IsConfigured => Deck.Count > 0;

        // ---------- Открытие типов ----------

        /// <summary>
        /// Открыт ли тип. Базовые открыты с начала — иначе первый забег
        /// играть нечем.
        /// </summary>
        public static bool IsUnlocked(BuildingDefinition definition)
        {
            if (definition == null)
                return false;

            if (definition.unlockedFromStart)
                return true;

            return PlayerProgress.GetUpgradeLevel(UnlockId(definition)) > 0;
        }

        /// <summary>
        /// Открыть тип за престиж. Возвращает false, если не хватило
        /// или тип уже открыт.
        /// </summary>
        public static bool TryUnlock(BuildingDefinition definition)
        {
            if (definition == null || IsUnlocked(definition))
                return false;

            if (!PlayerProgress.TrySpendPrestige(definition.unlockCost))
                return false;

            PlayerProgress.RaiseUpgradeLevel(UnlockId(definition));

            return true;
        }

        /// <summary>
        /// Идентификатор для сохранения. Берётся из имени ассета: отдельное
        /// поле пришлось бы заполнять руками в каждом ассете и однажды
        /// оказалось бы пустым.
        ///
        /// ⚠️ Переименование ассета сбросит открытие этого типа.
        /// </summary>
        private static string UnlockId(BuildingDefinition definition)
        {
            return UnlockPrefix + definition.name;
        }

        // ---------- Сборка колоды ----------

        public static int GetCount(BuildingDefinition definition)
        {
            DeckEntry entry = Find(definition);

            return entry != null ? entry.count : 0;
        }

        /// <summary>Добавить карточку в колоду. Ложь — нет места или тип закрыт.</summary>
        public static bool TryAddCard(BuildingDefinition definition)
        {
            if (definition == null || !IsUnlocked(definition) || RemainingSlots <= 0)
                return false;

            DeckEntry entry = Find(definition);

            if (entry == null)
            {
                entry = new DeckEntry { definition = definition, count = 0 };
                Deck.Add(entry);
            }

            entry.count++;

            return true;
        }

        public static bool TryRemoveCard(BuildingDefinition definition)
        {
            DeckEntry entry = Find(definition);

            if (entry == null || entry.count <= 0)
                return false;

            entry.count--;

            if (entry.count == 0)
                Deck.Remove(entry);

            return true;
        }

        public static void Clear() => Deck.Clear();

        /// <summary>
        /// Заполнить колоду поровну открытыми типами. Нужна для случая,
        /// когда игрок жмёт «в бой», ничего не выбрав: пустая колода
        /// означала бы забег, в котором нельзя построить ничего.
        /// </summary>
        public static void FillWithDefaults(IEnumerable<BuildingDefinition> catalog)
        {
            Clear();

            if (catalog == null)
                return;

            var unlocked = new List<BuildingDefinition>();

            foreach (BuildingDefinition definition in catalog)
            {
                if (IsUnlocked(definition))
                    unlocked.Add(definition);
            }

            if (unlocked.Count == 0)
                return;

            // По кругу, пока не кончатся места: так колода получается
            // сбалансированной по типам, а не забитой первым в списке.
            int guard = 0;

            while (RemainingSlots > 0 && guard < 1000)
            {
                foreach (BuildingDefinition definition in unlocked)
                {
                    if (RemainingSlots <= 0)
                        break;

                    TryAddCard(definition);
                }

                guard++;
            }
        }

        private static DeckEntry Find(BuildingDefinition definition)
        {
            if (definition == null)
                return null;

            foreach (DeckEntry entry in Deck)
            {
                if (entry.definition == definition)
                    return entry;
            }

            return null;
        }

        /// <summary>Сводка для отладки.</summary>
        public static string Describe()
        {
            if (Deck.Count == 0)
                return "Колода не собрана — лимиты берутся из ассетов.";

            var text = new System.Text.StringBuilder();

            text.AppendLine($"Колода: {TotalCards} / {MaxCards}");

            foreach (DeckEntry entry in Deck)
                text.AppendLine($"  {entry.definition.displayName} × {entry.count}");

            return text.ToString();
        }
    }
}
