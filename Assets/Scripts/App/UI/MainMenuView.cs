using UnityEngine;
using UnityEngine.UI;

namespace HeroDefense.App.UI
{
    /// <summary>
    /// Главное меню: Новая игра / Настройки / Выход.
    ///
    /// Кнопки не знают, куда ведут — они зовут методы этого класса,
    /// а он обращается к загрузчику. Так переходы остаются в одном месте.
    /// </summary>
    public sealed class MainMenuView : MonoBehaviour
    {
        [Header("Кнопки")]
        [SerializeField] private Button newGameButton;
        [SerializeField] private Button settingsButton;
        [SerializeField] private Button quitButton;

        [Header("Панели")]
        [Tooltip("Корень основного меню. Прячется, когда открыты настройки.")]
        [SerializeField] private GameObject rootPanel;

        [SerializeField] private GameObject settingsPanel;

        private void Awake()
        {
            Bind(newGameButton, OnNewGame);
            Bind(settingsButton, OpenSettings);
            Bind(quitButton, OnQuit);

            ShowSettings(false);
        }

        private static void Bind(Button button, UnityEngine.Events.UnityAction action)
        {
            if (button == null)
                return;

            button.onClick.RemoveAllListeners();
            button.onClick.AddListener(action);
        }

        // ---------- Действия ----------

        private void OnNewGame()
        {
            // Пока сразу в замок. Позже здесь появится выбор:
            // продолжить сохранённый забег или начать новый.
            SceneLoader.Instance?.GoToCastle();
        }

        public void OpenSettings() => ShowSettings(true);

        public void CloseSettings() => ShowSettings(false);

        private void ShowSettings(bool visible)
        {
            if (settingsPanel != null)
                settingsPanel.SetActive(visible);

            if (rootPanel != null)
                rootPanel.SetActive(!visible);
        }

        private void OnQuit()
        {
            // В редакторе Application.Quit ничего не делает —
            // без этой ветки кнопка выглядит сломанной при проверке.
#if UNITY_EDITOR
            UnityEditor.EditorApplication.isPlaying = false;
#else
            Application.Quit();
#endif
        }
    }
}
