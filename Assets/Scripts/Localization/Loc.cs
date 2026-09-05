using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroDefense.Localization
{
    /// <summary>
    /// Доступ к переводам и текущий язык.
    ///
    /// Статический, как <c>PlayerProgress</c> и <c>RunLoadout</c>: язык
    /// выбирается в настройках, а читается во всех сценах, то есть обязан
    /// переживать их смену.
    ///
    /// Таблица подгружается сама при первом обращении. Это сделано ради
    /// запуска сцены напрямую из редактора: боевую сцену тестируют без
    /// похода через меню, и требовать инициализации откуда-то сверху
    /// значило бы получать голые ключи в каждом таком запуске.
    /// </summary>
    public static class Loc
    {
        /// <summary>Имя ассета таблицы внутри Resources. Без расширения.</summary>
        private const string TablePath = "LocalizationTable";

        private const string SaveKey = "settings.language";

        private static readonly Dictionary<string, LocalizationTable.Entry> Entries = new();

        private static LocalizationTable _table;
        private static Language _language = Language.English;
        private static bool _loaded;

        /// <summary>Язык сменился. Надписи перерисовывают себя по этому событию.</summary>
        public static event Action<Language> LanguageChanged;

        /// <summary>Текущий язык. По умолчанию английский (решение владельца).</summary>
        public static Language Current
        {
            get
            {
                EnsureLoaded();
                return _language;
            }
        }

        /// <summary>
        /// Сменить язык и запомнить выбор. Повторная установка того же языка
        /// ничего не делает: иначе каждый клик по кнопке перерисовывал бы
        /// весь интерфейс впустую.
        /// </summary>
        public static void SetLanguage(Language language)
        {
            EnsureLoaded();

            if (_language == language)
                return;

            _language = language;

            PlayerPrefs.SetInt(SaveKey, (int)language);
            PlayerPrefs.Save();

            LanguageChanged?.Invoke(language);
        }

        /// <summary>
        /// Текст по ключу. Неизвестный ключ возвращается как есть —
        /// на экране это выглядит как «menu.newgame» и находится сразу,
        /// в отличие от пустой строки.
        /// </summary>
        public static string Get(string key)
        {
            if (string.IsNullOrEmpty(key))
                return string.Empty;

            EnsureLoaded();

            if (_table == null)
                return key;

            return Entries.TryGetValue(key, out LocalizationTable.Entry entry)
                ? _table.Resolve(entry, _language)
                : key;
        }

        /// <summary>Текст по ключу с подстановкой, как в <c>string.Format</c>.</summary>
        public static string Get(string key, params object[] args)
        {
            string format = Get(key);

            // Кривая разметка в переводе не должна ронять игру: показываем
            // исходную строку, а причину пишем в консоль.
            try
            {
                return string.Format(format, args);
            }
            catch (FormatException)
            {
                Debug.LogWarning($"[Локализация] Плохая разметка у ключа «{key}»: {format}");
                return format;
            }
        }

        /// <summary>
        /// Перевод по ключу, а если ключа нет — готовая строка из ассета.
        ///
        /// Нужен контенту (D48): у построек, улучшений и королей названия
        /// лежат прямо в дефинициях. Пока ключ не проставлен, показывается
        /// старый текст — иначе перевод пришлось бы включать разом для всех
        /// ассетов, и любой пропущенный превращал бы карточку в «building.tower».
        /// </summary>
        public static string GetOrFallback(string key, string fallback)
        {
            if (string.IsNullOrEmpty(key))
                return fallback;

            EnsureLoaded();

            if (_table != null && Entries.TryGetValue(key, out LocalizationTable.Entry entry))
                return _table.Resolve(entry, _language);

            return string.IsNullOrEmpty(fallback) ? key : fallback;
        }

        /// <summary>Есть ли такой ключ. Для редакторских проверок.</summary>
        public static bool HasKey(string key)
        {
            EnsureLoaded();

            return !string.IsNullOrEmpty(key) && Entries.ContainsKey(key);
        }

        // ---------- Загрузка ----------

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;

            _loaded = true;

            _table = Resources.Load<LocalizationTable>(TablePath);

            if (_table == null)
            {
                Debug.LogError($"[Локализация] Не найден ассет Resources/{TablePath}. " +
                               "Интерфейс покажет ключи вместо текста.");
                return;
            }

            Entries.Clear();

            foreach (LocalizationTable.Entry entry in _table.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key))
                    continue;

                Entries[entry.key] = entry;
            }

            _language = (Language)PlayerPrefs.GetInt(SaveKey, (int)Language.English);
        }

#if UNITY_EDITOR
        /// <summary>
        /// Сброс статики при выходе из Play Mode. Без него отключённая
        /// перезагрузка домена сохранила бы таблицу и язык между запусками,
        /// и правки в ассете не подхватывались бы.
        /// </summary>
        [UnityEngine.RuntimeInitializeOnLoadMethod(UnityEngine.RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _loaded = false;
            _table = null;
            Entries.Clear();
            LanguageChanged = null;
        }
#endif
    }
}
