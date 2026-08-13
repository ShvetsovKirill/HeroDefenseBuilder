using UnityEngine;
using UnityEngine.InputSystem;
using HeroDefense.Economy;

namespace HeroDefense.Building
{
    /// <summary>
    /// Обработка строительства: клик по слоту -> проверка золота -> постройка.
    ///
    /// Ввод здесь намеренно один — клик/тап по точке экрана.
    /// Это то же ограничение, что и с флагами (D12): всё управление должно
    /// сводиться к одиночным тапам, иначе на телефоне играть невозможно.
    /// </summary>
    public sealed class BuildController : MonoBehaviour
    {
        [Header("Что строим")]
        [Tooltip("Пока один тип на всё. Позже — выбор типа постройки.")]
        [SerializeField] private BuildingDefinition currentBuilding;

        [Header("Ссылки")]
        [SerializeField] private Wallet wallet;
        [SerializeField] private Camera worldCamera;

        [Header("Клик")]
        [Tooltip("Слой, на котором лежат слоты. Ограничивает raycast, " +
                 "чтобы клик не ловил врагов и землю.")]
        [SerializeField] private LayerMask slotLayer = ~0;

        [SerializeField] private float maxRayDistance = 200f;

        private BuildSlot[] _slots;

        private void Awake()
        {
            EnsureInitialized();
        }

        /// <summary>
        /// Сбор ссылок. Вызывается и из Awake, и из OnEnable —
        /// порядок между ними в Unity зависит от того, был ли объект
        /// активен при загрузке сцены, полагаться на него нельзя.
        /// </summary>
        private void EnsureInitialized()
        {
            if (_slots != null)
                return;

            if (worldCamera == null)
                worldCamera = Camera.main;

            _slots = FindObjectsByType<BuildSlot>(FindObjectsSortMode.None);
        }

        private void OnEnable()
        {
            EnsureInitialized();

            if (wallet != null)
                wallet.GoldChanged += OnGoldChanged;

            RefreshSlotHighlights();
        }

        private void OnDisable()
        {
            if (wallet != null)
                wallet.GoldChanged -= OnGoldChanged;
        }

        /// <summary>
        /// Повторный пересчёт: к моменту Start все Awake сцены гарантированно
        /// отработали, значит кошелёк и слоты точно готовы.
        /// </summary>
        private void Start()
        {
            RefreshSlotHighlights();
        }

        private void Update()
        {
            HandleClick();
        }

        private void OnGoldChanged(int _)
        {
            RefreshSlotHighlights();
        }

        /// <summary>
        /// Пересчёт подсветки. Вызывается по событию, а не каждый кадр:
        /// "по карману ли" меняется только при изменении баланса
        /// или при застройке слота.
        /// </summary>
        private void RefreshSlotHighlights()
        {
            if (wallet == null || currentBuilding == null || _slots == null)
                return;

            bool canAfford = wallet.CanAfford(currentBuilding.cost);

            for (int i = 0; i < _slots.Length; i++)
                _slots[i].UpdateHighlight(canAfford);
        }

        private void HandleClick()
        {
            Mouse mouse = Mouse.current;

            if (mouse == null || !mouse.leftButton.wasPressedThisFrame)
                return;

            BuildSlot slot = RaycastSlot(mouse.position.ReadValue());

            if (slot != null)
                TryBuild(slot);
        }

        private BuildSlot RaycastSlot(Vector2 screenPosition)
        {
            if (worldCamera == null)
                return null;

            Ray ray = worldCamera.ScreenPointToRay(screenPosition);

            if (!Physics.Raycast(ray, out RaycastHit hit, maxRayDistance, slotLayer))
                return null;

            return hit.collider.GetComponentInParent<BuildSlot>();
        }

        private void TryBuild(BuildSlot slot)
        {
            if (currentBuilding == null || wallet == null)
                return;

            if (slot.IsOccupied)
                return;

            if (!wallet.TrySpend(currentBuilding.cost))
                return;

            slot.Place(currentBuilding);

            // TrySpend уже дёрнул GoldChanged, но слот стал занят
            // после этого — обновляем ещё раз, чтобы он погас.
            RefreshSlotHighlights();
        }
    }
}
