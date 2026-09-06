using UnityEngine;
using UnityEngine.SceneManagement;
using HeroDefense.App;
using HeroDefense.Core;
using HeroDefense.Waves;

namespace HeroDefense.Campaign
{
    /// <summary>
    /// Связывает бой с кампанией: подставляет владение перед началом
    /// и разбирает итог после конца.
    ///
    /// Создаётся сам на боевой сцене, как и остальные служебные объекты.
    /// Если кампании нет — молча уходит: боевую сцену запускают напрямую
    /// из редактора сотню раз за день, и она обязана работать сама по себе.
    ///
    /// Отдельный класс, а не правка `GameLoop`: тот отвечает за фазы
    /// партии и ничего не должен знать про карту, награды и походы.
    /// </summary>
    [DefaultExecutionOrder(-800)]
    public sealed class CampaignBattleBridge : MonoBehaviour
    {
        /// <summary>
        /// Сколько ждать перед возвратом на карту. Реальные секунды:
        /// конец боя ставит время в ноль.
        /// </summary>
        private const float ReturnDelay = 3f;

        private GameState _state;
        private bool _reported;

        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.BeforeSceneLoad)]
        private static void Install()
        {
            SceneManager.sceneLoaded -= OnSceneLoaded;
            SceneManager.sceneLoaded += OnSceneLoaded;
        }

        private static void OnSceneLoaded(Scene scene, LoadSceneMode mode)
        {
            // Признак боевой карты — партия. Кампания проверяется позже:
            // объект нужен и для того, чтобы понять, что её нет.
            if (mode != LoadSceneMode.Single || GameState.Current == null)
                return;

            var host = new GameObject("CampaignBattleBridge");

            host.AddComponent<CampaignBattleBridge>();
        }

        private void Start()
        {
            if (!CampaignRun.IsActive)
            {
                Destroy(gameObject);
                return;
            }

            // Счётчик выданных отрядов свой на каждый бой: иначе во втором
            // владении казармы начали бы раздавать отряды с середины списка.
            CampaignArmy.ResetForBattle();

            // Колода восстанавливается до первой постройки: реестр читает
            // её при каждом обращении, и пустая означала бы бой, в котором
            // ничего нельзя поставить.
            CampaignBuildDeck.RestoreFromState();

            ApplyHolding();

            _state = GameState.Current;
            _state.PhaseChanged += OnPhaseChanged;
        }

        private void OnDestroy()
        {
            if (_state != null)
                _state.PhaseChanged -= OnPhaseChanged;
        }

        /// <summary>
        /// Настроить бой под выбранное владение: какие волны и что
        /// защищаем.
        /// </summary>
        private void ApplyHolding()
        {
            HoldingDefinition holding = CampaignFlow.CurrentHolding;

            if (holding == null)
            {
                Debug.LogWarning("[Кампания] Бой начат, но владение неизвестно. " +
                                 "Играем то, что стоит в сцене.");
                return;
            }

            if (holding.level != null)
            {
                WaveRunner runner = FindFirstObjectByType<WaveRunner>();

                // Уровень подменяется до Start раннера: у моста порядок
                // выполнения -800, у раннера обычный.
                if (runner != null)
                    runner.SetLevel(holding.level);
            }

            Debug.Log($"[Кампания] Владение «{holding.DisplayName}», " +
                      $"волн: {holding.WaveCount}, нападают: {holding.faction}.");
        }

        private void OnPhaseChanged(GamePhase phase)
        {
            if (_reported)
                return;

            switch (phase)
            {
                case GamePhase.Victory:
                    Report(true);
                    break;

                case GamePhase.Defeat:
                    Report(false);
                    break;
            }
        }

        /// <summary>
        /// Сообщить кампании итог и вернуться на карту.
        ///
        /// Один раз за бой: фаза может смениться дважды подряд, если волны
        /// кончились ровно в тот момент, когда добивают центральное здание.
        /// </summary>
        private void Report(bool victory)
        {
            _reported = true;

            CampaignFlow.FinishHolding(victory);
            StartCoroutine(ReturnToMap());
        }

        private System.Collections.IEnumerator ReturnToMap()
        {
            yield return new WaitForSecondsRealtime(ReturnDelay);

            // Кампания кончилась — игроку в лагерь, подводить итоги.
            if (!CampaignRun.IsActive)
                SceneLoader.Instance?.GoToCastle();
            else
                SceneLoader.Instance?.GoToMap();
        }
    }
}
