using UnityEngine;
using HeroDefense.Core;
using HeroDefense.Input;
using HeroDefense.King;
using KingCharacter = HeroDefense.King.King;

namespace HeroDefense.Hero
{
    /// <summary>
    /// Движение героя по плоскости XZ.
    ///
    /// Не знает, откуда пришёл ввод: клавиатура, тап, джойстик — всё через IMovementInput.
    /// Направление пересчитывается относительно камеры: при изометрии "W" = вверх по экрану,
    /// а не вдоль мировой оси Z.
    ///
    /// Скорость, разгон и поворот берутся из KingStats, а не из полей компонента (D66):
    /// прокачка меняет числа в рантайме, и зашитые в инспектор значения пришлось бы
    /// править руками при каждом бонусе.
    /// </summary>
    [RequireComponent(typeof(CharacterController))]
    [RequireComponent(typeof(KingCharacter))]
    public sealed class HeroMotor : MonoBehaviour
    {
        [Header("Физика")]
        [Tooltip("Гравитация нужна, чтобы CharacterController прижимался к земле. " +
                 "К характеристикам короля отношения не имеет — это свойство сцены.")]
        [SerializeField] private float gravity = -20f;

        [Header("Ссылки")]
        [Tooltip("Камера, относительно которой считается направление. Пусто = Camera.main.")]
        [SerializeField] private Transform cameraTransform;

        /// <summary>Небольшая отрицательная скорость, прижимающая героя к земле.</summary>
        private const float GroundedStickVelocity = -2f;

        /// <summary>Порог, ниже которого направление считается нулевым.</summary>
        private const float DirectionEpsilon = 0.0001f;

        private CharacterController _controller;
        private KingCharacter _king;
        private IMovementInput _input;

        private float _verticalVelocity;

        /// <summary>
        /// Текущая горизонтальная скорость — именно она даёт инерцию (D37).
        ///
        /// Ввод задаёт не скорость напрямую, а цель, к которой скорость ползёт
        /// с ограничением Acceleration. Отсюда и разгон, и торможение с раскатом:
        /// отпустил клавиши — цель стала нулём, но доехать до неё нужно время.
        /// </summary>
        private Vector3 _planarVelocity;

        /// <summary>Текущая скорость по плоскости, 0..1. Пригодится для анимации позже.</summary>
        public float NormalizedSpeed { get; private set; }

        private void Awake()
        {
            _controller = GetComponent<CharacterController>();
            _king = GetComponent<KingCharacter>();

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

            // Статы читаются каждый кадр, а не кешируются по событию StatsChanged.
            // Причина: правка ассета в инспекторе во время Play событие не поднимает,
            // а видеть результат сразу — половина смысла того, что числа лежат в ассете.
            // Стоит это двух словарных обращений, что на одном объекте несущественно.
            KingStats stats = _king != null ? _king.Stats : null;

            if (stats == null)
                return;

            float deltaTime = Time.deltaTime;

            Accelerate(stats, deltaTime);
            _planarVelocity = ClipAgainstBounds(_planarVelocity, deltaTime);

            NormalizedSpeed = stats.MoveSpeed > 0f
                ? Mathf.Clamp01(_planarVelocity.magnitude / stats.MoveSpeed)
                : 0f;

            RotateTowards(_planarVelocity, stats.TurnSpeedDeg, deltaTime);
            ApplyGravity(deltaTime);
            ApplyMovement(deltaTime);
        }

        /// <summary>Во время паузы и после конца партии король стоит.</summary>
        private static bool IsGameRunning =>
            GameState.Current == null || GameState.Current.IsPlaying;

        // ---------- Движение ----------

        /// <summary>
        /// Тянем текущую скорость к желаемой. MoveTowards, а не Lerp:
        /// он даёт постоянное ускорение в единицах в секунду, поэтому
        /// Acceleration в ассете читается как физическая величина,
        /// а не как безразмерный коэффициент сглаживания.
        /// </summary>
        private void Accelerate(KingStats stats, float deltaTime)
        {
            Vector3 desired = ToWorldDirection(_input.GetMoveDirection()) * stats.MoveSpeed;

            _planarVelocity = Vector3.MoveTowards(
                _planarVelocity, desired, stats.Acceleration * deltaTime);
        }

        /// <summary>
        /// Вертикальная скорость. Прижимаем к земле, иначе CharacterController
        /// "зависает" при сходе со склона.
        /// </summary>
        private void ApplyGravity(float deltaTime)
        {
            if (_controller.isGrounded && _verticalVelocity < 0f)
            {
                _verticalVelocity = GroundedStickVelocity;
                return;
            }

            _verticalVelocity += gravity * deltaTime;
        }

        private void ApplyMovement(float deltaTime)
        {
            Vector3 velocity = _planarVelocity;
            velocity.y = _verticalVelocity;

            _controller.Move(velocity * deltaTime);
        }

        // ---------- Границы поляны ----------

        /// <summary>
        /// Гасит ту часть скорости, что ведёт за кромку поляны (D70).
        ///
        /// Гасим составляющую, а не всю скорость: упёршись в кромку под углом,
        /// король скользит вдоль неё, а не встаёт колом. Жёсткий возврат позиции
        /// внутрь здесь не используется намеренно — он дёргал бы CharacterController,
        /// который двигает объект сам.
        /// </summary>
        private Vector3 ClipAgainstBounds(Vector3 velocity, float deltaTime)
        {
            PlayfieldBounds bounds = PlayfieldBounds.Current;

            if (bounds == null || velocity.sqrMagnitude < DirectionEpsilon)
                return velocity;

            Vector3 next = transform.position + velocity * deltaTime;
            Vector3 overshoot = next - bounds.Clamp(next);
            overshoot.y = 0f;

            // Следующий шаг остаётся внутри — вмешиваться не во что.
            if (overshoot.sqrMagnitude < DirectionEpsilon)
                return velocity;

            Vector3 outward = overshoot.normalized;
            float outwardSpeed = Vector3.Dot(velocity, outward);

            // Отрицательное значение означает движение внутрь: его не трогаем,
            // иначе король, случайно оказавшийся снаружи, не смог бы вернуться.
            return outwardSpeed <= 0f
                ? velocity
                : velocity - outward * outwardSpeed;
        }

        // ---------- Ориентация ----------

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

        /// <summary>
        /// Корпус разворачивается по фактической скорости, а не по вводу.
        /// Иначе при развороте на месте модель щёлкала бы в новую сторону,
        /// продолжая ехать в старую — ровно то, что убивает ощущение веса.
        /// </summary>
        private void RotateTowards(Vector3 direction, float turnSpeedDeg, float deltaTime)
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
                transform.rotation, target, turnSpeedDeg * deltaTime);
        }
    }
}
