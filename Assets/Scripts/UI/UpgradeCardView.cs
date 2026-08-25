using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HeroDefense.Meta;

namespace HeroDefense.App.UI
{
    /// <summary>
    /// Одна ветка прокачки в замке: название, эффект, уровень, цена (D92).
    ///
    /// Устроена по образцу <c>BuildCardView</c>: выкупленное и недоступное
    /// не прячется, а тускнеет. Игрок должен видеть всю карту прокачки
    /// целиком — иначе он не может планировать, ради чего копит.
    /// </summary>
    public sealed class UpgradeCardView : MonoBehaviour
    {
        [Header("Элементы")]
        [SerializeField] private Button button;
        [SerializeField] private Image iconImage;
        [SerializeField] private TMP_Text nameText;
        [SerializeField] private TMP_Text descriptionText;
        [SerializeField] private CanvasGroup canvasGroup;

        [Tooltip("Счётчик «куплено / потолок», например 2/5.")]
        [SerializeField] private TMP_Text levelText;

        [Tooltip("Цена следующего уровня в престиже.")]
        [SerializeField] private TMP_Text costText;

        [Tooltip("Что даёт ветка на текущем уровне, например «+4 урона». " +
                 "Необязательно: без него карточка просто не покажет накопленный эффект.")]
        [SerializeField] private TMP_Text bonusText;

        [Header("Вид")]
        [Tooltip("Прозрачность, когда купить нельзя. Ноль спрятал бы карточку совсем.")]
        [SerializeField] private float unavailableAlpha = 0.45f;

        [Tooltip("Цвет цены, когда не хватает престижа.")]
        [SerializeField] private Color unaffordableCostColor = new Color(0.9f, 0.35f, 0.35f);

        [Tooltip("Подпись вместо цены, когда ветка выкачана до потолка.")]
        [SerializeField] private string maxedLabel = "макс";

        private Color _defaultCostColor;
        private UpgradeDefinition _definition;
        private CastleUpgradeView _owner;

        private void Awake()
        {
            if (costText != null)
                _defaultCostColor = costText.color;
        }

        /// <summary>Привязать карточку к ветке. Вызывает панель при сборке списка.</summary>
        public void Bind(UpgradeDefinition definition, CastleUpgradeView owner)
        {
            _definition = definition;
            _owner = owner;

            if (iconImage != null)
            {
                iconImage.sprite = definition.Icon;
                iconImage.enabled = definition.Icon != null;
            }

            if (nameText != null)
                nameText.text = definition.DisplayName;

            if (descriptionText != null)
            {
                descriptionText.text = definition.Description;
                descriptionText.gameObject.SetActive(!string.IsNullOrEmpty(definition.Description));
            }

            if (button != null)
            {
                // RemoveAllListeners, а не Remove: карточки переиспользуются
                // при пересборке списка, и подписки копились бы.
                button.onClick.RemoveAllListeners();
                button.onClick.AddListener(OnClicked);
            }

            Refresh();
        }

        /// <summary>
        /// Пересчитать уровень, цену и доступность.
        /// Панель зовёт это у всех карточек после любой покупки: купив одно,
        /// игрок мог перестать тянуть остальное.
        /// </summary>
        public void Refresh()
        {
            if (_definition == null)
                return;

            bool maxed = _definition.IsMaxed;
            bool available = _definition.CanBuy;

            if (button != null)
                button.interactable = available;

            if (canvasGroup != null)
                canvasGroup.alpha = available ? 1f : unavailableAlpha;

            if (levelText != null)
                levelText.text = $"{_definition.CurrentLevel}/{_definition.MaxLevel}";

            if (bonusText != null)
            {
                float bonus = _definition.CurrentBonus;

                bonusText.gameObject.SetActive(bonus > 0f);
                bonusText.text = _definition.FormatBonus(bonus);
            }

            UpdateCost(maxed);
        }

        /// <summary>
        /// Цена краснеет отдельно от общего приглушения: «не хватает престижа»
        /// и «дальше некуда» — разные причины отказа, и различать их
        /// игрок должен с одного взгляда.
        /// </summary>
        private void UpdateCost(bool maxed)
        {
            if (costText == null)
                return;

            if (maxed)
            {
                costText.text = maxedLabel;
                costText.color = _defaultCostColor;
                return;
            }

            costText.text = _definition.NextCost.ToString();
            costText.color = PlayerProgress.CanAfford(_definition.NextCost)
                ? _defaultCostColor
                : unaffordableCostColor;
        }

        private void OnClicked()
        {
            if (_definition == null)
                return;

            // Списание и повышение уровня живут в ассете одним методом,
            // чтобы экран не мог сделать половину операции.
            if (_definition.TryBuy())
                _owner?.RefreshAll();
        }
    }
}
