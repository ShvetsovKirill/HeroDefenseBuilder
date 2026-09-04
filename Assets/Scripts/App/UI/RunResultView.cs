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

            // Подписи берутся из таблицы переводов, а не из полей в инспекторе:
            // иначе при смене языка экран итогов остался бы на русском.
            if (titleText != null)
                titleText.text = Localization.Loc.Get(RunResult.Victory ? "result.victory" : "result.defeat");

            if (summaryText != null)
            {
                summaryText.text = Localization.Loc.Get(
                    RunResult.Victory ? "result.waves" : "result.stopped", RunResult.WaveReached);
            }

            if (breakdownText != null)
                breakdownText.text = BuildBreakdown();

            if (prestigeTotalText != null)
                prestigeTotalText.text = Localization.Loc.Get("prestige.label", PlayerProgress.Prestige);
        }

        private static string BuildBreakdown()
        {
            var text = new StringBuilder();

            foreach (string line in RunResult.Breakdown)
                text.AppendLine(line);

            text.AppendLine();
            text.AppendLine(Localization.Loc.Get("result.total", RunResult.PrestigeEarned));

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
