using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.InputSystem.EnhancedTouch;
using Touch = UnityEngine.InputSystem.EnhancedTouch.Touch;
using TouchPhase = UnityEngine.InputSystem.TouchPhase;

namespace HeroDefense.Core
{
    /// <summary>
    /// Единая точка чтения ввода. Реализована как ScriptableObject, чтобы потребители
    /// (герой, камера, UI) ссылались на один ассет, а не искали друг друга по сцене.
    ///
    /// Поддерживает клавиатуру, геймпад и сенсорный экран. Потребитель не знает,
    /// откуда пришло направление — он просто спрашивает ReadMove().
    ///
    /// Биндинги заданы в коде намеренно: .inputactions-ассет настраивается руками,
    /// и ошибка в нём — это молча неработающий ввод без единого сообщения в консоли.
    /// Код проверяется компилятором. Переход на ассет понадобится только в том эпике,
    /// где появится переназначение клавиш в настройках.
    /// </summary>
    [CreateAssetMenu(menuName = "HeroDefense/Input Reader", fileName = "InputReader")]
    public class InputReader : ScriptableObject
    {
        [Header("Плавающий стик (сенсорный экран)")]
        [Tooltip("Радиус стика в долях высоты экрана. Задан долей, а не пикселями: " +
                 "иначе на телефоне с плотным экраном стик получится крошечным.")]
        [SerializeField, Range(0.05f, 0.30f)] private float stickRadiusScreenFraction = 0.12f;

        [Tooltip("Мёртвая зона в долях радиуса. Спасает от дрожания пальца.")]
        [SerializeField, Range(0f, 0.5f)] private float stickDeadZoneFraction = 0.12f;

        [Tooltip("Тянуть центр стика за пальцем, если тот ушёл за радиус. " +
                 "Нужно для долгого бега: без этого палец упирается в край и приходится отрывать.")]
        [SerializeField] private bool dragStickOrigin = true;

        private InputAction _moveAction;
        private bool _enabled;

        private int _stickFingerId = -1;
        private Vector2 _stickOrigin;
        private Vector2 _stickValue;
        private int _lastTouchFrame = -1;

        public bool IsEnabled => _enabled;

        // Пригодится, когда будем рисовать сам джойстик на экране: визуал возьмёт
        // готовые центр и отклонение отсюда и не будет заново разбирать касания.
        public bool HasActiveStick => _stickFingerId >= 0;
        public Vector2 StickOrigin => _stickOrigin;
        public Vector2 StickValue => _stickValue;

        /// <summary>Вызывается один раз из GameBootstrap.</summary>
        public void Enable()
        {
            if (_enabled) return;

            BuildActions();
            _moveAction.Enable();

            // Без этого Touch.activeTouches всегда пуст.
            if (!EnhancedTouchSupport.enabled) EnhancedTouchSupport.Enable();

            _enabled = true;
        }

        public void Disable()
        {
            if (!_enabled) return;

            _moveAction.Disable();
            if (EnhancedTouchSupport.enabled) EnhancedTouchSupport.Disable();

            ResetStick();
            _enabled = false;
        }

        /// <summary>
        /// Направление движения в экранных осях (x = вправо, y = вперёд), длина от 0 до 1.
        /// Приоритет у касания: если палец на экране, клавиатура игнорируется.
        /// Преобразование в мировые координаты — задача потребителя, оно зависит от угла камеры.
        /// </summary>
        public Vector2 ReadMove()
        {
            if (!_enabled) return Vector2.zero;

            UpdateTouchStick();
            if (_stickValue.sqrMagnitude > 0.000001f) return _stickValue;

            return _moveAction != null ? _moveAction.ReadValue<Vector2>() : Vector2.zero;
        }

        // ------------------------------------------------------------------
        // Клавиатура и геймпад
        // ------------------------------------------------------------------

        private void BuildActions()
        {
            if (_moveAction != null) return;

            _moveAction = new InputAction("Move", InputActionType.Value, expectedControlType: "Vector2");

            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/w")
                .With("Down", "<Keyboard>/s")
                .With("Left", "<Keyboard>/a")
                .With("Right", "<Keyboard>/d");

            _moveAction.AddCompositeBinding("2DVector")
                .With("Up", "<Keyboard>/upArrow")
                .With("Down", "<Keyboard>/downArrow")
                .With("Left", "<Keyboard>/leftArrow")
                .With("Right", "<Keyboard>/rightArrow");

            _moveAction.AddBinding("<Gamepad>/leftStick");
        }

        // ------------------------------------------------------------------
        // Сенсорный экран
        // ------------------------------------------------------------------

        /// <summary>
        /// Пересчитывает стик не чаще одного раза за кадр. Защита от того, что ReadMove()
        /// вызовут несколько потребителей: без неё состояние стика съезжало бы.
        /// </summary>
        private void UpdateTouchStick()
        {
            if (_lastTouchFrame == Time.frameCount) return;
            _lastTouchFrame = Time.frameCount;

            _stickValue = Vector2.zero;

            if (!EnhancedTouchSupport.enabled)
            {
                _stickFingerId = -1;
                return;
            }

            float radius = Mathf.Max(1f, Screen.height * stickRadiusScreenFraction);
            float deadZone = radius * stickDeadZoneFraction;

            // Уже удерживаем палец — продолжаем вести его.
            if (_stickFingerId >= 0 && TryFindTouch(_stickFingerId, out Touch held))
            {
                if (held.phase == TouchPhase.Ended || held.phase == TouchPhase.Canceled)
                {
                    _stickFingerId = -1;
                    return;
                }

                ApplyStick(held.screenPosition, radius, deadZone);
                return;
            }

            // Палец потерян или его не было — ищем новый.
            _stickFingerId = -1;

            foreach (Touch touch in Touch.activeTouches)
            {
                if (touch.phase == TouchPhase.Ended || touch.phase == TouchPhase.Canceled) continue;

                // TODO (эпик с UI): пропускать касания, начавшиеся над кнопками интерфейса,
                // иначе тап по способности будет заодно дёргать героя.
                _stickFingerId = touch.finger.index;
                _stickOrigin = touch.startScreenPosition;
                ApplyStick(touch.screenPosition, radius, deadZone);
                return;
            }
        }

        private static bool TryFindTouch(int fingerId, out Touch result)
        {
            foreach (Touch touch in Touch.activeTouches)
            {
                if (touch.finger.index != fingerId) continue;
                result = touch;
                return true;
            }

            result = default;
            return false;
        }

        private void ApplyStick(Vector2 currentPosition, float radius, float deadZone)
        {
            Vector2 delta = currentPosition - _stickOrigin;
            float distance = delta.magnitude;

            if (dragStickOrigin && distance > radius)
            {
                // Подтягиваем центр за пальцем, оставляя его ровно на границе радиуса.
                _stickOrigin = currentPosition - delta / distance * radius;
                delta = currentPosition - _stickOrigin;
                distance = radius;
            }

            _stickValue = distance <= deadZone
                ? Vector2.zero
                : Vector2.ClampMagnitude(delta / radius, 1f);
        }

        private void ResetStick()
        {
            _stickFingerId = -1;
            _stickValue = Vector2.zero;
            _lastTouchFrame = -1;
        }

        /// <summary>
        /// Обязательная очистка. ScriptableObject переживает выход из Play Mode,
        /// а InputAction — нативный ресурс: без Dispose получим утечку и задвоенный ввод
        /// при следующем запуске.
        /// </summary>
        private void OnDisable()
        {
            Disable();
            _moveAction?.Dispose();
            _moveAction = null;
        }
    }
}
