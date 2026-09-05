using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HeroDefense.Audio;
using HeroDefense.Localization;

namespace HeroDefense.App.UI
{
    /// <summary>
    /// Раздел настроек: язык и громкость.
    ///
    /// Раньше панель была пустой рамкой без кнопки возврата: игрок,
    /// открывший настройки, застревал в них насовсем. Кнопка «назад»
    /// здесь важнее самих настроек.
    ///
    /// Ползунки необязательны: пока их нет в вёрстке, экран работает
    /// как раньше, а звук играет на полной громкости.
    /// </summary>
    public sealed class SettingsView : MonoBehaviour
    {
        [Header("Кнопки языка")]
        [SerializeField] private Button englishButton;
        [SerializeField] private Button russianButton;

        [Header("Громкость")]
        [Tooltip("Общая громкость. Необязательно: без ползунка звук играет как есть.")]
        [SerializeField] private Slider masterSlider;

        [Tooltip("Громкость музыки. Отдельно от эффектов: её выключают " +
                 "гораздо чаще, чем звуки боя.")]
        [SerializeField] private Slider musicSlider;

        [Tooltip("Громкость звуковых эффектов.")]
        [SerializeField] private Slider sfxSlider;

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

            BindVolume(masterSlider, value => AudioOptions.Master = value);
            BindVolume(musicSlider, value => AudioOptions.Music = value);
            BindVolume(sfxSlider, value => AudioOptions.Sfx = value);
        }

        /// <summary>
        /// Подписать ползунок на канал громкости.
        ///
        /// Диапазон и целочисленность выставляются кодом, а не в инспекторе:
        /// ползунок со Whole Numbers даёт две ступени громкости вместо
        /// плавной регулировки, и заметить это на глаз в инспекторе трудно.
        /// </summary>
        private static void BindVolume(Slider slider, UnityEngine.Events.UnityAction<float> apply)
        {
            if (slider == null)
                return;

            slider.minValue = 0f;
            slider.maxValue = 1f;
            slider.wholeNumbers = false;

            slider.onValueChanged.RemoveAllListeners();
            slider.onValueChanged.AddListener(apply);
        }

        private void OnEnable()
        {
            Loc.LanguageChanged += OnLanguageChanged;
            Highlight();
            ShowVolumes();
        }

        /// <summary>
        /// Показать сохранённые громкости. Обязательно без вызова обработчиков:
        /// SetValueWithoutNotify не дёргает onValueChanged, иначе открытие
        /// экрана записывало бы значения обратно и на пустых настройках
        /// сбрасывало бы их в ноль.
        /// </summary>
        private void ShowVolumes()
        {
            masterSlider?.SetValueWithoutNotify(AudioOptions.Master);
            musicSlider?.SetValueWithoutNotify(AudioOptions.Music);
            sfxSlider?.SetValueWithoutNotify(AudioOptions.Sfx);
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
