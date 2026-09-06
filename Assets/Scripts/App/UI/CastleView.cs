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

            // Поход кончился — игрока вернули сюда, и первое, что он должен
            // увидеть, это его итог, а не молча опустевшую кнопку «В поход».
            Campaign.UI.CampaignResultScreen.ShowIfFinished();
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
            // Казармы в лагере — это войско: кому дать командира и что
            // делать с осиротевшим отрядом. Экран собирается кодом,
            // поэтому панель в сцене для него не нужна.
            if (id == "barracks" && Campaign.CampaignRules.Current != null)
            {
                Campaign.UI.ArmyScreen.Show();
                return;
            }

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

        /// <summary>
        /// Выйти в поход.
        ///
        /// Из лагеря игрок уходит не в бой, а на карту: куда именно идти,
        /// он решает там. Начатая кампания продолжается с того места,
        /// где остановилась, — заново её не создаём, иначе трёхчасовой
        /// поход обнулялся бы визитом в лагерь.
        /// </summary>
        private void Depart()
        {
            if (!Campaign.CampaignRun.IsActive)
                Campaign.CampaignFlow.BeginCampaign();

            // Колода собирается один раз на поход. Спрашиваем только когда
            // её нет: возвращение на карту посреди кампании не должно
            // каждый раз упираться в экран сборки.
            if (!Campaign.CampaignRun.State.HasDeck)
            {
                Campaign.UI.DeckScreen.Show(() => SceneLoader.Instance?.GoToMap());
                return;
            }

            SceneLoader.Instance?.GoToMap();
        }

        private void BackToMenu()
        {
            SceneLoader.Instance?.GoToMainMenu();
        }
    }
}
