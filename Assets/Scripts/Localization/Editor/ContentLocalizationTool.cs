using System.Collections.Generic;
using System.Linq;
using System.Text;
using UnityEditor;
using UnityEngine;
using HeroDefense.Building;
using HeroDefense.King;
using HeroDefense.Meta;
using HeroDefense.Squads;
using HeroDefense.Waves;

namespace HeroDefense.Localization.Editor
{
    /// <summary>
    /// Проставляет ключи перевода дефинициям контента и пополняет таблицу.
    ///
    /// Зачем инструмент, а не ручная работа: названий построек, улучшений
    /// и королей — десятки, и каждое нужно в двух местах (ключ в ассете,
    /// строка в таблице). Вбивая это руками, легко ошибиться в одном
    /// символе, и надпись молча покажет голый ключ.
    ///
    /// Инструмент ничего не переводит. Он раскладывает уже написанные
    /// тексты по таблице и связывает их ключами — перевод на второй язык
    /// остаётся человеку.
    /// </summary>
    public static class ContentLocalizationTool
    {
        private const string TableAssetName = "LocalizationTable";

        /// <summary>
        /// Строки, которые задаёт код, а не ассеты: подписи служебных панелей.
        ///
        /// Их нельзя собрать обходом проекта — они лежат внутри классов
        /// как запасной текст. Здесь они попадают в таблицу, чтобы
        /// переводчик увидел их вместе с остальными.
        /// </summary>
        private static readonly (string Key, string Russian)[] CodeStrings =
        {
            ("upgrade.maxed", "макс"),

            ("pause.title", "Пауза"),
            ("pause.resume", "Продолжить"),
            ("pause.restart", "Начать заново"),
            ("pause.castle", "Выйти в замок"),

            ("controls.move", "Двигать короля"),
            ("controls.squad", "Выбрать отряд"),
            ("controls.flag", "Воткнуть флаг там, где стоишь"),
            ("controls.pause", "Пауза"),
            ("controls.restart", "Начать забег заново после конца"),
            ("controls.build", "Подъедь к свободной площадке — откроется список построек"),

            ("loading.title", "Загрузка"),

            ("wave.choose", "Выбери условие следующей волны"),
            ("wave.mod.count", "врагов"),
            ("wave.mod.health", "здоровья"),
            ("wave.mod.speed", "скорости"),
            ("wave.mod.gold", "золота"),
            ("wave.mod.density", "плотность"),

            ("castle.barracks", "Казармы"),
            ("castle.tech", "Технологии"),
            ("castle.king", "Король"),

            ("controls.title", "Управление"),
            ("common.back", "Назад"),
        };

        [MenuItem("Tools/Локализация/Проставить ключи контента")]
        private static void AssignKeys()
        {
            LocalizationTable table = FindTable();

            if (table == null)
                return;

            var entries = new Dictionary<string, LocalizationTable.Entry>();

            foreach (LocalizationTable.Entry entry in table.entries)
            {
                if (entry != null && !string.IsNullOrEmpty(entry.key))
                    entries[entry.key] = entry;
            }

            int assignedKeys = 0;
            int addedRows = 0;

            foreach (BuildingDefinition definition in LoadAll<BuildingDefinition>())
            {
                string slug = Slug(definition.name);

                assignedKeys += Assign(ref definition.nameKey, $"building.{slug}.name", definition);
                assignedKeys += Assign(ref definition.descriptionKey, $"building.{slug}.desc", definition);

                addedRows += Ensure(entries, definition.nameKey, definition.displayName);
                addedRows += Ensure(entries, definition.descriptionKey, definition.description);
            }

            foreach (BarracksDefinition definition in LoadAll<BarracksDefinition>())
            {
                string slug = Slug(definition.name);

                assignedKeys += Assign(ref definition.nameKey, $"barracks.{slug}.name", definition);
                addedRows += Ensure(entries, definition.nameKey, definition.displayName);
            }

            foreach (WaveModifier definition in LoadAll<WaveModifier>())
            {
                string slug = Slug(definition.name);

                assignedKeys += Assign(ref definition.nameKey, $"wavemod.{slug}.name", definition);
                assignedKeys += Assign(ref definition.descriptionKey, $"wavemod.{slug}.desc", definition);

                addedRows += Ensure(entries, definition.nameKey, definition.displayName);
                addedRows += Ensure(entries, definition.descriptionKey, definition.description);
            }

            foreach (KingDefinition definition in LoadAll<KingDefinition>())
            {
                string slug = Slug(definition.name);

                assignedKeys += Assign(ref definition.nameKey, $"king.{slug}.name", definition);
                assignedKeys += Assign(ref definition.descriptionKey, $"king.{slug}.desc", definition);

                addedRows += Ensure(entries, definition.nameKey, definition.displayName);
                addedRows += Ensure(entries, definition.descriptionKey, definition.description);
            }

            // У улучшений ключ строится из их собственного id, а не из имени
            // файла: id уже стабилен (его нельзя менять после релиза, иначе
            // обнулится купленное), и привязка к нему переживёт переименование ассета.
            foreach (UpgradeDefinition definition in LoadAll<UpgradeDefinition>())
            {
                var serialized = new SerializedObject(definition);

                string slug = Slug(definition.Id);

                assignedKeys += Assign(serialized, "nameKey", $"upgrade.{slug}.name");
                assignedKeys += Assign(serialized, "descriptionKey", $"upgrade.{slug}.desc");

                serialized.ApplyModifiedPropertiesWithoutUndo();

                addedRows += Ensure(entries, $"upgrade.{slug}.name", definition.DisplayName);
                addedRows += Ensure(entries, $"upgrade.{slug}.desc", definition.Description);
            }

            foreach ((string key, string russian) in CodeStrings)
                addedRows += Ensure(entries, key, russian);

            table.entries = entries.Values.ToArray();

            EditorUtility.SetDirty(table);
            AssetDatabase.SaveAssets();

            Debug.Log($"[Локализация] Проставлено ключей: {assignedKeys}. " +
                      $"Добавлено строк в таблицу: {addedRows}. " +
                      "Русский заполнен текстом из ассетов, английский пуст — " +
                      "проверь отчётом, что осталось перевести.", table);
        }

