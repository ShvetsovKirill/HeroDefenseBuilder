using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HeroDefense.Localization;

namespace HeroDefense.App.UI
{
    /// <summary>
    /// Раздел настроек. Пока в нём один пункт — язык.
    ///
    /// Раньше панель была пустой рамкой без кнопки возврата: игрок,
    /// открывший настройки, застревал в них насовсем. Кнопка «назад»
    /// здесь важнее самих настроек.
    /// </summary>
    public sealed class SettingsView : MonoBehaviour
    {
        [Header("Кнопки языка")]
        [SerializeField] private Button englishButton;
        [SerializeField] private Button russianButton;

        [Header("Возврат")]
        [Tooltip("Закрывает настройки. Обязательна: без неё из раздела нет выхода.")]
        [SerializeField] private Button backButton;

        [Tooltip("Меню, к которому возвращаемся. Его метод CloseSettings " +
                 "прячет панель и показывает корневое меню обратно.")]
        [SerializeField] private MainMenuView menu;

        [Header("Подсветка выбранного")]
        [Tooltip("Цвет надписи выбранного языка.")]
        [SerializeField] private Color selectedColor = new Color(0.79f, 0.64f, 0.15f);

        [Tooltip("Цвет надписи невыбранного языка.")]
        [SerializeField] private Color normalColor = new Color(0.95f, 0.89f, 0.78f);

        private void Awake()
        {
            Bind(englishButton, () => Choose(Language.English));
            Bind(russianButton, () => Choose(Language.Russian));
            Bind(backButton, Close);
        }

        private void OnEnable()
        {
            Loc.LanguageChanged += OnLanguageChanged;
            Highlight();
        }

        private void OnDisable()
        {
            Loc.LanguageChanged -= OnLanguageChanged;
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        private void Choose(Language language)
        {
            Loc.SetLanguage(language);
        }

        private void OnLanguageChanged(Language language)
        {
            Highlight();
        }

        /// <summary>
        /// Подсветить выбранный язык. Названия языков намеренно не переводятся:
        /// «Русский» ищут глазами именно как «Русский», а не как «Russian».
        /// </summary>
        private void Highlight()
        {
            Paint(englishButton, Loc.Current == Language.English);
            Paint(russianButton, Loc.Current == Language.Russian);
        }

        private void Paint(Button button, bool selected)
        {
            if (button == null)
                return;

            TMP_Text label = button.GetComponentInChildren<TMP_Text>(true);

            if (label != null)
                label.color = selected ? selectedColor : normalColor;
        }

        private void Close()
        {
            if (menu != null)
                menu.CloseSettings();
            else
                gameObject.SetActive(false);
        }
    }
}
