using System;
using System.Collections.Generic;
using UnityEngine;
using HeroDefense.Meta;

namespace HeroDefense.Building
{
    /// <summary>
    /// Учёт построек: сколько чего построено и сколько ещё можно (D86).
    ///
    /// Отдельно от BuildController, потому что тот и так делает три вещи —
    /// фокус, подсветку и постройку. А учёт нужен ещё и панели (чтобы
    /// показать «3 / 4»), и мете (чтобы поднять лимит между забегами).
    ///
    /// Живёт на время забега: лимиты от чертежей (D88) сбрасываются
    /// при старте нового.
    /// </summary>
    [DefaultExecutionOrder(-700)]
    public sealed class BuildRegistry : MonoBehaviour
    {
        private readonly Dictionary<BuildingDefinition, int> _built = new();
        private readonly Dictionary<BuildingDefinition, int> _bonusLimits = new();

        private static BuildRegistry _current;

        public static BuildRegistry Current => _current;

        /// <summary>Что-то построено или снесено — панели обновляют счётчики.</summary>
        public event Action Changed;

        private void Awake()
        {
            if (_current != null && _current != this)
            {
                Debug.LogError("[BuildRegistry] На сцене уже есть реестр построек.", this);
                return;
            }

            _current = this;
        }

        private void OnDestroy()
        {
            if (_current == this)
                _current = null;
        }

        // ---------- Чтение ----------

        /// <summary>Сколько таких построек уже стоит.</summary>
        public int GetBuilt(BuildingDefinition definition)
        {
            if (definition == null)
                return 0;

            return _built.TryGetValue(definition, out int count) ? count : 0;
        }

        /// <summary>
        /// Действующий лимит: сколько карточек этого типа взято в забег
        /// плюс чертежи, выпавшие по ходу (D88).
        ///
        /// Колода заменила собой лимиты из ассетов (D122): раньше потолок
        /// задавался maxCount, теперь — тем, что игрок решил взять с собой.
        /// Так выбор делается до забега и стоит престижа.
        ///
        /// Если колода не собрана, значит сцена Battle запущена напрямую
        /// из редактора — тогда работаем по старому, от maxCount. Иначе
        /// тестировать бой можно было бы только через замок.
        /// </summary>
        public int GetLimit(BuildingDefinition definition)
        {
            if (definition == null)
                return 0;

            int bonus = _bonusLimits.TryGetValue(definition, out int b) ? b : 0;

            int baseLimit = RunLoadout.IsConfigured
                ? RunLoadout.GetCount(definition)
                : definition.maxCount;

            return baseLimit + bonus;
        }

        public int GetRemaining(BuildingDefinition definition)
        {
            return Mathf.Max(0, GetLimit(definition) - GetBuilt(definition));
        }

        public bool CanBuildMore(BuildingDefinition definition)
        {
            return GetRemaining(definition) > 0;
        }

        // ---------- Изменение ----------

        public void RegisterBuilt(BuildingDefinition definition)
        {
            if (definition == null)
                return;

            _built.TryGetValue(definition, out int count);
            _built[definition] = count + 1;

            Changed?.Invoke();
        }

        /// <summary>
        /// Постройка снесена или разрушена — место освободилось, карточка
        /// вернулась в колоду (D123).
        ///
        /// Возвращать право обязательно: за прошлый прогон погибло пять
        /// построек из девяти, и без возврата колода пустела бы к третьему
        /// акту ровно тогда, когда угроза максимальна — спираль поражения,
        /// против которой существует D44. Золото при этом не возвращается:
        /// восстановление остаётся платным (D42).
        /// </summary>
        public void RegisterRemoved(BuildingDefinition definition)
        {
            if (definition == null || !_built.TryGetValue(definition, out int count))
                return;

            _built[definition] = Mathf.Max(0, count - 1);

            Changed?.Invoke();
        }

        /// <summary>
        /// Чертёж из сундука: +1 к лимиту на этот забег (D88).
        /// Не навсегда — это не «больше места», а «повезло с редкой штукой».
        /// </summary>
        public bool ApplyBlueprint(BuildingDefinition definition)
        {
            if (definition == null || !definition.allowBlueprints)
                return false;

            _bonusLimits.TryGetValue(definition, out int bonus);
            _bonusLimits[definition] = bonus + 1;

            Changed?.Invoke();

            return true;
        }

        /// <summary>Сброс при старте нового забега.</summary>
        public void ResetForNewRun()
        {
            _built.Clear();
            _bonusLimits.Clear();

            Changed?.Invoke();
        }

        /// <summary>
        /// Проверка настройки: доступного должно быть больше, чем слотов.
        /// Иначе игрок строит всё разрешённое, и выбора между типами нет.
        /// </summary>
        public void ValidateAgainstSlots(BuildingDefinition[] catalog, int slotCount)
        {
            if (catalog == null)
                return;

            int total = 0;

            if (RunLoadout.IsConfigured)
            {
                total = RunLoadout.TotalCards;
            }
            else
            {
                foreach (BuildingDefinition definition in catalog)
                {
                    if (definition != null)
                        total += definition.maxCount;
                }
            }

            if (total > slotCount)
                return;

            Debug.LogWarning(
                $"[BuildRegistry] Доступно построек ({total}) не больше числа слотов ({slotCount}). " +
                "Игрок сможет построить всё разрешённое — выбор между типами исчезает. " +
                "Увеличь размер колоды (D124) или лимиты в ассетах (D86).", this);
        }
    }
}
