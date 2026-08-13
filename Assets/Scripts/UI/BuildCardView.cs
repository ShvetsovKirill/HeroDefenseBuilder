using TMPro;
using UnityEngine;
using UnityEngine.UI;

namespace HeroDefense.Building.UI
{
    /// <summary>
    /// Одна карточка в панели строительства: иконка, название, цена.
    ///
    /// Недоступные варианты не прячутся, а приглушаются — игрок должен видеть,
    /// что ещё бывает, даже если сейчас не по карману.
    ///
    /// Текст на TextMeshPro: legacy Text мылится при масштабировании
    /// и хуже работает с кириллицей.
    /// </summary>
    public sealed class BuildCardView : MonoBehaviour
    {
        [Header("Элементы")]
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text costText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Header("Вид")]
        [SerializeField] private float unavailableAlpha = 0.45f;

        [Tooltip("Цвет цены, когда не хватает золота.")]
        [SerializeField] private Color unaffordableCostColor = new Color(0.9f, 0.35f, 0.35f);

        private Color _defaultCostColor;
        private BuildingDefinition _definition;
        private BuildController _controller;

        private void Awake()
        {
            if (costText != null)
                _defaultCostColor = costText.color;
        }

        public void Bind(BuildingDefinition definition, BuildController controller)
        {
            _definition = definition;
            _controller = controller;

            if (iconImage != null)
            {
                iconImage.sprite = definition.icon;
                iconImage.enabled = definition.icon != null;
            }

            if (nameText != null)
                nameText.text = definition.displayName;

            if (costText != null)
                costText.text = definition.cost.ToString();

            if (button != null)
            {
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClicked);
            }
        }

        /// <summary>Обновить доступность. Вызывается панелью, пока она открыта.</summary>
        public void Refresh()
        {
            if (_controller == null || _definition == null)
                return;

            bool available = _controller.CanBuild(_definition, out _);

            if (button != null)
                button.interactable = available;

            if (canvasGroup != null)
                canvasGroup.alpha = available ? 1f : unavailableAlpha;

            UpdateCostColor();
        }

        /// <summary>
        /// Цена краснеет отдельно от общего приглушения:
        /// «не хватает золота» и «идёт бой» — разные причины отказа,
        /// и игрок должен различать их с одного взгляда.
        /// </summary>
        private void UpdateCostColor()
        {
            if (costText == null || _controller == null)
                return;

            bool canAfford = _controller.CanAfford(_definition);

            costText.color = canAfford ? _defaultCostColor : unaffordableCostColor;
        }

        private void OnClicked()
        {
            _controller.TryBuild(_definition);
        }
    }
}
