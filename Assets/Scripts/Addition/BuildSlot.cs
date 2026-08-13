using UnityEngine;
using HeroDefense.Enemies;

namespace HeroDefense.Building
{
    /// <summary>
    /// Фиксированная точка застройки (D3).
    ///
    /// Один слот принимает башню, экономику или казарму — это D4, единый пул.
    /// Именно поэтому конфликт «жадность против безопасности» становится
    /// пространственным: слот, отданный экономике, не защищает.
    /// </summary>
    public sealed class BuildSlot : MonoBehaviour
    {
        [Header("Подсветка")]
        [SerializeField] private Renderer highlightRenderer;

        [SerializeField] private Color freeColor = new Color(0.3f, 0.8f, 0.3f, 0.5f);
        [SerializeField] private Color focusedColor = new Color(0.9f, 0.9f, 0.4f, 0.7f);
        [SerializeField] private Color blockedColor = new Color(0.8f, 0.3f, 0.3f, 0.5f);
        [SerializeField] private Color occupiedColor = new Color(0.4f, 0.4f, 0.4f, 0.25f);

        [Header("Ограничения")]
        [Tooltip("Радиус проверки на врагов: нельзя строить там, где идёт бой.")]
        [SerializeField] private float combatCheckRadius = 3f;

        private GameObject _placedBuilding;
        private MaterialPropertyBlock _propertyBlock;

        /// <summary>
        /// Ленивое создание: Awake слота может не успеть отработать до того,
        /// как подсветку запросит другой объект — Unity не гарантирует
        /// порядок инициализации между объектами сцены.
        /// </summary>
        private MaterialPropertyBlock PropertyBlock =>
            _propertyBlock ??= new MaterialPropertyBlock();

        public bool IsOccupied => _placedBuilding != null;

        /// <summary>Точка, где появится постройка.</summary>
        public Vector3 BuildPosition => transform.position;

        /// <summary>
        /// Нельзя строить там, где идёт бой.
        /// Кроме логики это ещё и тактическое ограничение:
        /// проглядел фланг — не сможешь заткнуть его прямо в бою.
        /// </summary>
        public bool IsContested(EnemyManager enemyManager)
        {
            if (enemyManager == null)
                return false;

            return enemyManager.FindNearest(BuildPosition, combatCheckRadius) != null;
        }

        /// <summary>
        /// Поставить здание. Слот не проверяет ни деньги, ни бой —
        /// это забота BuildController. Слот знает только, занят он или нет.
        /// </summary>
        public GameObject Place(BuildingDefinition definition)
        {
            if (IsOccupied || definition == null || definition.prefab == null)
                return null;

            _placedBuilding = Instantiate(definition.prefab, BuildPosition, transform.rotation, transform);

            return _placedBuilding;
        }

        /// <summary>Снести постройку — для продажи и замены (D43).</summary>
        public void Clear()
        {
            if (_placedBuilding != null)
                Destroy(_placedBuilding);

            _placedBuilding = null;
        }

        public enum HighlightState
        {
            /// <summary>Свободен, король далеко.</summary>
            Free,

            /// <summary>Король рядом — этот слот сейчас активен.</summary>
            Focused,

            /// <summary>Строить нельзя: идёт бой.</summary>
            Blocked,

            /// <summary>Уже застроен.</summary>
            Occupied
        }

        public void SetHighlight(HighlightState state)
        {
            if (highlightRenderer == null)
                return;

            Color color = state switch
            {
                HighlightState.Focused => focusedColor,
                HighlightState.Blocked => blockedColor,
                HighlightState.Occupied => occupiedColor,
                _ => freeColor
            };

            // MaterialPropertyBlock вместо material — не плодит копии материала.
            MaterialPropertyBlock block = PropertyBlock;

            highlightRenderer.GetPropertyBlock(block);
            block.SetColor("_BaseColor", color);
            highlightRenderer.SetPropertyBlock(block);
        }
    }
}
