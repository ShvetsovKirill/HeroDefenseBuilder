using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroDefense.Meta
{
    /// <summary>
    /// Прогресс игрока между забегами: престиж и купленные улучшения (D92).
    ///
    /// Статический класс без сцены — намеренно. Прогресс живёт дольше любой
    /// сцены: его читает замок, пишет конец забега, а между ними идёт полная
    /// выгрузка Battle. Объект в сцене пришлось бы тащить через
    /// DontDestroyOnLoad и ловить гонки инициализации (на них уже наступали).
    ///
    /// Хранилище — PlayerPrefs. Не потому что это лучшее решение, а потому
    /// что оно одинаково работает в редакторе и в WebGL. Когда появится
    /// IPlatformServices (D24), сюда подставится облачное сохранение —
    /// менять придётся только Load и Save, остальной код о хранилище не знает.
    /// </summary>
    public static class PlayerProgress
    {
        private const string StorageKey = "herodefense.progress.v1";

        /// <summary>
        /// Слепок прогресса для сериализации.
        ///
        /// Список пар вместо словаря: JsonUtility не умеет Dictionary,
        /// а тащить стороннюю библиотеку ради одного файла незачем.
        /// </summary>
        [Serializable]
        private sealed class ProgressData
        {
            public int prestige;
            public int runsCompleted;
            public int runsWon;
            public int bestWaveReached;

            public List<UpgradeEntry> upgrades = new();
        }

        [Serializable]
        private sealed class UpgradeEntry
        {
            public string id;
            public int level;
        }

        private static ProgressData _data;

        /// <summary>
        /// Ленивая загрузка: прогресс могут прочитать из Awake любого экрана,
        /// а порядок Awake между объектами Unity не гарантирует.
        /// </summary>
        private static ProgressData Data
        {
            get
            {
                if (_data == null)
                    Load();

                return _data;
            }
        }

        /// <summary>Престиж изменился. Для экрана замка.</summary>
        public static event Action<int> PrestigeChanged;

        /// <summary>Уровень улучшения изменился. Аргументы: id, новый уровень.</summary>
        public static event Action<string, int> UpgradeChanged;

        // ---------- Престиж ----------

        public static int Prestige => Data.prestige;

        public static int RunsCompleted => Data.runsCompleted;

        public static int RunsWon => Data.runsWon;

        public static int BestWaveReached => Data.bestWaveReached;

        public static void AddPrestige(int amount)
        {
            if (amount <= 0)
                return;

            Data.prestige += amount;

            Save();
            PrestigeChanged?.Invoke(Data.prestige);
        }

        public static bool CanAfford(int cost) => Data.prestige >= cost;

        /// <summary>Попытка потратить. Возвращает false, если не хватает.</summary>
        public static bool TrySpendPrestige(int cost)
        {
            if (cost < 0 || !CanAfford(cost))
                return false;

            Data.prestige -= cost;

            Save();
            PrestigeChanged?.Invoke(Data.prestige);

            return true;
        }

        // ---------- Улучшения ----------

        /// <summary>
        /// Текущий уровень улучшения. Ноль — не куплено.
        /// Уровнями, а не флагами: ветки прокачки многоступенчатые,
        /// и «+2 к урону короля трижды» должно храниться одним числом.
        /// </summary>
        public static int GetUpgradeLevel(string id)
        {
            if (string.IsNullOrEmpty(id))
                return 0;

            UpgradeEntry entry = FindEntry(id);

            return entry != null ? entry.level : 0;
        }

        /// <summary>
        /// Поднять уровень улучшения на единицу. Престиж здесь НЕ списывается:
        /// цену знает ассет улучшения, а не хранилище. Разделено, чтобы
        /// хранилище не зависело от правил ценообразования.
        /// </summary>
        public static void RaiseUpgradeLevel(string id)
        {
            if (string.IsNullOrEmpty(id))
                return;

            UpgradeEntry entry = FindEntry(id);

            if (entry == null)
            {
                entry = new UpgradeEntry { id = id, level = 0 };
                Data.upgrades.Add(entry);
            }

            entry.level++;

            Save();
            UpgradeChanged?.Invoke(id, entry.level);
        }

        private static UpgradeEntry FindEntry(string id)
        {
            foreach (UpgradeEntry entry in Data.upgrades)
            {
                if (entry.id == id)
                    return entry;
            }

            return null;
        }

        // ---------- Итоги забега ----------

        /// <summary>
        /// Записать факт завершения забега. Победа или поражение — оба
        /// считаются пройденными: провал часть цикла, а не поломка (D79).
        /// </summary>
        public static void RegisterRunFinished(bool victory, int waveReached)
        {
            Data.runsCompleted++;

            if (victory)
                Data.runsWon++;

            if (waveReached > Data.bestWaveReached)
                Data.bestWaveReached = waveReached;

            Save();
        }

        // ---------- Хранилище ----------

        public static void Load()
        {
            string json = PlayerPrefs.GetString(StorageKey, string.Empty);

            if (string.IsNullOrEmpty(json))
            {
                _data = new ProgressData();
                return;
            }

            try
            {
                _data = JsonUtility.FromJson<ProgressData>(json) ?? new ProgressData();
            }
            catch (Exception exception)
            {
                // Битое сохранение не должно мешать играть: начинаем с нуля
                // и сообщаем в консоль, чтобы это не выглядело как пропажа.
                Debug.LogWarning($"[Прогресс] Сохранение повреждено, начинаем заново: {exception.Message}");
                _data = new ProgressData();
            }

            _data.upgrades ??= new List<UpgradeEntry>();
        }

        public static void Save()
        {
            if (_data == null)
                return;

            PlayerPrefs.SetString(StorageKey, JsonUtility.ToJson(_data));
            PlayerPrefs.Save();
        }

        /// <summary>Полный сброс. Для отладки и кнопки «начать заново».</summary>
        public static void ResetAll()
        {
            _data = new ProgressData();

            Save();
            PrestigeChanged?.Invoke(0);
        }

        /// <summary>Сводка для отладки.</summary>
        public static string Describe()
        {
            var text = new System.Text.StringBuilder();

            text.AppendLine($"Престиж: {Data.prestige}");
            text.AppendLine($"Забегов: {Data.runsCompleted} (побед {Data.runsWon}), лучшая волна {Data.bestWaveReached}");

            if (Data.upgrades.Count == 0)
            {
                text.AppendLine("Улучшений нет.");
                return text.ToString();
            }

            text.AppendLine("Улучшения:");

            foreach (UpgradeEntry entry in Data.upgrades)
                text.AppendLine($"  {entry.id} — уровень {entry.level}");

            return text.ToString();
        }
    }
}
