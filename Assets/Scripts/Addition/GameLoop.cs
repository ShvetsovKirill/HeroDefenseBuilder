using UnityEngine;
using UnityEngine.InputSystem;
using UnityEngine.SceneManagement;
using HeroDefense.Base;

namespace HeroDefense.Core
{
    /// <summary>
    /// Состояние партии: играем или проиграли.
    ///
    /// Пока делает минимум — останавливает время и перезагружает сцену.
    /// Позже отсюда вырастет экран поражения и переход между картами (эпик 2.7).
    /// </summary>
    public sealed class GameLoop : MonoBehaviour
    {
        [SerializeField] private TownHall townHall;

        public bool IsGameOver { get; private set; }

        private void OnEnable()
        {
            if (townHall != null)
                townHall.Destroyed += OnTownHallDestroyed;
        }

        private void OnDisable()
        {
            if (townHall != null)
                townHall.Destroyed -= OnTownHallDestroyed;
        }

        private void Update()
        {
            if (IsGameOver)
                HandleRestartInput();
        }

        private void OnTownHallDestroyed()
        {
            IsGameOver = true;

            // Пауза через timeScale — временное решение.
            // Позже вместо этого будет экран поражения без остановки времени.
            Time.timeScale = 0f;

            Debug.Log("[GameLoop] Ратуша разрушена. Игра окончена.");
        }

        private void HandleRestartInput()
        {
            Keyboard kb = Keyboard.current;

            if (kb == null || !kb.enterKey.wasPressedThisFrame)
                return;

            Restart();
        }

        public void Restart()
        {
            Time.timeScale = 1f;
            SceneManager.LoadScene(SceneManager.GetActiveScene().buildIndex);
        }
    }
}
