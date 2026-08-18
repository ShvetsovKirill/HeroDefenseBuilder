using UnityEngine;
using HeroDefense.Core;
using HeroDefense.Economy;

namespace HeroDefense.Squads
{
    /// <summary>
    /// Постройка, производящая отряд.
    ///
    /// Пополнение автоматическое (D21a): погиб боец, есть золото — списывается,
    /// идёт шкала, новобранцы выходят и топают к флагу. Нет золота — шкала ждёт.
    /// Герой в этом не участвует (D21c).
    ///
    /// Разрушена казарма — отряд живёт, но больше не восстанавливается (D46).
    /// Погиб полностью без казармы — потерян навсегда.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class Barracks : MonoBehaviour
    {
        [Header("Данные")]
        [SerializeField] private BarracksDefinition definition;

        [Header("Ссылки")]
        [Tooltip("Точка выхода новобранцев. Пусто — позиция постройки.")]
        [SerializeField] private Transform spawnPoint;

        [Tooltip("Куда идёт отряд, пока флаг не поставлен.")]
        [SerializeField] private Transform rallyPoint;

        private Health _health;
        private Squad _squad;

        private float _buildProgress;
        private bool _batchPaid;

        /// <summary>Отряд этой постройки. Через него ставится флаг.</summary>
        public Squad Squad => _squad;

        public BarracksDefinition Definition => definition;

        /// <summary>Прогресс шкалы пополнения, 0..1. Для индикатора над постройкой.</summary>
        public float BuildProgress => _buildProgress;

        public bool IsBuilding => _batchPaid && _squad != null && !_squad.IsFull;

        /// <summary>Не хватает золота. Для подсветки постройки.</summary>
        public bool IsWaitingForGold { get; private set; }

        /// <summary>Кошелёк берётся из контекста карты — своего поля нет намеренно.</summary>
        private static Wallet Wallet => SceneContext.Current?.Wallet;

        private void Awake()
        {
            _health = GetComponent<Health>();
            CreateSquad();

            // Порядок регистрации задаёт номер клавиши флага (D51):
            // первая построенная казарма — всегда «1».
            SquadRegistry.Register(this);
        }

        private void OnEnable()
        {
            _health.Died += OnBuildingDestroyed;
            _health.Damaged += OnBuildingDamaged;
        }

        private void OnDisable()
        {
            _health.Died -= OnBuildingDestroyed;
            _health.Damaged -= OnBuildingDamaged;
        }

        /// <summary>
        /// Казарму бьют — поднимаем тревогу своему отряду.
        /// Иначе бойцы стоят рядом и смотрят, как ломают их дом:
        /// враг может бить постройку, оставаясь вне радиуса самозащиты бойца.
        /// </summary>
        private void OnBuildingDamaged(float amount)
        {
            if (_squad != null)
                _squad.RaiseAlert();
        }

        private void OnDestroy()
        {
            SquadRegistry.Unregister(this);

            // Отряд создан кодом и живёт как дочерний объект —
            // при уничтожении постройки он умрёт вместе с ней,
            // но подписки надо снять явно.
            if (_squad != null)
            {
                _squad.Wiped -= OnSquadWiped;
                _squad.UnitLost -= OnUnitLost;
            }
        }

        private void CreateSquad()
        {
            var squadObject = new GameObject($"Squad_{definition?.displayName ?? name}");
            squadObject.transform.SetParent(transform, false);

            _squad = squadObject.AddComponent<Squad>();
            _squad.Wiped += OnSquadWiped;
            _squad.UnitLost += OnUnitLost;

            if (definition != null)
                _squad.SetMaxUnits(definition.squadSize);

            _squad.ClearFlag(RallyPosition);
        }

        private void OnUnitLost(Squad squad)
        {
            // HUD пересчитает армию по событию, а не сканированием сцены.
            SquadRegistry.NotifyChanged();
        }

        private void OnSquadWiped(Squad squad)
        {
            // Отряд выбит полностью — сборка начнётся с нуля,
            // флаг игрок поставит заново (D21b).
            ResetProgress();
        }

        private void Update()
        {
            if (!_health.IsAlive || definition == null || !IsGameRunning)
                return;

            TickReinforcement(Time.deltaTime);
        }

        private static bool IsGameRunning =>
            GameState.Current == null || GameState.Current.IsPlaying;

        // ---------- Пополнение ----------

        private void TickReinforcement(float deltaTime)
        {
            if (_squad.IsFull)
            {
                ResetProgress();
                return;
            }

            if (!_batchPaid && !TryPayForBatch())
                return;

            _buildProgress += deltaTime / definition.unitBuildTime;

            if (_buildProgress < 1f)
                return;

            ReleaseBatch();
        }

        /// <summary>
        /// Оплата вперёд, а не по факту выхода: иначе игрок мог бы потратить
        /// золото на башню, пока шкала почти заполнена, и получить бойцов даром.
        /// </summary>
        private bool TryPayForBatch()
        {
            Wallet purse = Wallet;

            if (purse == null)
                return false;

            int needed = Mathf.Min(definition.unitsPerBatch, _squad.MaxUnits - _squad.AliveCount);
            int cost = definition.unitCost * needed;

            if (!purse.TrySpend(cost))
            {
                IsWaitingForGold = true;
                return false;
            }

            IsWaitingForGold = false;
            _batchPaid = true;

            return true;
        }

        private void ReleaseBatch()
        {
            int needed = Mathf.Min(definition.unitsPerBatch, _squad.MaxUnits - _squad.AliveCount);

            for (int i = 0; i < needed; i++)
                SpawnUnit();

            ResetProgress();
        }

        private void ResetProgress()
        {
            _buildProgress = 0f;
            _batchPaid = false;
        }

        private void SpawnUnit()
        {
            if (definition.unitPrefab == null)
            {
                Debug.LogError($"[Barracks] У {name} не задан префаб бойца.", this);
                enabled = false;
                return;
            }

            GameObject instance = Instantiate(definition.unitPrefab, SpawnPosition, transform.rotation);
            var unit = instance.GetComponent<SquadUnit>();

            if (unit == null)
            {
                Debug.LogError("[Barracks] На префабе бойца нет SquadUnit.", this);
                Destroy(instance);
                return;
            }

            // Прокачка выдаётся здесь, а не в префабе: боец рождается
            // в середине забега, когда раздавать бонусы уже некому.
            HeroDefense.Meta.UpgradeApplier.ApplyToUnit(unit);

            _squad.AddUnit(unit);
        }

        // ---------- Разрушение ----------

        /// <summary>
        /// Казарму снесли. Отряд остаётся жить, но больше не пополняется (D46).
        /// Это делает казарму осмысленной целью для врагов и создаёт драму:
        /// «казарму снесли — теперь мой отряд смертен».
        /// </summary>
        private void OnBuildingDestroyed()
        {
            enabled = false;
            ResetProgress();

            IsProducing = false;

            Debug.Log($"[Barracks] {name} разрушена. Отряд больше не пополняется.");
        }

        /// <summary>
        /// Может ли постройка ещё производить бойцов.
        /// Ложь после разрушения — отряд становится смертным (D46).
        /// </summary>
        public bool IsProducing { get; private set; } = true;

        private Vector3 SpawnPosition => spawnPoint != null ? spawnPoint.position : transform.position;
        private Vector3 RallyPosition => rallyPoint != null ? rallyPoint.position : SpawnPosition;
    }
}
