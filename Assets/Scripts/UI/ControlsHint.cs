using TMPro;
using UnityEngine;
using UnityEngine.SceneManagement;
using UnityEngine.UI;
using HeroDefense.Core;

namespace HeroDefense.UI
{
    /// <summary>
    /// Подсказка управления в начале боя.
    ///
    /// Закрывает главный пробел прототипа: клавиши не показаны нигде,
    /// а игрок не читает описание страницы перед тем, как нажать «играть».
    /// Особенно это касается строительства — оно открывается подъездом
    /// к площадке, и сам этот способ никто не угадывает.
    ///
    /// Гаснет само: подсказка, висящая весь бой, превращается в мусор
    /// на экране. Вернуться к ней можно на паузе.
    /// </summary>
    [DefaultExecutionOrder(-600)]
    public sealed class ControlsHint : MonoBehaviour
    {
        /// <summary>Ниже паузы: пауза должна перекрывать подсказку, а не наоборот.</summary>
        private const int SortingOrder = 400;

        /// <summary>
        /// Сколько секунд подсказка держится. Считается от начала боя
        /// по неотмасштабированному времени: на паузе она читается,
        /// а не тикает в пустоту.
        /// </summary>
        private const float HoldTime = 14f;

        /// <summary>Сколько секунд гаснет.</summary>
        private const float FadeTime = 1.2f;

        private CanvasGroup _group;
        private float _timer;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Только боевая карта: признак — партия, то есть GameState.
            if (mode != LoadSceneMode.Single || GameState.Current == null)
                return;

            var host = new GameObject("ControlsHint");

            host.AddComponent<ControlsHint>();
        }

        private void Start()
        {
            Build();
            _timer = HoldTime;
        }

        private void Update()
        {
            // Пока на паузе, подсказка не гаснет: игрок мог поставить паузу
            // именно затем, чтобы её дочитать.
            GameState state = GameState.Current;

            if (state != null && state.Phase == GamePhase.Paused)
                return;

            _timer -= Time.unscaledDeltaTime;

            if (_timer > 0f)
                return;

            _group.alpha = Mathf.MoveTowards(_group.alpha, 0f, Time.unscaledDeltaTime / FadeTime);

            // Погасшая подсказка перехватывала бы клики, поэтому её
            // не прячут, а удаляют совсем.
            if (_group.alpha <= 0f)
                Destroy(gameObject);
        }

        private void Build()
        {
            RuntimeUi.CreateCanvas(gameObject, SortingOrder);

            RectTransform root = RuntimeUi.CreateStretched("Root", transform);

            _group = root.gameObject.AddComponent<CanvasGroup>();

            // Подсказка не должна ловить клики: под ней панель строительства.
            _group.blocksRaycasts = false;
            _group.interactable = false;

            var host = new GameObject("Hint", typeof(RectTransform));

            var rect = host.GetComponent<RectTransform>();

            rect.SetParent(root, false);

            // Левый край, но выше низа: центр экрана занят боем, правый —
            // карточками построек, а в самом низу слева стоит панель короля.
            rect.anchorMin = new Vector2(0f, 0f);
            rect.anchorMax = new Vector2(0f, 0f);
            rect.pivot = new Vector2(0f, 0f);
            rect.anchoredPosition = new Vector2(40f, 190f);
            rect.sizeDelta = new Vector2(660f, 0f);

            var background = host.AddComponent<Image>();

            UiSkin skin = RuntimeUi.Skin;

            RuntimeUi.ApplyPanelLook(background, skin != null ? skin.panelCompact : null);

            var layout = host.AddComponent<VerticalLayoutGroup>();

            layout.padding = new RectOffset(28, 28, 22, 22);
            layout.childControlHeight = true;
            layout.childControlWidth = true;
            layout.childForceExpandHeight = false;

            var fitter = host.AddComponent<ContentSizeFitter>();

            fitter.verticalFit = ContentSizeFitter.FitMode.PreferredSize;

            RuntimeUi.CreateControlsList(RuntimeUi.CreateColumn(host.transform, 8f), 26f);
        }
    }
}
