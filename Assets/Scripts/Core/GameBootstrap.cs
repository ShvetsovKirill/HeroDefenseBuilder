using UnityEngine;
using HeroDefense.Hero;
using HeroDefense.Input;
using HeroDefense.CameraRig;

namespace HeroDefense.Core
{
    /// <summary>
    /// Composition root. Единственное место в проекте, которое знает,
    /// какая конкретная реализация ввода используется.
    ///
    /// Переход на мобильный ввод = изменить одну строку в CreateInput().
    /// Ни герой, ни камера при этом не трогаются.
    /// </summary>
    public sealed class GameBootstrap : MonoBehaviour
    {
        public enum InputMode
        {
            Keyboard,
            // Touch,   // сюда добавится тап/джойстик
        }

        [Header("Конфигурация")]
        [SerializeField] private InputMode inputMode = InputMode.Keyboard;

        [Header("Сцена")]
        [SerializeField] private HeroMotor hero;
        [SerializeField] private IsometricCameraFollow cameraFollow;

        private void Awake()
        {
            if (hero == null)
            {
                Debug.LogError("[Bootstrap] Не назначен HeroMotor.", this);
                return;
            }

            IMovementInput input = CreateInput(inputMode);
            hero.SetInput(input);

            if (cameraFollow != null)
                cameraFollow.SetTarget(hero.transform);

            Debug.Log($"[Bootstrap] Инициализация завершена. Режим ввода: {inputMode}");
        }

        private static IMovementInput CreateInput(InputMode mode)
        {
            switch (mode)
            {
                case InputMode.Keyboard:
                default:
                    return new KeyboardMovementInput();
            }
        }
    }
}