        [MenuItem("Tools/Локализация/Отчёт: что не переведено")]
        private static void Report()
        {
            LocalizationTable table = FindTable();

            if (table == null)
                return;

            var missingEnglish = new List<string>();
            var missingRussian = new List<string>();

            foreach (LocalizationTable.Entry entry in table.entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key))
                    continue;

                if (string.IsNullOrEmpty(entry.english))
                    missingEnglish.Add(entry.key);

                if (string.IsNullOrEmpty(entry.russian))
                    missingRussian.Add(entry.key);
            }

            var report = new StringBuilder();

            report.AppendLine($"[Локализация] Строк в таблице: {table.entries.Length}.");
            Append(report, "Без английского", missingEnglish);
            Append(report, "Без русского", missingRussian);

            // Ассеты без ключа опаснее пустого перевода: их текст вообще
            // не попадает в таблицу, и переводчик о них не узнает.
            var withoutKeys = new List<string>();

            foreach (BuildingDefinition definition in LoadAll<BuildingDefinition>())
            {
                if (string.IsNullOrEmpty(definition.nameKey))
                    withoutKeys.Add($"Постройка «{definition.name}»");
            }

            foreach (BarracksDefinition definition in LoadAll<BarracksDefinition>())
            {
                if (string.IsNullOrEmpty(definition.nameKey))
                    withoutKeys.Add($"Казарма «{definition.name}»");
            }

            foreach (KingDefinition definition in LoadAll<KingDefinition>())
            {
                if (string.IsNullOrEmpty(definition.nameKey))
                    withoutKeys.Add($"Король «{definition.name}»");
            }

            Append(report, "Ассеты без ключа названия", withoutKeys);

            Debug.Log(report.ToString(), table);
        }

        // ---------- Работа с таблицей ----------

        private static void Append(StringBuilder report, string title, List<string> lines)
        {
            report.AppendLine();
            report.AppendLine(lines.Count == 0 ? $"{title}: пусто." : $"{title} ({lines.Count}):");

            foreach (string line in lines)
                report.AppendLine($"  {line}");
        }

        /// <summary>
        /// Завести строку, если её ещё нет. Существующую не трогаем:
        /// перевод могли уже поправить руками, и перезапись инструментом
        /// откатила бы работу переводчика.
        /// </summary>
        private static int Ensure(Dictionary<string, LocalizationTable.Entry> entries,
                                  string key, string russianText)
        {
            if (string.IsNullOrEmpty(key) || entries.ContainsKey(key))
                return 0;

            entries[key] = new LocalizationTable.Entry
            {
                key = key,
                english = string.Empty,
                russian = russianText ?? string.Empty
            };

            return 1;
        }

        /// <summary>Проставить ключ публичному полю, если оно пусто.</summary>
        private static int Assign(ref string field, string key, Object owner)
        {
            if (!string.IsNullOrEmpty(field))
                return 0;

            field = key;
            EditorUtility.SetDirty(owner);

            return 1;
        }

        /// <summary>
        /// То же для приватного поля — через SerializedObject.
        /// У <c>UpgradeDefinition</c> поля закрыты, и напрямую их не выставить.
        /// </summary>
        private static int Assign(SerializedObject serialized, string propertyName, string key)
        {
            SerializedProperty property = serialized.FindProperty(propertyName);

            if (property == null || !string.IsNullOrEmpty(property.stringValue))
                return 0;

            property.stringValue = key;

            return 1;
        }

        // ---------- Поиск ассетов ----------

        private static LocalizationTable FindTable()
        {
            LocalizationTable table = LoadAll<LocalizationTable>().FirstOrDefault();

            if (table == null)
            {
                Debug.LogError($"[Локализация] В проекте нет ассета {TableAssetName}. " +
                               "Создай его через меню HeroDefense и положи в Resources.");
            }

            return table;
        }

        private static List<T> LoadAll<T>() where T : Object
        {
            return AssetDatabase.FindAssets($"t:{typeof(T).Name}")
                .Select(AssetDatabase.GUIDToAssetPath)
                .Select(AssetDatabase.LoadAssetAtPath<T>)
                .Where(asset => asset != null)
                .ToList();
        }

        /// <summary>
        /// Имя ассета в кусок ключа: нижний регистр, пробелы и подчёркивания
        /// в точки. «Tower Archer» превращается в «tower.archer».
        /// </summary>
        private static string Slug(string source)
        {
            if (string.IsNullOrEmpty(source))
                return "unnamed";

            var builder = new StringBuilder(source.Length);

            foreach (char symbol in source.ToLowerInvariant())
            {
                if (char.IsLetterOrDigit(symbol))
                    builder.Append(symbol);
                else if (builder.Length > 0 && builder[builder.Length - 1] != '.')
                    builder.Append('.');
            }

            return builder.ToString().Trim('.');
        }
    }
}
