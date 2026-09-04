using System.Text;
using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HeroDefense.Meta;

namespace HeroDefense.App.UI
{
    /// <summary>
    /// Экран итогов забега. Показывается в замке сразу после возвращения.
    ///
    /// Ничего не начисляет: престиж уже начислен по ходу боя (D118).
    /// Здесь только отчёт — иначе при поражении игрок не понимал бы,
    /// что вообще что-то заработал.
    ///
    /// Если результата нет (игрок зашёл в замок из меню, а не из боя),
    /// экран не показывается вовсе.
    /// </summary>
    public sealed class RunResultView : MonoBehaviour
    {
        [Header("Корень")]
        [Tooltip("Панель, которая включается и выключается. ⚠️ Это ДРУГОЙ объект, " +
                 "не тот, на котором висит скрипт: иначе панель выключит саму себя " +
                 "и больше не включится.")]
        [SerializeField] private GameObject panelRoot;

        [Header("Тексты")]
        [SerializeField] private TMP_Text titleText;
        [SerializeField] private TMP_Text summaryText;
        [SerializeField] private TMP_Text breakdownText;
        [SerializeField] private TMP_Text prestigeTotalText;

        [Header("Кнопки")]
        [SerializeField] private Button closeButton;

        [Header("Подписи")]
        [SerializeField] private string victoryTitle = "Забег пройден";
        [SerializeField] private string defeatTitle = "Ратуша пала";

        private void Awake()
        {
            if (panelRoot != null)
                panelRoot.SetActive(false);
        }

        private void OnEnable()
        {
            if (closeButton != null)
                closeButton.onClick.AddListener(Close);
        }

        private void OnDisable()
        {
            if (closeButton != null)
                closeButton.onClick.RemoveListener(Close);
        }

        private void Start()
        {
            if (RunResult.HasResult)
                Show();
        }

        private void Show()
        {
            if (panelRoot != null)
                panelRoot.SetActive(true);

            if (titleText != null)
                titleText.text = RunResult.Victory ? victoryTitle : defeatTitle;

            if (summaryText != null)
            {
                summaryText.text = RunResult.Victory
                    ? $"Пройдено волн: {RunResult.WaveReached}"
                    : $"Остановлен на волне {RunResult.WaveReached}";
            }

            if (breakdownText != null)
                breakdownText.text = BuildBreakdown();

            if (prestigeTotalText != null)
                prestigeTotalText.text = $"Престиж: {PlayerProgress.Prestige}";
        }

        private static string BuildBreakdown()
        {
            var text = new StringBuilder();

            foreach (string line in RunResult.Breakdown)
                text.AppendLine(line);

            text.AppendLine();
            text.AppendLine($"Всего за забег: +{RunResult.PrestigeEarned}");

            return text.ToString();
        }

        private void Close()
        {
            // Забываем результат, иначе экран всплывал бы при каждом
            // возвращении в замок.
            RunResult.Consume();

            if (panelRoot != null)
                panelRoot.SetActive(false);
        }
    }
}
