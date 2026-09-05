using System;
using TMPro;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;
using HeroDefense.Localization;

namespace HeroDefense.App.UI
{
    /// <summary>
    /// Одно кликабельное здание в замке (D91).
    ///
    /// Здания — это разделы: казармы, технологии, прокачка короля.
    /// Клик по картинке, а не по кнопке в списке: так экран остаётся
    /// иллюстрацией, а не меню с кнопками.
    /// </summary>
    [RequireComponent(typeof(Image))]
    public sealed class CastleBuildingButton : MonoBehaviour,
        IPointerClickHandler, IPointerEnterHandler, IPointerExitHandler
    {
        [Header("Что открывает")]
        [SerializeField] private string sectionId = "barracks";

        [Header("Подсветка")]
        [Tooltip("Что подсвечивается при наведении: обводка, свечение окон.")]
        [SerializeField] private GameObject highlight;

        [Tooltip("Всплывающая подпись. Показывается при наведении.")]
        [SerializeField] private GameObject label;

        [Tooltip("Подставлять ли в подпись название раздела из таблицы переводов. " +
                 "Ключ строится сам: castle.<sectionId>. Выключено — подпись " +
                 "остаётся такой, как набрана в сцене.")]
        [SerializeField] private bool localizeLabel = true;

        [Header("Доступность")]
        [Tooltip("Выключено — здание видно, но не кликается. " +
                 "Для разделов, которые ещё не открыты.")]
        [SerializeField] private bool available = true;

        [Range(0f, 1f)]
        [SerializeField] private float unavailableAlpha = 0.5f;

        /// <summary>Кликнули по зданию. Аргумент — идентификатор раздела.</summary>
        public event Action<string> Clicked;

        public string SectionId => sectionId;

        private Image _image;

        private void Awake()
        {
            _image = GetComponent<Image>();

            SetHighlight(false);
            ApplyAvailability();
            RefreshLabel();
        }

        private void OnEnable()
        {
            Loc.LanguageChanged += OnLanguageChanged;
        }

        private void OnDisable()
        {
            Loc.LanguageChanged -= OnLanguageChanged;
        }

        private void OnLanguageChanged(Language language) => RefreshLabel();

        /// <summary>
        /// Название раздела в подпись здания.
        ///
        /// Ключ строится из sectionId, а не задаётся отдельным полем:
        /// у здания уже есть опознание, и второе поле рядом с ним неминуемо
        /// разъедется с первым. Нет ключа в таблице — остаётся набранный
        /// в сцене текст, поэтому включать это безопасно.
        /// </summary>
        private void RefreshLabel()
        {
            if (!localizeLabel || label == null || string.IsNullOrEmpty(sectionId))
                return;

            TMP_Text text = label.GetComponentInChildren<TMP_Text>(true);

            if (text != null)
                text.text = Loc.GetOrFallback($"castle.{sectionId}", text.text);
        }

        public void SetAvailable(bool value)
        {
            available = value;
            ApplyAvailability();
        }

        private void ApplyAvailability()
        {
            if (_image == null)
                return;

            Color color = _image.color;

            color.a = available ? 1f : unavailableAlpha;
            _image.color = color;
        }

        public void OnPointerClick(PointerEventData eventData)
        {
            if (available)
                Clicked?.Invoke(sectionId);
        }

        public void OnPointerEnter(PointerEventData eventData)
        {
            if (available)
                SetHighlight(true);
        }

        public void OnPointerExit(PointerEventData eventData)
        {
            SetHighlight(false);
        }

        private void SetHighlight(bool visible)
        {
            if (highlight != null)
                highlight.SetActive(visible);

            if (label != null)
                label.SetActive(visible);
        }
    }
}
