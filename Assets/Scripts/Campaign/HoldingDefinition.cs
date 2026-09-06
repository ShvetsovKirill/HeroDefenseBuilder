using System;
using UnityEngine;
using HeroDefense.Localization;
using HeroDefense.Waves;

namespace HeroDefense.Campaign
{
    /// <summary>
    /// Одно владение на карте кампании: что за место, кто нападает,
    /// сколько длится бой и что дают за помощь.
    ///
    /// ScriptableObject, потому что владений будут десятки и крутить их
    /// должен дизайнер (D48). Сам бой при этом не переизобретается:
    /// владение ссылается на готовый <see cref="LevelDefinition"/> —
    /// конструктор волн остаётся тем же, что и был.
    /// </summary>
    [CreateAssetMenu(fileName = "Holding", menuName = "HeroDefense/Владение")]
    public sealed class HoldingDefinition : ScriptableObject
    {
        [Header("Описание")]
        [Tooltip("Название на случай, если ключ перевода не проставлен.")]
        public string displayName = "Село";

        [Tooltip("Ключ названия в таблице переводов. Пусто — текст выше.")]
        public string nameKey;

        [TextArea(2, 3)]
        [Tooltip("Строка на карте: чем это место живёт и что там стряслось.")]
        public string description;

        [Tooltip("Ключ описания. Пусто — текст из поля выше.")]
        public string descriptionKey;

        [Tooltip("Значок на карте. Необязателен.")]
        public Sprite icon;

        [Header("Что за место")]
        [Tooltip("Тип владения. Задаёт длину боя, награду и то, что защищаем.")]
        public HoldingKind kind = HoldingKind.Village;

        [Tooltip("Что защищаем: дом старейшины, ратуша, донжон, шатёр. " +
                 "Пусто — возьмётся то, что стоит в боевой сцене.")]
        public GameObject centerpiecePrefab;

        [Header("Бой")]
        [Tooltip("Набор волн этого владения. Тот же конструктор, что и был: " +
                 "владение не изобретает бой, а выбирает готовый.")]
        public LevelDefinition level;

        [Tooltip("Кто нападает. Для строки на карте и для подбора врагов.")]
        public EnemyFaction faction = EnemyFaction.Bandits;

        [Header("Награда")]
        [Tooltip("Что дают за помощь. Может быть несколько сразу: " +
                 "престиж плюс командир.")]
        public HoldingReward[] rewards = Array.Empty<HoldingReward>();

        /// <summary>Название для игрока: перевод по ключу, иначе текст из ассета.</summary>
        public string DisplayName => Loc.GetOrFallback(nameKey, displayName);

        /// <summary>Описание для карты.</summary>
        public string Description => Loc.GetOrFallback(descriptionKey, description);

        /// <summary>Сколько волн в этом владении. Ноль — уровень не назначен.</summary>
        public int WaveCount => level != null && level.waves != null ? level.waves.Length : 0;

        /// <summary>
        /// Даёт ли владение командира. Считается по наградам, а не по типу:
        /// тип — это ожидание игрока, а награда — факт, и расходиться
        /// они не должны молча.
        /// </summary>
        public bool GivesCommander
        {
            get
            {
                foreach (HoldingReward reward in rewards)
                {
                    if (reward != null && reward.kind == RewardKind.Commander)
                        return true;
                }

                return false;
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Проверки, которые дешевле поймать в инспекторе, чем в бою.
        /// </summary>
        private void OnValidate()
        {
            if (level == null)
                Debug.LogWarning($"[Владение] «{name}»: не назначен набор волн.", this);

            if (kind == HoldingKind.Barony && !GivesCommander)
                Debug.LogWarning($"[Владение] «{name}»: баронство без командира в награде. " +
                                 "Игрок идёт в тяжёлый бой именно за ним.", this);
        }
#endif
    }
}
