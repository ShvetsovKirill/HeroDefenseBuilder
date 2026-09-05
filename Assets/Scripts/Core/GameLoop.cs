using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using HeroDefense.Base;
using HeroDefense.Waves;

namespace HeroDefense.Core
{
    /// <summary>
    /// Связывает игровые события с состоянием партии.
    ///
    /// Сам ничего не решает про время и UI: только переводит GameState
    /// в нужную фазу, а на неё уже реагируют системы и экраны.
    ///
    /// Раньше здесь стоял Time.timeScale = 0 напрямую — он глобальный
    /// и переживал смену сцены, из-за чего следующая карта могла
    /// загрузиться замороженной.
    /// </summary>
    public sealed class GameLoop : MonoBehaviour
    {
        [Header("Ссылки")]
        [Tooltip("Раннер волн этой карты. Ратуша и остальные системы " +
                 "берутся из SceneContext — дублировать их здесь не нужно.")]
        [SerializeField] private WaveRunner waveRunner;

        [Header("Управление")]
        [Tooltip("Разрешить рестарт по Enter после поражения. " +
                 "Временно — пока нет нормального экрана.")]
        [SerializeField] private bool allowQuickRestart = true;

        public bool IsGameOver => State != null && State.IsFinished;

        private static GameState State => GameState.Current;
        private static TownHall Hall => SceneContext.Current?.TownHall;

        private void OnEnable()
        {
            if (Hall != null)
                Hall.Destroyed += OnTownHallDestroyed;

            if (waveRunner != null)
                waveRunner.LevelCompleted += OnLevelCompleted;
        }

        private void OnDisable()
        {
            if (Hall != null)
                Hall.Destroyed -= OnTownHallDestroyed;

            if (waveRunner != null)
                waveRunner.LevelCompleted -= OnLevelCompleted;
        }

        private void Update()
        {
            HandleHotkeys();
        }

        private void HandleHotkeys()
        {
            Keyboard keyboard = Keyboard.current;

            if (keyboard == null || State == null)
                return;

            // Две клавиши на паузу, а не одна: Escape перехватывают
            // оверлеи Steam и полноэкранный режим, и нажатие до игры
            // не доходит. P свободна и работает всегда.
            bool pausePressed = keyboard.escapeKey.wasPressedThisFrame
                                || keyboard.pKey.wasPressedThisFrame;

            if (pausePressed && !State.IsFinished)
                State.TogglePause();

            if (allowQuickRestart && State.IsFinished && keyboard.enterKey.wasPressedThisFrame)
                Restart();
        }

        private void OnTownHallDestroyed()
        {
            State?.Defeat();
            Debug.Log("[GameLoop] Ратуша разрушена. Поражение.");
        }

        private void OnLevelCompleted()
        {
            // Победа только если ратуша цела: волны могут закончиться
            // ровно в тот момент, когда её добивают.
            if (State != null && State.IsFinished)
                return;

            State?.Victory();
            Debug.Log("[GameLoop] Карта пройдена.");
        }

        public void Restart()
        {
            // GameState вернёт timeScale при уничтожении, но делаем это явно:
            // загрузка сцены не должна зависеть от порядка OnDestroy.
            Time.timeScale = 1f;

            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
