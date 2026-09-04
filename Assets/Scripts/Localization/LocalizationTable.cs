using System;
using UnityEngine;

namespace HeroDefense.Localization
{
    /// <summary>
    /// Таблица переводов: ключ и по строке на каждый язык.
    ///
    /// ScriptableObject, а не файл в коде: тексты — это контент, их правит
    /// не программист (D48). Дизайнер меняет формулировку в инспекторе,
    /// пересборка не нужна.
    ///
    /// Ассет обязан лежать в папке Resources и называться так, как ждёт
    /// <see cref="Loc"/> — иначе он не загрузится и игра покажет голые ключи.
    /// </summary>
    [CreateAssetMenu(menuName = "HeroDefense/Таблица переводов", fileName = "LocalizationTable")]
    public sealed class LocalizationTable : ScriptableObject
    {
        /// <summary>Одна строка перевода.</summary>
        [Serializable]
        public sealed class Entry
        {
            [Tooltip("Ключ, по которому строка запрашивается из кода и из LocalizedText. " +
                     "Менять нельзя: ключ прописан в сценах, и правка осиротит надпись.")]
            public string key;

            [Tooltip("Английский текст. Язык по умолчанию — если он пуст, " +
                     "надпись покажет сам ключ, и пропуск сразу видно.")]
            [TextArea(1, 3)]
            public string english;

            [Tooltip("Русский текст. Пусто — подставится английский, " +
                     "а не пустота: недопереведённая игра остаётся играбельной.")]
            [TextArea(1, 3)]
            public string russian;
        }

        [Tooltip("Все строки игры. Порядок значения не имеет, поиск идёт по ключу.")]
        public Entry[] entries = Array.Empty<Entry>();

        /// <summary>
        /// Текст на нужном языке. Пустой перевод откатывается на английский,
        /// пустой английский — на сам ключ.
        /// </summary>
        public string Resolve(Entry entry, Language language)
        {
            if (entry == null)
                return string.Empty;

            string text = language == Language.Russian ? entry.russian : entry.english;

            if (!string.IsNullOrEmpty(text))
                return text;

            return !string.IsNullOrEmpty(entry.english) ? entry.english : entry.key;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Проверка на повторяющиеся ключи. Дубликат означает, что один
        /// из двух переводов никогда не покажется, а какой именно —
        /// зависит от порядка в массиве.
        /// </summary>
        private void OnValidate()
        {
            var seen = new System.Collections.Generic.HashSet<string>();

            foreach (Entry entry in entries)
            {
                if (entry == null || string.IsNullOrEmpty(entry.key))
                    continue;

                if (!seen.Add(entry.key))
                    Debug.LogWarning($"[Локализация] Ключ «{entry.key}» встречается дважды.", this);
            }
        }
#endif
    }
}
