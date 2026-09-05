using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;

namespace HeroDefense.UI
{
    /// <summary>
    /// Держит поля между подписью кнопки и её краем.
    ///
    /// Кнопки в сценах свёрстаны под русский текст, а он короче английского:
    /// «В поход» помещается, «March Out» вылезает за подложку. С третьим
    /// языком то же случится снова, и подгонять каждую кнопку руками —
    /// работа без конца.
    ///
    /// Поэтому правило одно на игру: подпись держит отступ от края, а если
    /// не помещается — ужимается. Кегль при этом только уменьшается,
    /// никогда не растёт: иначе короткое слово раздулось бы на всю кнопку.
    ///
    /// Применяется ко всем кнопкам сцены при её загрузке, включая те, что
    /// лежат в выключенных панелях, — иначе настройки и экраны замка
    /// остались бы неисправленными до первого показа.
    /// </summary>
    public static class ButtonTextFitter
    {
        /// <summary>
        /// Отступ по горизонтали в единицах разметки. Подобран под кнопки
        /// меню: заметно меньше — подпись липнет к рамке.
        /// </summary>
        private const float HorizontalPadding = 22f;

        /// <summary>Отступ по вертикали. Меньше горизонтального: кнопки низкие.</summary>
        private const float VerticalPadding = 6f;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            if (mode != LoadSceneMode.Single)
                return;

            foreach (Button button in Object.FindObjectsByType<Button>(
                         FindObjectsInactive.Include, FindObjectsSortMode.None))
            {
                Apply(button);
            }
        }

        /// <summary>
        /// Прижать поле надписи к самой кнопке.
        ///
        /// Отступы и автосжатие считаются от прямоугольника надписи, а не
        /// кнопки. Если надпись шире кнопки, ужимать её бесполезно: она
        /// всё равно вылезет за подложку. Растягиваем только тех, кто
        /// действительно шире — намеренно смещённые подписи не трогаем.
        /// </summary>
        private static RectTransform FitToButton(TMP_Text label, Button button)
        {
            // Границей считаем подложку кнопки, а не её объект: подложка —
            // это то, что игрок видит как кнопку, и она бывает меньше
            // самого объекта с компонентом Button.
            RectTransform frame = button.targetGraphic != null
                ? button.targetGraphic.rectTransform
                : button.transform as RectTransform;

            RectTransform labelRect = label.rectTransform;

            if (frame == null)
                return labelRect;

            // Надпись, вложенную глубже подложки, не двигаем: её положение
            // задано вёрсткой осознанно. Но рамкой всё равно считаем подложку.
            if (labelRect.parent != frame)
                return frame;

            bool fits = labelRect.rect.width <= frame.rect.width
                        && labelRect.rect.height <= frame.rect.height;

            if (!fits)
            {
                labelRect.anchorMin = Vector2.zero;
                labelRect.anchorMax = Vector2.one;
                labelRect.offsetMin = Vector2.zero;
                labelRect.offsetMax = Vector2.zero;
            }

            return frame;
        }

        /// <summary>
        /// Поправить одну кнопку. Публичный: экраны, собранные в рантайме,
        /// зовут это сразу после создания кнопки — их sceneLoaded уже не застанет.
        /// </summary>
        public static void Apply(Button button)
        {
            if (button == null)
                return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

            if (label == null)
                return;

            RectTransform frame = FitToButton(label, button);

            // Отступы ставятся ВСЕГДА, в том числе поверх уже включённого
            // автосжатия: у половины надписей в сцене оно включено, и подпись
            // прижималась вплотную к рамке.
            label.margin = new Vector4(HorizontalPadding, VerticalPadding,
                                       HorizontalPadding, VerticalPadding);

            // Кегль подбирает отдельный компонент: штатное автосжатие TMP
            // считает только высоту и на вылезающую вбок подпись не реагирует.
            ButtonLabelFit fit = label.GetComponent<ButtonLabelFit>();

            if (fit == null)
                fit = label.gameObject.AddComponent<ButtonLabelFit>();

            fit.Bind(frame);
        }
    }
}
