using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeroDefense.UI
{
    /// <summary>
    /// Держит подпись кнопки внутри её подложки, с полями по краям.
    ///
    /// Почему не штатное автосжатие TMP: оно уменьшает кегль, только когда
    /// текст не влезает ПО ВЫСОТЕ. Подпись кнопки идёт одной строкой, по
    /// высоте влезает всегда — и автосжатие не срабатывает, сколько бы
    /// текст ни вылезал вбок. Проверено на «March Out»: 375 точек текста
    /// в кнопке шириной 320, автосжатие включено, кегль не изменился.
    ///
    /// Поэтому кегль считается сам: во сколько раз текст шире доступного
    /// места, во столько же уменьшается размер.
    ///
    /// Компонент вешает <see cref="ButtonTextFitter"/> при загрузке сцены,
    /// руками ставить не нужно.
    /// </summary>
    [RequireComponent(typeof(TMP_Text))]
    [DisallowMultipleComponent]
    public sealed class ButtonLabelFit : MonoBehaviour
    {
        /// <summary>Ниже этой доли исходного кегля не опускаемся: станет нечитаемо.</summary>
        private const float MinScale = 0.55f;

        /// <summary>Сколько раз уточняем кегль. Трёх хватает с запасом.</summary>
        private const int MaxPasses = 3;

        private TMP_Text _label;
        private RectTransform _frame;

        private float _baseSize;
        private string _lastText;
        private float _lastWidth;

        /// <summary>
        /// Задать рамку, внутри которой держим подпись. Обычно это подложка
        /// кнопки: она бывает меньше объекта с компонентом Button.
        /// </summary>
        public void Bind(RectTransform frame)
        {
            _frame = frame;
            Refit();
        }

        private void Awake()
        {
            _label = GetComponent<TMP_Text>();

            // Штатное автосжатие мешает: оно перебивает выставленный кегль
            // своим, посчитанным по высоте.
            _label.enableAutoSizing = false;
            _label.textWrappingMode = TextWrappingModes.NoWrap;

            _baseSize = _label.fontSize;
        }

        /// <summary>
        /// LateUpdate, а не событие: текст подписи меняют и локализация,
        /// и сами экраны, каждый по-своему. Проверка дешёвая — сравнение
        /// строки и ширины, — а пересчёт идёт только когда что-то изменилось.
        /// </summary>
        private void LateUpdate()
        {
            if (_label == null)
                return;

            float width = Available;

            if (_label.text == _lastText && Mathf.Approximately(width, _lastWidth))
                return;

            Refit();
        }

        private float Available
        {
            get
            {
                RectTransform area = _frame != null ? _frame : _label.rectTransform;

                return area.rect.width - _label.margin.x - _label.margin.z;
            }
        }

        private void Refit()
        {
            if (_label == null)
                return;

            _lastText = _label.text;
            _lastWidth = Available;

            if (_lastWidth <= 1f || string.IsNullOrEmpty(_lastText))
                return;

            // Меряем при исходном кегле: иначе каждый пересчёт шёл бы от уже
            // уменьшенного размера, и подпись ужималась бы всё сильнее.
            _label.fontSize = _baseSize;
            _label.ForceMeshUpdate();

            // Уточняем в несколько проходов: ширина текста не строго
            // пропорциональна кеглю — мешают кернинг и округление глифов.
            // Один проход давал 261 точку там, где помещается 256.
            for (int pass = 0; pass < MaxPasses; pass++)
            {
                float needed = _label.preferredWidth;

                if (needed <= _lastWidth)
                    return;

                float scale = Mathf.Max(_label.fontSize / _baseSize * (_lastWidth / needed), MinScale);

                _label.fontSize = _baseSize * scale;
                _label.ForceMeshUpdate();

                // Упёрлись в предел читаемости — дальше жать нельзя.
                if (Mathf.Approximately(scale, MinScale))
                    return;
            }
        }
    }
}
