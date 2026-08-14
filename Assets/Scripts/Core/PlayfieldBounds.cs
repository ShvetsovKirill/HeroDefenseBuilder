using UnityEngine;

namespace HeroDefense.Core
{
    /// <summary>
    /// Границы поляны (D70).
    ///
    /// До сих пор за край карты можно было просто уехать. Границы нужны
    /// королю, духу, отрядам и флагам — то есть всему, что игрок двигает.
    ///
    /// Заодно честно закрывает D23: лес непроходим потому, что там стоит
    /// граница, а не потому, что за ней ничего не нарисовано.
    ///
    /// Форма — круг или прямоугольник. Для поляны круг обычно достаточен
    /// и считается на порядок дешевле полигона.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public sealed class PlayfieldBounds : MonoBehaviour
    {
        public enum Shape
        {
            Circle,
            Rectangle
        }

        [Header("Форма")]
        [SerializeField] private Shape shape = Shape.Circle;

        [Tooltip("Радиус для круга.")]
        [SerializeField] private float radius = 30f;

        [Tooltip("Половина размера для прямоугольника: X и Z.")]
        [SerializeField] private Vector2 halfExtents = new Vector2(30f, 30f);

        [Header("Отладка")]
        [SerializeField] private bool drawGizmo = true;
        [SerializeField] private Color gizmoColor = new Color(0.3f, 0.9f, 0.4f, 0.6f);

        private static PlayfieldBounds _current;

        public static PlayfieldBounds Current => _current;

        private void Awake()
        {
            if (_current != null && _current != this)
            {
                Debug.LogError("[PlayfieldBounds] На сцене уже есть границы поляны.", this);
                return;
            }

            _current = this;
        }

        private void OnDestroy()
        {
            if (_current == this)
                _current = null;
        }

        /// <summary>Точка внутри поляны?</summary>
        public bool Contains(Vector3 point)
        {
            Vector3 local = point - transform.position;
            local.y = 0f;

            return shape == Shape.Circle
                ? local.sqrMagnitude <= radius * radius
                : Mathf.Abs(local.x) <= halfExtents.x && Mathf.Abs(local.z) <= halfExtents.y;
        }

        /// <summary>
        /// Ближайшая допустимая точка. Если позиция внутри — возвращается как есть.
        ///
        /// Используется для мягкого удержания: вместо блокировки движения
        /// объект просто не может оказаться снаружи. Это работает и для
        /// духа без коллайдера, и для флагов, и для бойцов.
        /// </summary>
        public Vector3 Clamp(Vector3 point)
        {
            Vector3 center = transform.position;
            Vector3 local = point - center;

            float originalY = point.y;
            local.y = 0f;

            if (shape == Shape.Circle)
            {
                float distanceSqr = local.sqrMagnitude;

                if (distanceSqr > radius * radius)
                    local = local.normalized * radius;
            }
            else
            {
                local.x = Mathf.Clamp(local.x, -halfExtents.x, halfExtents.x);
                local.z = Mathf.Clamp(local.z, -halfExtents.y, halfExtents.y);
            }

            Vector3 result = center + local;
            result.y = originalY;

            return result;
        }

        /// <summary>Удобная обёртка: удержать точку, если границы вообще заданы.</summary>
        public static Vector3 ClampToField(Vector3 point)
        {
            return _current != null ? _current.Clamp(point) : point;
        }

        private void OnDrawGizmosSelected()
        {
            if (!drawGizmo)
                return;

            Gizmos.color = gizmoColor;

            if (shape == Shape.Circle)
            {
                DrawCircleGizmo();
                return;
            }

            Gizmos.DrawWireCube(
                transform.position,
                new Vector3(halfExtents.x * 2f, 0.1f, halfExtents.y * 2f));
        }

        private void DrawCircleGizmo()
        {
            const int segments = 48;

            Vector3 previous = transform.position + new Vector3(radius, 0f, 0f);

            for (int i = 1; i <= segments; i++)
            {
                float angle = i / (float)segments * Mathf.PI * 2f;

                Vector3 next = transform.position + new Vector3(
                    Mathf.Cos(angle) * radius, 0f, Mathf.Sin(angle) * radius);

                Gizmos.DrawLine(previous, next);
                previous = next;
            }
        }
    }
}
