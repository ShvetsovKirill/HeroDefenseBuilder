using System;
using TMPro;
using UnityEngine;

namespace HeroDefense.UI
{
    /// <summary>
    /// Оформление служебных экранов: рамки, кнопки, полоски.
    ///
    /// Экраны паузы, подсказки и загрузки собираются кодом, и без этого
    /// ассета они рисуются плоскими прямоугольниками — работает, но
    /// выглядит как отладка. Здесь спрайты подставляются в те же панели
    /// без единой правки кода.
    ///
    /// Ассет необязателен. Нет его — экраны рисуются заливкой, как раньше:
    /// служебная панель не должна ломаться из-за отсутствия картинки.
    ///
    /// Лежит в Resources и называется так, как ждёт <see cref="RuntimeUi"/>.
    /// </summary>
    [CreateAssetMenu(menuName = "HeroDefense/Оформление служебных экранов", fileName = "UiSkin")]
    public sealed class UiSkin : ScriptableObject
    {
        [Header("Панели")]
        [Tooltip("Фон модальной панели. Спрайт с настроенными границами 9-slice, " +
                 "иначе углы растянутся вместе с полем.")]
        public Sprite panel;

        [Tooltip("Фон небольшой панели, ближе к квадрату: подсказка управления.")]
        public Sprite panelCompact;

        [Header("Кнопки")]
        [Tooltip("Фон кнопки. Тоже 9-slice: подписи разной длины.")]
        public Sprite button;

        [Header("Полоска загрузки")]
        [Tooltip("Ложе полоски — пустая рамка.")]
        public Sprite barTrack;

        [Tooltip("Заполнение полоски.")]
        public Sprite barFill;

        [Header("Шрифт")]
        [Tooltip("Шрифт служебных экранов. Пусто — берётся шрифт по умолчанию " +
                 "из настроек TMP, а если и его нет — шрифт первой попавшейся " +
                 "надписи сцены.")]
        public TMP_FontAsset font;

        [Header("Логотип")]
        [Tooltip("Название игры для главного меню. Пусто — меню выглядит " +
                 "как раньше, без пустого места на месте логотипа.")]
        public Sprite logo;

        [Header("Клавиши")]
        [Tooltip("Картинки клавиш для подсказки управления. Пусто — подсказка " +
                 "покажет клавиши текстом, как раньше.")]
        public KeyIcon[] keyIcons = Array.Empty<KeyIcon>();

        [Header("Цвета")]
        [Tooltip("Цвет, которым красится спрайт панели. Белый — показать спрайт как есть. " +
                 "Без спрайта этим цветом рисуется плоская заливка.")]
        public Color panelTint = Color.white;

        [Tooltip("Цвет кнопки. Белый — спрайт как есть.")]
        public Color buttonTint = Color.white;

        /// <summary>Картинка одной клавиши.</summary>
        [Serializable]
        public sealed class KeyIcon
        {
            [Tooltip("Код клавиши из ControlsInfo: wasd, 1234, f, esc, enter. " +
                     "Не совпал — строка покажется текстом, это не ошибка.")]
            public string id;

            [Tooltip("Картинка клавиши.")]
            public Sprite sprite;
        }

        /// <summary>Картинка клавиши по коду. Null — рисовать текстом.</summary>
        public Sprite FindKey(string id)
        {
            if (string.IsNullOrEmpty(id))
                return null;

            foreach (KeyIcon icon in keyIcons)
            {
                if (icon != null && icon.id == id)
                    return icon.sprite;
            }

            return null;
        }
    }
}
