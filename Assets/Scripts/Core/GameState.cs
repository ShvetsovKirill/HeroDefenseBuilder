using System;
using UnityEngine;

namespace HeroDefense.Core
{
    public enum GamePhase
    {
        /// <summary>Идёт бой.</summary>
        Playing,

        /// <summary>Пауза по желанию игрока.</summary>
        Paused,

        /// <summary>Ратуша разрушена.</summary>
        Defeat,

        /// <summary>Все волны карты пройдены.</summary>
        Victory
    }

    /// <summary>
    /// Состояние партии.
    ///
    /// Заменяет Time.timeScale = 0 как способ паузы. Проблема timeScale
    /// была в том, что он глобальный и переживает смену сцены: выйти
    /// из карты в замороженном состоянии — значит получить намертво
    /// вставшую следующую карту.
    ///
    /// Здесь пауза — это флаг, на который системы реагируют сами.
    /// timeScale используется, но им владеет ровно один класс,
    /// и он гарантированно возвращает его при выгрузке.
    /// </summary>
    [DefaultExecutionOrder(-900)]
    public sealed class GameState : MonoBehaviour
    {
        [Header("Настройки")]
        [Tooltip("Останавливать ли время при паузе. Выключено — игра продолжает " +
                 "идти под панелью, что нужно для перебивок без разрыва темпа.")]
        [SerializeField] private bool freezeTimeOnPause = true;

        private static GameState _current;

        public static GameState Current => _current;

        public GamePhase Phase { get; private set; } = GamePhase.Playing;

        /// <summary>Идёт ли активный бой. Основная проверка для игровых систем.</summary>
        public bool IsPlaying => Phase == GamePhase.Playing;

        /// <summary>Партия закончена — победой или поражением.</summary>
        public bool IsFinished => Phase is GamePhase.Defeat or GamePhase.Victory;

        public event Action<GamePhase> PhaseChanged;

        private void Awake()
        {
            if (_current != null && _current != this)
            {
                Debug.LogError("[GameState] Уже есть состояние игры на сцене.", this);
                return;
            }

            _current = this;
            Phase = GamePhase.Playing;
        }

        private void OnDestroy()
        {
            if (_current != this)
                return;

            // Обязательно: иначе следующая карта загрузится замороженной.
            Time.timeScale = 1f;
            _current = null;
        }

        public void SetPhase(GamePhase phase)
        {
            if (Phase == phase)
                return;

            Phase = phase;
            ApplyTimeScale();

            PhaseChanged?.Invoke(phase);
        }

        public void Pause() => SetPhase(GamePhase.Paused);
        public void Resume() => SetPhase(GamePhase.Playing);
        public void Defeat() => SetPhase(GamePhase.Defeat);
        public void Victory() => SetPhase(GamePhase.Victory);

        public void TogglePause()
        {
            if (Phase == GamePhase.Playing)
                Pause();
            else if (Phase == GamePhase.Paused)
                Resume();
        }

        private void ApplyTimeScale()
        {
            bool shouldFreeze = Phase == GamePhase.Paused
                ? freezeTimeOnPause
                : IsFinished;

            Time.timeScale = shouldFreeze ? 0f : 1f;
        }
    }
}
