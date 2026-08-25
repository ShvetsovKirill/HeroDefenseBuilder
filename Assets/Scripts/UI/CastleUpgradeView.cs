using System;
using System.Collections.Generic;
using TMPro;
using UnityEngine;
using HeroDefense.Meta;

namespace HeroDefense.App.UI
{
    /// <summary>
    /// Экран траты престижа в замке (D92).
    ///
    /// Собирает карточки веток из ассетов и держит их в актуальном виде.
    /// Ничего не считает сам: цену знает <see cref="UpgradeDefinition"/>,
    /// баланс — <see cref="PlayerProgress"/>. Здесь только показ и обновление.
    ///
    /// Устроен по образцу <c>BuildPanelView</c>: контейнер плюс префаб карточки.
    /// Фиксированные слоты в инспекторе пришлось бы переделывать вручную
    /// при каждой новой ветке.
    /// </summary>
    public sealed class CastleUpgradeView : MonoBehaviour
    {
        [Header("Ветки")]
        [Tooltip("Ассеты улучшений в порядке показа. Те же, что заведены " +
                 "в UpgradeApplier в сцене боя — иначе игрок купит то, " +
                 "что в забег не попадёт.")]
        [SerializeField] private UpgradeDefinition[] upgrades = Array.Empty<UpgradeDefinition>();

        [Header("Ссылки")]
        [Tooltip("Куда складывать карточки. Обычно объект с Vertical/Grid Layout Group.")]
        [SerializeField] private RectTransform cardContainer;

        [Tooltip("Префаб одной карточки.")]
        [SerializeField] private UpgradeCardView cardPrefab;

        [Tooltip("Строка с текущим балансом престижа. Необязательно.")]
        [SerializeField] private TMP_Text prestigeText;

        [Header("Подписи")]
        [Tooltip("Формат строки баланса. {0} заменяется на количество престижа.")]
        [SerializeField] private string prestigeFormat = "Престиж: {0}";

        private readonly List<UpgradeCardView> _cards = new();

        private void Awake()
        {
            BuildCards();
        }

        private void OnEnable()
        {
            // Подписка здесь, а не в Awake: панель включается и выключается
            // вместе с разделом замка, и висеть на событиях в закрытом
            // состоянии ей незачем.
            PlayerProgress.PrestigeChanged += OnPrestigeChanged;

            RefreshAll();
        }

        private void OnDisable()
        {
            PlayerProgress.PrestigeChanged -= OnPrestigeChanged;
        }

        // ---------- Сборка ----------

        /// <summary>
        /// Создать карточки по ассетам. Пустой список — не ошибка:
        /// экран просто окажется пустым, а не свалится.
        /// </summary>
        private void BuildCards()
        {
            if (cardContainer == null || cardPrefab == null)
            {
                Debug.LogWarning("[Прокачка] Не задан контейнер или префаб карточки — " +
                                 "экран улучшений будет пустым.", this);
                return;
            }

            foreach (UpgradeDefinition upgrade in upgrades)
            {
                if (upgrade == null)
                    continue;

                UpgradeCardView card = Instantiate(cardPrefab, cardContainer);

                card.Bind(upgrade, this);
                _cards.Add(card);
            }
        }

        // ---------- Обновление ----------

        /// <summary>
        /// Пересчитать все карточки и баланс.
        /// Публичный, потому что карточка зовёт его после удачной покупки:
        /// потратив престиж, игрок мог перестать тянуть соседние ветки.
        /// </summary>
        public void RefreshAll()
        {
            foreach (UpgradeCardView card in _cards)
            {
                if (card != null)
                    card.Refresh();
            }

            UpdatePrestigeText();
        }

        private void OnPrestigeChanged(int prestige)
        {
            RefreshAll();
        }

        private void UpdatePrestigeText()
        {
            if (prestigeText == null)
                return;

            prestigeText.text = string.Format(prestigeFormat, PlayerProgress.Prestige);
        }

        // ---------- Отладка ----------

        /// <summary>Сводка по всем веткам. Для отладочного HUD и логов.</summary>
        public string Describe()
        {
            var text = new System.Text.StringBuilder();

            text.AppendLine($"Престиж: {PlayerProgress.Prestige}");

            foreach (UpgradeDefinition upgrade in upgrades)
            {
                if (upgrade != null)
                    text.AppendLine(upgrade.Describe());
            }

            return text.ToString();
        }
    }
}
