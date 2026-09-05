using System.Text;
using HeroDefense.Localization;

namespace HeroDefense.UI
{
    /// <summary>
    /// Что игрок должен знать об управлении.
    ///
    /// Один список на всю игру: подсказка в бою, экран паузы и раздел
    /// в меню читают отсюда. Иначе три копии разъедутся при первой же
    /// смене клавиши, и хуже всего то, что заметит это игрок, а не автор.
    ///
    /// Строки не захардкожены прямо в панели: у каждой есть ключ перевода,
    /// а текст рядом — запасной вариант, пока ключа нет в таблице.
    /// </summary>
    public static class ControlsInfo
    {
        /// <summary>Одна строка подсказки: клавиша и что она делает.</summary>
        public readonly struct Line
        {
            /// <summary>Как выглядит клавиша: «WASD», «1–4», «F».</summary>
            public readonly string Keys;

            /// <summary>
            /// Код картинки клавиши. По нему панель ищет спрайт в оформлении;
            /// не нашла — покажет текст из <see cref="Keys"/>.
            ///
            /// Отдельный короткий код, а не поиск по самой надписи: надпись
            /// содержит тире и пробелы, и привязка к ней ломалась бы
            /// от любой правки вёрстки.
            /// </summary>
            public readonly string IconId;

            /// <summary>Ключ описания в таблице переводов.</summary>
            public readonly string DescriptionKey;

            /// <summary>Описание, если ключа нет в таблице.</summary>
            public readonly string DescriptionFallback;

            public Line(string keys, string iconId, string descriptionKey, string descriptionFallback)
            {
                Keys = keys;
                IconId = iconId;
                DescriptionKey = descriptionKey;
                DescriptionFallback = descriptionFallback;
            }

            /// <summary>Описание на текущем языке.</summary>
            public string Description => Loc.GetOrFallback(DescriptionKey, DescriptionFallback);
        }

        /// <summary>
        /// Порядок важен: первым идёт то, без чего игрок вообще не сдвинется
        /// с места, последним — то, что можно узнать позже.
        /// </summary>
        public static readonly Line[] Lines =
        {
            new("WASD", "wasd", "controls.move", "Двигать короля"),
            new("1 – 4", "1234", "controls.squad", "Выбрать отряд"),
            new("F", "f", "controls.flag", "Воткнуть флаг там, где стоишь"),
            new("Esc / P", "esc", "controls.pause", "Пауза"),
            new("Enter", "enter", "controls.restart", "Начать забег заново после конца"),
        };

        /// <summary>
        /// Подсказка о строительстве. Отдельно от списка клавиш: у неё нет
        /// своей кнопки — постройка открывается подъездом к свободному месту,
        /// и это ровно то, чего никто не угадывает сам.
        /// </summary>
        public static string BuildHint =>
            Loc.GetOrFallback("controls.build", "Подъедь к свободной площадке — откроется список построек");

        /// <summary>Готовый текст списка клавиш: по строке на клавишу.</summary>
        public static string BuildText()
        {
            var builder = new StringBuilder();

            foreach (Line line in Lines)
                builder.AppendLine($"<b>{line.Keys}</b>   {line.Description}");

            builder.AppendLine();
            builder.Append(BuildHint);

            return builder.ToString();
        }
    }
}
