using System;
using UnityEngine;
using UnityEngine.EventSystems;
using UnityEngine.UI;

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
