using System.Collections.Generic;
using UnityEngine;

namespace HeroDefense.Building.UI
{
    /// <summary>
    /// Панель выбора постройки. Появляется, когда король подъехал к слоту,
    /// прячется, когда отъехал.
    ///
    /// Карточки создаются один раз при старте и переиспользуются:
    /// пересоздавать их при каждом открытии — лишний мусор.
    /// </summary>
    public sealed class BuildPanelView : MonoBehaviour
    {
        [Header("Ссылки")]
        [SerializeField] private BuildController controller;

        [Tooltip("Контейнер карточек — обычно с Horizontal Layout Group.")]
        [SerializeField] private RectTransform cardContainer;

        [SerializeField] private BuildCardView cardPrefab;

        [Tooltip("Корень панели. Включается и выключается целиком.")]
        [SerializeField] private GameObject panelRoot;

        private readonly List<BuildCardView> _cards = new();

        private void Awake()
        {
            if (controller == null)
            {
                // Единственное допустимое место для поиска: UI ищет контроллер
                // своей же сцены один раз при старте. Но лучше назначить явно.
                controller = FindFirstObjectByType<BuildController>();

                if (controller == null)
                {
                    Debug.LogError("[BuildPanelView] Не найден BuildController.", this);
                    enabled = false;
                    return;
                }
            }

            ValidatePanelRoot();
            BuildCards();
            SetVisible(false);

            // Подписка в Awake, а не в OnEnable: если panelRoot когда-нибудь
            // окажется родителем этого объекта, OnDisable отписал бы нас
            // навсегда и панель больше не открылась бы.
            if (controller != null)
                controller.FocusChanged += OnFocusChanged;
        }

        private void OnDestroy()
        {
            if (controller != null)
                controller.FocusChanged -= OnFocusChanged;
        }

        /// <summary>
        /// Panel Root не должен быть этим же объектом или его родителем:
        /// выключив его, скрипт выключил бы сам себя и перестал получать Update.
        /// </summary>
        private void ValidatePanelRoot()
        {
            if (panelRoot == null)
                return;

            bool isSelfOrParent = panelRoot == gameObject
                || transform.IsChildOf(panelRoot.transform);

            if (!isSelfOrParent)
                return;

            Debug.LogError(
                $"[BuildPanelView] Panel Root ({panelRoot.name}) — это сам объект со скриптом " +
                "или его родитель. Выключив его, панель отключила бы саму себя. " +
                "Назначь дочерний объект.", this);

            panelRoot = null;
        }

        private void BuildCards()
        {
            if (controller == null || cardPrefab == null || cardContainer == null)
                return;

            foreach (BuildingDefinition definition in controller.Catalog)
            {
                if (definition == null)
                    continue;

                BuildCardView card = Instantiate(cardPrefab, cardContainer);

                card.Bind(definition, controller);
                _cards.Add(card);
            }
        }

        private void OnFocusChanged(BuildSlot slot)
        {
            SetVisible(slot != null);
        }

        private void SetVisible(bool visible)
        {
            if (panelRoot != null)
                panelRoot.SetActive(visible);
        }

        private void Update()
        {
            if (panelRoot == null || !panelRoot.activeSelf)
                return;

            // Доступность меняется от золота и от появления врагов рядом,
            // поэтому обновляем постоянно, пока панель открыта.
            for (int i = 0; i < _cards.Count; i++)
                _cards[i].Refresh();
        }
    }
}
