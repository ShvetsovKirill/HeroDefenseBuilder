using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HeroDefense.Localization;
using HeroDefense.UI;

namespace HeroDefense.Campaign.UI
{
    /// <summary>
    /// Итог похода: чем всё кончилось и что дальше.
    ///
    /// Закрывает последнюю дыру в цикле. До этого экрана победа
    /// существовала только в логе: игрок выигрывал последнюю битву
    /// и молча оказывался в лагере, не понимая, кончилась ли кампания
    /// и почему карта опустела.
    ///
    /// Показывается один раз — по нажатию завершённая кампания стирается,
    /// и лагерь снова предлагает выйти в поход. Купленное в лагере
    /// остаётся: с нуля начинается поход, а не игра.
    /// </summary>
    public sealed class CampaignResultScreen : MonoBehaviour
    {
        private const int SortingOrder = 700;

        private static CampaignResultScreen _open;

        /// <summary>
        /// Показать итог, если он есть. Зовётся лагерем при входе:
        /// проверка «а был ли поход» внутри, чтобы вызывающему
        /// не приходилось знать устройство состояния.
        /// </summary>
        public static void ShowIfFinished()
        {
            if (_open != null)
                return;

            CampaignState state = CampaignRun.State;

            // Пустое состояние — это не проигранный поход, а его отсутствие:
            // карта не выбрана, значит игрок вообще не выходил.
            if (!state.finished || string.IsNullOrEmpty(state.mapId))
                return;

            var host = new GameObject("CampaignResultScreen");

            _open = host.AddComponent<CampaignResultScreen>();
        }

        private void Awake()
        {
            _open = this;
            Build();
        }

        private void OnDestroy()
        {
            if (_open == this)
                _open = null;
        }

        private void Build()
        {
            CampaignState state = CampaignRun.State;

            RuntimeUi.CreateCanvas(gameObject, SortingOrder);

            RectTransform root = RuntimeUi.CreateStretched("Root", transform);

            RuntimeUi.CreateFill("Overlay", root, RuntimeUi.OverlayColor);

            RectTransform panel = RuntimeUi.CreatePanel(
                root, 820f, new RectOffset(56, 56, 44, 44), 18f);

            string title = state.victory
                ? Loc.GetOrFallback("result.win", "Королевство отстояно")
                : Loc.GetOrFallback("result.lose", "Поход окончен");

            RuntimeUi.CreateText(panel, title, 46f,
                                 state.victory ? RuntimeUi.AccentColor : LossColor,
                                 TextAlignmentOptions.Center);

            RuntimeUi.CreateText(panel, Describe(state), 26f,
                                 RuntimeUi.TextColor, TextAlignmentOptions.Center);

            Button close = RuntimeUi.CreateButton(
                panel, Loc.GetOrFallback("result.next", "Новый поход"), 60f);

            close.onClick.AddListener(Acknowledge);
            close.gameObject.AddComponent<Audio.UiClickSound>();
        }

        /// <summary>Цвет заголовка поражения. Не красный крик, а тусклая медь.</summary>
        private static readonly Color LossColor = new(0.85f, 0.45f, 0.36f);

        /// <summary>
        /// Строка итога: сколько прошёл и с чем остался. Числа, а не
        /// похвала — по ним игрок сравнит следующий поход с этим.
        /// </summary>
        private static string Describe(CampaignState state)
        {
            string cleared = Loc.GetOrFallback("result.cleared", "Владений взято: {0}");
            string army = state.HasArmy
                ? string.Format(Loc.GetOrFallback("result.army", "Отрядов уцелело: {0}"),
                                state.AliveSquadCount)
                : Loc.GetOrFallback("result.wiped", "Армии не осталось");

            return string.Format(cleared, state.holdingsCleared) + System.Environment.NewLine + army;
        }

        /// <summary>
        /// Итог принят. Завершённая кампания стирается: она уже ничего
        /// не хранит, а пока она лежит в сохранении, лагерь показывал бы
        /// этот экран при каждом входе.
        /// </summary>
        private void Acknowledge()
        {
            CampaignRun.Clear();
            Destroy(gameObject);
        }
    }
}
