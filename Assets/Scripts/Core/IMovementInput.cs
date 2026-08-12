using UnityEngine;

namespace HeroDefense.Input
{
    /// <summary>
    /// Единственный контракт между вводом и героем.
    /// Отдаёт желаемое направление движения на плоскости XZ.
    /// Магнитуда 0..1: 0 — стоять, 1 — полная скорость.
    ///
    /// Клавиатура, виртуальный джойстик, тап-to-move — все они реализуют
    /// этот интерфейс. Герой не знает, кто именно за ним стоит.
    /// </summary>
    public interface IMovementInput
    {
        Vector2 GetMoveDirection();
    }
}
