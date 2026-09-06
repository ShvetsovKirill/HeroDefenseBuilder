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

        /// <summary>Запись отряда из армии похода. Null вне кампании.</summary>
        private Campaign.SquadRecord _record;

        /// <summary>Командир ещё не вышел: следующий боец будет им.</summary>
        private bool _commanderPending;
        private Squad _squad;

        private float _buildProgress;
        private bool _batchPaid;

        /// <summary>За скольких бойцов уплачено в текущей партии.</summary>
        private int _paidUnits;

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
            // В походе казарма — место для готового отряда: выходит тот,
            // что уцелел в прошлом владении, со своим командиром. Вне
            // похода всё как было: безымянное ополчение по числам ассета.
            _record = Campaign.CampaignArmy.TakeNext();

            string squadName = _record != null
                ? Campaign.CampaignArmy.DescribeSquad(_record)
                : $"Squad_{definition?.displayName ?? name}";

            var squadObject = new GameObject(squadName);
            squadObject.transform.SetParent(transform, false);

            _squad = squadObject.AddComponent<Squad>();
            _squad.Wiped += OnSquadWiped;
            _squad.UnitLost += OnUnitLost;

            int size = _record != null
                ? _record.size
                : definition != null ? definition.squadSize : 6;

            // Командир идёт сверх штата: отряд из шести человек и командир —
            // это семеро, а не пятеро и командир.
            if (_record != null && _record.HasCommander)
            {
                size++;
                _commanderPending = true;
            }

            _squad.SetMaxUnits(size);
            _squad.ClearFlag(RallyPosition);

            // Командир приходит сразу и даром: он не рекрут, которого
            // набирают за деньги, а человек, который уже служит королю.
            // Ждать его по шкале пополнения было бы странно.
            if (_commanderPending)
                SpawnUnit();
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

            // Отряд выбит — значит и командир погиб вместе с ним,
            // второй раз он уже не выйдет.
            _commanderPending = false;

            if (_record == null)
                return;

            // Командир полёг вместе со всеми: отряд теряет имя
            // и специализацию, а его судьбу игрок решит в лагере.
            // Люди наберутся заново за золото, командир — нет.
            if (_record.HasCommander)
            {
                Debug.Log($"[Кампания] Отряд {_record.slot} выбит, командир погиб.");
                _record.LoseCommander();
            }

            // Метка значит «людей нет СЕЙЧАС», а не «когда-то выбили»:
            // пока казарма стоит, отряд наберётся заново за золото
            // и метка снимется. Снесли казарму — снимать её станет некому,
            // и отряд потерян насовсем (D46).
            //
            // Без этой записи армия в сохранении оставалась полной, отряды
            // выходили в следующем владении как ни в чём не бывало,
            // а поход нельзя было проиграть вообще.
            MarkArmyRecord(true);
        }

        /// <summary>
        /// Отметить в армии похода, есть ли у отряда люди. Пишем только
        /// на смене значения: сохранение уходит на диск, и дёргать его
        /// на каждого новобранца незачем.
        /// </summary>
        private void MarkArmyRecord(bool wipedOut)
        {
            if (_record == null || _record.wipedOut == wipedOut)
                return;

            _record.wipedOut = wipedOut;
            Campaign.CampaignRun.Save();
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

            // Запоминаем, за сколько человек заплачено. Пересчитать это число
            // при выходе нельзя: пока крутится шкала, отряд теряет ещё бойцов,
            // свободных мест становится больше — и партия вышла бы крупнее
            // оплаченной, то есть частично даром (D21a).
            _paidUnits = needed;

            return true;
        }

        private void ReleaseBatch()
        {
            // Не больше оплаченного и не больше, чем есть мест: погибшие
            // во время шкалы восполняются следующей партией, за деньги.
            int needed = Mathf.Min(_paidUnits, _squad.MaxUnits - _squad.AliveCount);

            for (int i = 0; i < needed; i++)
                SpawnUnit();

            // Звук только на непустую партию: шкала докручивается и при полном
            // отряде, и без проверки казарма щёлкала бы вхолостую.
            if (needed > 0)
                Audio.Sfx.PlayAt(Audio.SoundId.SquadReinforced, SpawnPosition);

            ResetProgress();
        }

        private void ResetProgress()
        {
            _buildProgress = 0f;
            _batchPaid = false;
            _paidUnits = 0;
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

            // Командир добавляет своё поверх общей прокачки: улучшения
            // из лагеря достаются всем, а он — только своему отряду.
            Campaign.CampaignArmy.ApplyCommander(unit, _record);

            // Первым из казармы выходит сам командир: он живучее своих
            // и виден по венцу. Дальше идут рядовые.
            //
            // Флаг сбрасывается навсегда: второй раз командир не выйдет
            // ни по шкале пополнения, ни после гибели отряда.
            if (_commanderPending)
            {
                _commanderPending = false;
                Campaign.CampaignArmy.MakeCommanderUnit(unit, _record);
            }

            _squad.AddUnit(unit);

            // Отряд снова с людьми: если он был выбит и отмечен потерянным,
            // метку снимаем — за него заплатили заново.
            MarkArmyRecord(false);
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
