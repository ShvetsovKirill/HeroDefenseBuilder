using UnityEngine;
using UnityEngine.InputSystem;

namespace HeroDefense.Input
{
    /// <summary>
    /// WASD и стрелки. Читаем устройство напрямую через New Input System —
    /// без .inputactions-ассета, чтобы первый запуск был без ручной настройки.
    /// Позже это можно заменить на InputActions-ассет, не трогая героя.
    /// </summary>
    public sealed class KeyboardMovementInput : IMovementInput
    {
        public Vector2 GetMoveDirection()
        {
            Keyboard kb = Keyboard.current;
            if (kb == null)
                return Vector2.zero; // на всякий случай: клавиатуры может не быть (например, чистый WebGL-тач)

            float x = 0f;
            float y = 0f;

            if (kb.aKey.isPressed || kb.leftArrowKey.isPressed)  x -= 1f;
            if (kb.dKey.isPressed || kb.rightArrowKey.isPressed) x += 1f;
            if (kb.sKey.isPressed || kb.downArrowKey.isPressed)  y -= 1f;
            if (kb.wKey.isPressed || kb.upArrowKey.isPressed)    y += 1f;

            Vector2 dir = new Vector2(x, y);

            // По диагонали не даём скорость в 1.41 раза больше.
            return dir.sqrMagnitude > 1f ? dir.normalized : dir;
        }
    }
}
