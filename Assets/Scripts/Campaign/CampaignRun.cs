using System;
using UnityEngine;

namespace HeroDefense.Campaign
{
    /// <summary>
    /// Идущая кампания: доступ к состоянию и его сохранение.
    ///
    /// Статический, как <c>PlayerProgress</c> и <c>RunLoadout</c>: кампанию
    /// читают карта, лагерь и боевая сцена, то есть три сцены сразу,
    /// а переживать их смену состояние обязано.
    ///
    /// Хранилище — PlayerPrefs с JSON внутри, как у <c>PlayerProgress</c>.
    /// Не потому что это лучшее решение, а потому что оно уже работает
    /// в проекте и не тянет зависимостей. Кампания длится три часа,
    /// и сохранение посреди неё — не роскошь.
    /// </summary>
    public static class CampaignRun
    {
        private const string SaveKey = "campaign.state";

        private static CampaignState _state;
        private static bool _loaded;

        /// <summary>Состояние изменилось: карта и лагерь перерисовывают себя.</summary>
        public static event Action Changed;

        /// <summary>Есть ли начатая кампания. Ложь — игрок ещё в лагере.</summary>
        public static bool IsActive
        {
            get
            {
                EnsureLoaded();
                return _state != null && !_state.finished;
            }
        }

        /// <summary>
        /// Состояние текущей кампании. Никогда не null: если кампании нет,
        /// возвращается пустое состояние — потребителям не приходится
        /// проверять null на каждом обращении.
        /// </summary>
        public static CampaignState State
        {
            get
            {
                EnsureLoaded();
                return _state ??= new CampaignState();
            }
        }

        /// <summary>
        /// Начать новую кампанию по этой карте. Всё, что было, стирается:
        /// провал означает «заново, всё с нуля», кроме купленного в лагере.
        /// </summary>
        public static void Begin(CampaignMapDefinition map, CampaignRules rules)
        {
            _loaded = true;
            _state = new CampaignState { mapId = map != null ? map.name : string.Empty };

            if (rules != null)
                rules.FillStartingArmy(_state);

            Save();
        }

        /// <summary>Кампания окончена — победой или потерей армии.</summary>
        public static void Finish()
        {
            State.finished = true;
            Save();
        }

        /// <summary>Записать состояние. Зовётся после каждого значимого шага.</summary>
        public static void Save()
        {
            if (_state == null)
                return;

            PlayerPrefs.SetString(SaveKey, JsonUtility.ToJson(_state));
            PlayerPrefs.Save();

            Changed?.Invoke();
        }

        /// <summary>Забыть кампанию. Нужно для «начать заново» и для отладки.</summary>
        public static void Clear()
        {
            _state = new CampaignState();
            _loaded = true;

            PlayerPrefs.DeleteKey(SaveKey);
            PlayerPrefs.Save();

            Changed?.Invoke();
        }

        private static void EnsureLoaded()
        {
            if (_loaded)
                return;

            _loaded = true;

            string json = PlayerPrefs.GetString(SaveKey, string.Empty);

            if (string.IsNullOrEmpty(json))
            {
                _state = new CampaignState();
                return;
            }

            // Битое сохранение не должно ронять игру: теряем кампанию,
            // но не доступ к лагерю и не весь прогресс игрока.
            try
            {
                _state = JsonUtility.FromJson<CampaignState>(json) ?? new CampaignState();
            }
            catch (Exception error)
            {
                Debug.LogError($"[Кампания] Сохранение не читается, начинаем заново: {error.Message}");
                _state = new CampaignState();
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Сброс статики при выходе из Play Mode: без него кампания
        /// прошлого запуска досталась бы следующему.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            _state = null;
            _loaded = false;
            Changed = null;
        }
#endif
    }
}
