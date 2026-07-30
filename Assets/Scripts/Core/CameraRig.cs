using UnityEngine;

namespace HeroDefense.Core
{
    /// <summary>
    /// Камера с фиксированным углом обзора, плавно следующая за целью.
    /// Вешается на GameObject с компонентом Camera.
    ///
    /// Угол камеры фиксирован сознательно: под ним будет запекаться VAT-анимация врагов
    /// и рисоваться иконки построек. Менять pitch/yaw после Epic 1 — дорого.
    /// </summary>
    [RequireComponent(typeof(Camera))]
    [DisallowMultipleComponent]
    public class CameraRig : MonoBehaviour
    {
        [Header("Target")]
        [SerializeField] private Transform target;
        [Tooltip("Смещение точки, за которой следит камера, относительно цели.")]
        [SerializeField] private Vector3 targetOffset = Vector3.zero;

        [Header("Framing")]
        [Tooltip("Дистанция от точки взгляда до камеры.")]
        [SerializeField, Min(1f)] private float distance = 24f;

        [Tooltip("Наклон. 90 = строго сверху, 45 = изометрия. 55 — компромисс: видно толпу и читается силуэт.")]
        [SerializeField, Range(20f, 89f)] private float pitch = 55f;

        [Tooltip("Поворот вокруг вертикали. 45 даёт классический изометрический вид.")]
        [SerializeField, Range(-180f, 180f)] private float yaw = 45f;

        [Header("Follow")]
        [Tooltip("Время сглаживания. 0 = жёсткая привязка без инерции.")]
        [SerializeField, Min(0f)] private float smoothTime = 0.15f;

        [Tooltip("Мгновенно поставить камеру на место в Start, без наезда из точки (0,0,0).")]
        [SerializeField] private bool snapOnStart = true;

        private Vector3 _velocity;

        /// <summary>Направление «вперёд» на плоскости земли с точки зрения игрока.</summary>
        public Vector3 PlanarForward { get; private set; } = Vector3.forward;

        /// <summary>Направление «вправо» на плоскости земли с точки зрения игрока.</summary>
        public Vector3 PlanarRight { get; private set; } = Vector3.right;

        private void Awake()
        {
            RecalculatePlanarAxes();
        }

        private void Start()
        {
            if (snapOnStart) SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null) return;

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var focus = target.position + targetOffset;
            var desired = focus - rotation * Vector3.forward * distance;

            transform.position = smoothTime <= 0f
                ? desired
                : Vector3.SmoothDamp(transform.position, desired, ref _velocity, smoothTime);

            transform.rotation = rotation;
        }

        public void SetTarget(Transform newTarget, bool snap = true)
        {
            target = newTarget;
            if (snap) SnapToTarget();
        }

        [ContextMenu("Snap To Target")]
        public void SnapToTarget()
        {
            if (target == null) return;

            var rotation = Quaternion.Euler(pitch, yaw, 0f);
            var focus = target.position + targetOffset;

            transform.position = focus - rotation * Vector3.forward * distance;
            transform.rotation = rotation;
            _velocity = Vector3.zero;
        }

        /// <summary>
        /// Переводит ввод (экранные оси) в направление на плоскости земли.
        /// Без этого W при yaw = 45 поедет по диагонали относительно того, что видит игрок.
        /// </summary>
        public Vector3 InputToWorld(Vector2 input)
        {
            return PlanarRight * input.x + PlanarForward * input.y;
        }

        private void RecalculatePlanarAxes()
        {
            var flatRotation = Quaternion.Euler(0f, yaw, 0f);
            PlanarForward = flatRotation * Vector3.forward;
            PlanarRight = flatRotation * Vector3.right;
        }

        private void OnValidate()
        {
            RecalculatePlanarAxes();
        }
    }
}
