using UnityEngine;

namespace HeroDefense.Building
{
    /// <summary>
    /// Фиксированная точка застройки (D3).
    ///
    /// Один и тот же слот принимает башню, экономику или казарму — это D4,
    /// единый пул. Именно поэтому конфликт «жадность против безопасности»
    /// становится пространственным: слот, отданный экономике, не защищает.
    ///
    /// Визуал сейчас — цветной квад под ногами. По D17 он должен стать
    /// рамкой с ценой прямо на земле, без UI-панелей.
    /// </summary>
    public sealed class BuildSlot : MonoBehaviour
    {
        [Header("Подсветка")]
        [SerializeField] private Renderer highlightRenderer;

        [SerializeField] private Color freeColor = new Color(0.3f, 0.8f, 0.3f, 0.5f);
        [SerializeField] private Color unaffordableColor = new Color(0.8f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color occupiedColor = new Color(0.4f, 0.4f, 0.4f, 0.25f);

        private GameObject _placedBuilding;
        private MaterialPropertyBlock _propertyBlock;

        /// <summary>
        /// Ленивое создание. Awake слота может не успеть отработать до того,
        /// как подсветку запросит другой объект: Unity не гарантирует
        /// порядок инициализации между разными объектами сцены.
        /// </summary>
        private MaterialPropertyBlock PropertyBlock =>
            _propertyBlock ??= new MaterialPropertyBlock();

        public bool IsOccupied => _placedBuilding != null;

        /// <summary>Точка, где появится постройка.</summary>
        public Vector3 BuildPosition => transform.position;

        /// <summary>
        /// Поставить здание. Слот не проверяет деньги — это забота BuildController,
        /// слот только знает, занят он или нет.
        /// </summary>
        public GameObject Place(BuildingDefinition definition)
        {
            if (IsOccupied || definition == null || definition.prefab == null)
                return null;

            _placedBuilding = Instantiate(definition.prefab, BuildPosition, transform.rotation, transform);

            return _placedBuilding;
        }

        /// <summary>Снести постройку — понадобится для продажи и замены.</summary>
        public void Clear()
        {
            if (_placedBuilding != null)
                Destroy(_placedBuilding);

            _placedBuilding = null;
        }

        /// <summary>
        /// Обновить подсветку. Вызывается контроллером, потому что
        /// «по карману ли» — знание об экономике, а не о слоте.
        /// </summary>
        public void UpdateHighlight(bool canAfford)
        {
            if (highlightRenderer == null)
                return;

            Color color = IsOccupied
                ? occupiedColor
                : (canAfford ? freeColor : unaffordableColor);

            // MaterialPropertyBlock вместо material — не плодит копии материала.
            MaterialPropertyBlock block = PropertyBlock;

            highlightRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            highlightRenderer.SetPropertyBlock(block);
        }
    }
}
