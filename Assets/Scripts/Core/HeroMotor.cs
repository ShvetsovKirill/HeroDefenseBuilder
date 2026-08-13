using UnityEngine;
using HeroDefense.Core;
using HeroDefense.Input;

namespace HeroDefense.Hero
{
    /// <summary>
    /// Движение героя по плоскости XZ.
    /// Не знает, откуда пришёл ввод: клавиатура, тап, джойстик — всё через IMovementInput.
    /// Направление пересчитывается относительно камеры: при изометрии "W" = вверх по экрану,
    /// а не вдоль мировой оси Z.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    public sealed class HeroMotor : MonoBehaviour
    {
        [Header("Движение")]
        [SerializeField] private float moveSpeed = 6f;

        [Tooltip("Скорость поворота в градусах/сек. 0 = мгновенный поворот.")]
        [SerializeField] private float turnSpeedDeg = 720f;

        [Tooltip("Гравитация нужна, чтобы CharacterController прижимался к земле.")]
        [SerializeField] private float gravity = -20f;

        [Header("Ссылки")]
        [Tooltip("Камера, относительно которой считается направление. Пусто = Camera.main.")]
        [SerializeField] private Transform cameraTransform;

        /// <summary>Небольшая отрицательная скорость, прижимающая героя к земле.</summary>
        private const float GroundedStickVelocity = -2f;

        /// <summary>Порог, ниже которого направление считается нулевым.</summary>
        private const float DirectionEpsilon = 0.0001f;

        private CharacterController _controller;
        private IMovementInput _input;
        private float _verticalVelocity;

        /// <summary>Текущая скорость по плоскости, 0..1. Пригодится для анимации позже.</summary>
        public float NormalizedSpeed { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();

            if (cameraTransform == null && Camera.main != null)
                cameraTransform = Camera.main.transform;
        }

        /// <summary>
        /// Точка подмены ввода. Вызывается из GameBootstrap.
        /// Смена клавиатуры на тач — одна строка здесь, герой не меняется.
        /// </summary>
        public void SetInput(IMovementInput input)
        {
            _input = input;
        }

        private void Update()
        {
            if (_input == null || !IsGameRunning)
                return;

            Vector3 move = ToWorldDirection(_input.GetMoveDirection());
            NormalizedSpeed = move.magnitude;

            RotateTowards(move);
            ApplyGravity();
            ApplyMovement(move);
        }

        /// <summary>Во время паузы и после конца партии король стоит.</summary>
        private static bool IsGameRunning =>
            GameState.Current == null || GameState.Current.IsPlaying;

        /// <summary>
        /// Вертикальная скорость. Прижимаем к земле, иначе CharacterController
        /// "зависает" при сходе со склона.
        /// </summary>
        private void ApplyGravity()
        {
            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = GroundedStickVelocity;
                return;
            }

            _verticalVelocity += gravity * Time.deltaTime;
        }

        private void ApplyMovement(Vector3 planarDirection)
        {
            Vector3 velocity = planarDirection * moveSpeed;
            velocity.y = _verticalVelocity;

            _controller.Move(velocity * Time.deltaTime);
        }

        /// <summary>
        /// Экранный ввод -> мировое направление на плоскости XZ, с учётом поворота камеры.
        /// </summary>
        private Vector3 ToWorldDirection(Vector2 input)
        {
            if (input.sqrMagnitude < DirectionEpsilon)
                return Vector3.zero;

            if (cameraTransform == null)
                return new Vector3(input.x, 0f, input.y);

            Vector3 forward = cameraTransform.forward;
            Vector3 right = cameraTransform.right;

            // Уплощаем: камера смотрит вниз, её forward имеет большую Y-компоненту.
            forward.y = 0f;
            right.y = 0f;
            forward.Normalize();
            right.Normalize();

            Vector3 dir = right * input.x + forward * input.y;
            return dir.sqrMagnitude > 1f ? dir.normalized : dir;
        }

        private void RotateTowards(Vector3 direction)
        {
            if (direction.sqrMagnitude < DirectionEpsilon)
                return;

            Quaternion target = Quaternion.LookRotation(direction, Vector3.up);

            if (turnSpeedDeg <= 0f)
            {
                transform.rotation = target;
                return;
            }

            transform.rotation = Quaternion.RotateTowards(
                transform.rotation, target, turnSpeedDeg * Time.deltaTime);
        }
    }
}
