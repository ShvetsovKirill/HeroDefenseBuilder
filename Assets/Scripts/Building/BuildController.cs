using System;
using UnityEngine;
using HeroDefense.Core;
using HeroDefense.Economy;
using HeroDefense.Enemies;

namespace HeroDefense.Building
{
    /// <summary>
    /// Строительство: король подъезжает к слоту — открывается панель выбора.
    ///
    /// Почему по подъезду, а не по клику: король — физический курсор игры,
    /// как и с флагами (D12). Нельзя застроить дальний угол, не съездив туда,
    /// и на тач не надо целиться пальцем в мелкий слот.
    ///
    /// Панель с карточками вместо клавиш: типов построек будет больше четырёх,
    /// а клавиатурные слоты кончаются и не переносятся на тач.
    /// </summary>
    public sealed class BuildController : MonoBehaviour
    {
        [Header("Каталог")]
        [Tooltip("Все постройки, доступные игроку. Порядок = порядок карточек.")]
        [SerializeField] private BuildingDefinition[] catalog = Array.Empty<BuildingDefinition>();

        [Header("Взаимодействие")]
        [Tooltip("На каком расстоянии от слота открывается панель. " +
                 "Король подъезжает к слоту, а не встаёт в него — " +
                 "поэтому здание не выталкивает его при постройке.")]
        [SerializeField] private float interactionRadius = 3f;

        private BuildSlot[] _slots;
        private BuildSlot _focusedSlot;

        /// <summary>Слот, рядом с которым сейчас король. null — панель закрыта.</summary>
        public BuildSlot FocusedSlot => _focusedSlot;

        public BuildingDefinition[] Catalog => catalog;

        /// <summary>Король подъехал к слоту или отъехал. null = закрыть панель.</summary>
        public event Action<BuildSlot> FocusChanged;

        /// <summary>Что-то построено. Слушает FlagController, чтобы добавить флаг казармы.</summary>
        public event Action<GameObject> BuildingPlaced;

        // Общие системы карты живут в SceneContext и только там.
        // Дублировать их полями в инспекторе — значит завести второй
        // источник правды: назначил одно, работает другое.
        private static Transform King => SceneContext.Current?.King;
        private static Wallet Purse => SceneContext.Current?.Wallet;
        private static EnemyManager Enemies => SceneContext.Current?.EnemyManager;

        private static BuildRegistry Registry => BuildRegistry.Current;

        private void Awake()
        {
            // Слоты ищем один раз: они статичны в пределах карты
            // и создаются вместе с ней.
            _slots = FindObjectsByType<BuildSlot>(FindObjectsSortMode.None);
        }

        private void Start()
        {
            // Предупреждаем о настройке, при которой лимиты не работают:
            // если разрешено построить больше, чем есть слотов,
            // выбор между типами исчезает (D86).
            Registry?.ValidateAgainstSlots(catalog, _slots.Length);
        }

        /// <summary>Сколько ещё таких построек можно поставить. Для карточки.</summary>
        public int GetRemaining(BuildingDefinition definition)
        {
            return Registry != null ? Registry.GetRemaining(definition) : int.MaxValue;
        }

        /// <summary>Действующий лимит с учётом чертежей. Для карточки.</summary>
        public int GetLimit(BuildingDefinition definition)
        {
            return Registry != null ? Registry.GetLimit(definition) : 0;
        }

        private void Update()
        {
            UpdateFocus();
            UpdateHighlights();
        }

        // ---------- Фокус ----------

        private void UpdateFocus()
        {
            BuildSlot nearest = IsGameRunning ? FindNearestFreeSlot() : null;

            if (nearest == _focusedSlot)
                return;

            _focusedSlot = nearest;
            FocusChanged?.Invoke(_focusedSlot);
        }

        private static bool IsGameRunning =>
            GameState.Current == null || GameState.Current.IsPlaying;

        private BuildSlot FindNearestFreeSlot()
        {
            Transform monarch = King;

            if (monarch == null)
                return null;

            float bestSqr = interactionRadius * interactionRadius;
            BuildSlot best = null;

            for (int i = 0; i < _slots.Length; i++)
            {
                BuildSlot slot = _slots[i];

                if (slot == null || slot.IsOccupied)
                    continue;

                Vector3 delta = slot.BuildPosition - monarch.position;
                delta.y = 0f;

                float distanceSqr = delta.sqrMagnitude;

                if (distanceSqr >= bestSqr)
                    continue;

                bestSqr = distanceSqr;
                best = slot;
            }

            return best;
        }

        // ---------- Подсветка ----------

        private void UpdateHighlights()
        {
            for (int i = 0; i < _slots.Length; i++)
            {
                if (_slots[i] != null)
                    _slots[i].SetHighlight(ResolveState(_slots[i]));
            }
        }

        private BuildSlot.HighlightState ResolveState(BuildSlot slot)
        {
            if (slot.IsOccupied)
                return BuildSlot.HighlightState.Occupied;

            if (slot != _focusedSlot)
                return BuildSlot.HighlightState.Free;

            return slot.IsContested(Enemies)
                ? BuildSlot.HighlightState.Blocked
                : BuildSlot.HighlightState.Focused;
        }

        // ---------- Постройка ----------

        /// <summary>
        /// Хватает ли золота. Отдельно от CanBuild: карточка показывает
        /// нехватку денег и запрет из-за боя по-разному.
        /// </summary>
        public bool CanAfford(BuildingDefinition definition)
        {
            Wallet purse = Purse;

            return definition != null && purse != null && purse.CanAfford(definition.cost);
        }

        /// <summary>Может ли игрок построить это прямо сейчас. Для состояния карточки.</summary>
        public bool CanBuild(BuildingDefinition definition, out string reason)
        {
            reason = string.Empty;

            if (_focusedSlot == null || definition == null)
            {
                reason = "Нет слота";
                return false;
            }

            if (_focusedSlot.IsContested(Enemies))
            {
                reason = "Идёт бой";
                return false;
            }

            if (!CanAfford(definition))
            {
                reason = "Не хватает золота";
                return false;
            }

            if (Registry != null && !Registry.CanBuildMore(definition))
            {
                reason = "Достигнут предел";
                return false;
            }

            return true;
        }

        /// <summary>Вызывается карточкой в панели.</summary>
        public bool TryBuild(BuildingDefinition definition)
        {
            // Отказ озвучиваем здесь, а не в карточке: причин отказа две,
            // а «ничего не произошло» после клика выглядит как поломка.
            if (!CanBuild(definition, out _))
            {
                Audio.Sfx.Play(Audio.SoundId.BuildRejected);
                return false;
            }

            if (!Purse.TrySpend(definition.cost))
            {
                Audio.Sfx.Play(Audio.SoundId.BuildRejected);
                return false;
            }

            GameObject placed = _focusedSlot.Place(definition);

            if (placed != null)
            {
                Registry?.RegisterBuilt(definition);
                BuildingPlaced?.Invoke(placed);
            }

            // Слот занят — фокус снимается, панель закроется.
            _focusedSlot = null;
            FocusChanged?.Invoke(null);

            return true;
        }
    }
}
