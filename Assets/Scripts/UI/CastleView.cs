using System;
using System.Collections.Generic;
using UnityEngine;
using UnityEngine.UI;

namespace HeroDefense.App.UI
{
    /// <summary>
    /// Экран замка (D91).
    ///
    /// Собирает кликабельные здания и показывает панель нужного раздела.
    /// Панели пока пустые заглушки — их содержимое появится вместе
    /// с системами найма и прокачки.
    ///
    /// Открыт может быть только один раздел: иначе панели наложатся,
    /// и непонятно, где ты находишься.
    /// </summary>
    public sealed class CastleView : MonoBehaviour
    {
        [Serializable]
        public sealed class Section
        {
            [Tooltip("Должен совпадать с Section Id на здании.")]
            public string id;

            [Tooltip("Панель этого раздела. Показывается при клике по зданию.")]
            public GameObject panel;
        }

        [Header("Разделы")]
        [SerializeField] private Section[] sections = Array.Empty<Section>();

        [Header("Кнопки")]
        [Tooltip("Отправиться в забег.")]
        [SerializeField] private Button departButton;

        [Tooltip("Вернуться в главное меню.")]
        [SerializeField] private Button backButton;

        [Header("Закрытие")]
        [Tooltip("Затемнение позади открытой панели. Клик по нему закрывает раздел.")]
        [SerializeField] private Button backdrop;

        private readonly List<CastleBuildingButton> _buildings = new();

        private void Awake()
        {
            CollectBuildings();
            CloseAllSections();

            if (departButton != null)
                departButton.onClick.AddListener(Depart);

            if (backButton != null)
                backButton.onClick.AddListener(BackToMenu);

            if (backdrop != null)
                backdrop.onClick.AddListener(CloseAllSections);
        }

        private void OnDestroy()
        {
            for (int i = 0; i < _buildings.Count; i++)
            {
                if (_buildings[i] != null)
                    _buildings[i].Clicked -= OpenSection;
            }
        }

        private void CollectBuildings()
        {
            GetComponentsInChildren(true, _buildings);

            for (int i = 0; i < _buildings.Count; i++)
                _buildings[i].Clicked += OpenSection;
        }

        // ---------- Разделы ----------

        private void OpenSection(string id)
        {
            bool found = false;

            for (int i = 0; i < sections.Length; i++)
            {
                bool match = sections[i].id == id;

                found |= match;

                if (sections[i].panel != null)
                    sections[i].panel.SetActive(match);
            }

            if (backdrop != null)
                backdrop.gameObject.SetActive(found);

            if (!found)
                Debug.LogWarning($"[CastleView] Нет панели для раздела «{id}».", this);
        }

        public void CloseAllSections()
        {
            for (int i = 0; i < sections.Length; i++)
            {
                if (sections[i].panel != null)
                    sections[i].panel.SetActive(false);
            }

            if (backdrop != null)
                backdrop.gameObject.SetActive(false);
        }

        // ---------- Переходы ----------

        private void Depart()
        {
            // Позже здесь появится проверка: выбран ли король,
            // взят ли хотя бы один отряд.
            SceneLoader.Instance?.GoToBattle();
        }

        private void BackToMenu()
        {
            SceneLoader.Instance?.GoToMainMenu();
        }
    }
}
