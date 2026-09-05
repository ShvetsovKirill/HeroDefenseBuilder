using UnityEngine;
using UnityEngine.UI;

namespace HeroDefense.Audio
{
    /// <summary>
    /// Щелчок по кнопке. Вешается на объект с кнопкой или на общего родителя —
    /// тогда озвучиваются все кнопки внутри.
    ///
    /// Отдельный компонент, а не вызов в каждом экране: экранов уже семь,
    /// и добавлять строку в каждый обработчик — значит однажды забыть.
    /// Подписка идёт через <c>onClick</c>, поэтому кнопка, выключенная
    /// по <c>interactable</c>, молчит сама собой — как и должна.
    /// </summary>
    [DisallowMultipleComponent]
    public sealed class UiClickSound : MonoBehaviour
    {
        [Tooltip("Какой звук играет. Обычно UiClick; для «назад» или отказа " +
                 "можно поставить другой.")]
        [SerializeField] private SoundId sound = SoundId.UiClick;

        [Tooltip("Озвучить и кнопки внутри дочерних объектов. Включено — " +
                 "компонент можно повесить один раз на корень экрана.")]
        [SerializeField] private bool includeChildren = true;

        private Button[] _buttons;

        private void Awake()
        {
            _buttons = includeChildren
                ? GetComponentsInChildren<Button>(true)
                : GetComponents<Button>();

            foreach (Button button in _buttons)
                button.onClick.AddListener(OnClicked);
        }

        private void OnDestroy()
        {
            if (_buttons == null)
                return;

            // Отписка обязательна: экраны переиспользуются между показами,
            // а onClick переживает выключение объекта.
            foreach (Button button in _buttons)
            {
                if (button != null)
                    button.onClick.RemoveListener(OnClicked);
            }
        }

        private void OnClicked() => Sfx.Play(sound);
    }
}
