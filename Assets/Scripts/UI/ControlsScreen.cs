using TMPro;
using UnityEngine;
using UnityEngine.UI;
using HeroDefense.Localization;

namespace HeroDefense.UI
{
    /// <summary>
    /// Модальное окно «Управление» — то же, что показывает пауза,
    /// но доступное из меню, до боя.
    ///
    /// Игрок на itch или в Steam не читает описание страницы перед
    /// запуском: если клавиши не показаны в самой игре, их нет.
    ///
    /// Создаётся по требованию и уничтожается при закрытии: экран,
    /// который открывают раз за сессию, незачем держать в памяти.
    /// </summary>
    public sealed class ControlsScreen : MonoBehaviour
    {
        /// <summary>Выше обычных панелей меню, ниже перехода между сценами.</summary>
        private const int SortingOrder = 600;

        private static ControlsScreen _open;

        /// <summary>
        /// Показать окно. Повторный вызов при уже открытом окне ничего
        /// не делает: два наложенных экрана выглядят как поломка.
        /// </summary>
        public static void Show()
        {
            if (_open != null)
                return;

            var host = new GameObject("ControlsScreen");

            _open = host.AddComponent<ControlsScreen>();
        }

        /// <summary>Закрыть окно, если оно открыто.</summary>
        public static void Close()
        {
            if (_open != null)
                Destroy(_open.gameObject);
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
            RuntimeUi.CreateCanvas(gameObject, SortingOrder);

            RectTransform root = RuntimeUi.CreateStretched("Root", transform);

            RuntimeUi.CreateFill("Overlay", root, RuntimeUi.OverlayColor);

            RectTransform panel = RuntimeUi.CreatePanel(
                root, 800f, new RectOffset(48, 48, 40, 40), 20f);

            RuntimeUi.CreateText(panel, Loc.GetOrFallback("controls.title", "Управление"),
                                 52f, RuntimeUi.AccentColor, TextAlignmentOptions.Center);

            RuntimeUi.CreateControlsList(RuntimeUi.CreateColumn(panel, 10f), 30f);

            Button close = RuntimeUi.CreateButton(
                panel, Loc.GetOrFallback("common.back", "Назад"), 64f);

            close.onClick.AddListener(() => Destroy(gameObject));
            close.gameObject.AddComponent<Audio.UiClickSound>();
        }
    }
}
