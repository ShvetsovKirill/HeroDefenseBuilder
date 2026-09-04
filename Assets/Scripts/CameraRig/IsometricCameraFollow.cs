using UnityEngine;

namespace HeroDefense.CameraRig
{
    /// <summary>
    /// Камера с фиксированным углом (стиль Bad North / Kingshot), следует за целью.
    /// Угол не меняется — это важно: весь ввод считается относительно него.
    ///
    /// Позже, когда остров станет маленьким и целиком влезет в кадр,
    /// следование можно будет отключить и просто зафиксировать камеру на острове.
    /// </summary>
    public sealed class IsometricCameraFollow : MonoBehaviour
    {
        [Header("Цель")]
        [SerializeField] private Transform target;

        [Header("Положение")]
        [Tooltip("Смещение камеры от цели в мировых координатах.")]
        [SerializeField] private Vector3 offset = new Vector3(0f, 18f, -14f);

        [Tooltip("Плавность следования. Меньше = резче.")]
        [SerializeField] private float smoothTime = 0.15f;

        [Header("Угол обзора")]
        [Tooltip("Наклон камеры вниз, градусы.")]
        [SerializeField] private float pitch = 50f;

        [Tooltip("Поворот вокруг вертикали, градусы. 45 = классическая изометрия.")]
        [SerializeField] private float yaw = 45f;

        private Vector3 _velocity;

        public void SetTarget(Transform newTarget)
        {
            target = newTarget;
            SnapToTarget();
        }

        private void Start()
        {
            ApplyRotation();
            SnapToTarget();
        }

        private void LateUpdate()
        {
            if (target == null)
                return;

            ApplyRotation();
            FollowTarget();
        }

        private void ApplyRotation()
        {
            transform.rotation = Quaternion.Euler(pitch, yaw, 0f);
        }

        private void FollowTarget()
        {
            transform.position = Vector3.SmoothDamp(
                transform.position, GetDesiredPosition(), ref _velocity, smoothTime);
        }

        /// <summary>
        /// Точка, в которой камера должна находиться относительно цели.
        /// Смещение поворачивается на yaw, чтобы при смене угла обзора
        /// камера оставалась позади цели, а не уезжала вбок.
        /// </summary>
        private Vector3 GetDesiredPosition()
        {
            return target.position + Quaternion.Euler(0f, yaw, 0f) * offset;
        }

        /// <summary>Мгновенно поставить камеру на место, без интерполяции.</summary>
        private void SnapToTarget()
        {
            if (target == null)
                return;

            transform.position = GetDesiredPosition();
            _velocity = Vector3.zero;
        }
    }
}
