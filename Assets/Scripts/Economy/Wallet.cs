using System;
using UnityEngine;
using HeroDefense.Enemies;

namespace HeroDefense.Economy
{
    /// <summary>
    /// Золото игрока. Единственный ресурс в игре (D31).
    ///
    /// Убийства начисляются по событию, пассивный доход зданий —
    /// прямым вызовом Add из GoldIncome: подписываться на каждое
    /// здание с доходом означало бы лишнюю связность.
    ///
    /// Золото начисляется сразу, без физических монеток — по D9 мелочь
    /// от толпы не должна порождать сотни пикапов. Физические монеты
    /// появятся только для элит и боссов.
    /// </summary>
    public sealed class Wallet : MonoBehaviour
    {
        [Header("Старт")]
        [SerializeField] private int startingGold = 100;

        [Header("Доход")]
        [Tooltip("Запасное значение: используется, только если у врага " +
                 "не задан EnemyDefinition. Обычная награда берётся из ассета — " +
                 "иначе за бугая давали бы столько же, сколько за роевого, " +
                 "и дилемма «убить самому ради золота» не работала бы.")]
        [SerializeField] private int fallbackGoldPerKill = 1;

        [Header("Ссылки")]
        [SerializeField] private EnemyManager enemyManager;

        private int _gold;
        private bool _initialized;

        /// <summary>
        /// Ленивая инициализация: Gold могут прочитать раньше, чем отработает
        /// Awake кошелька — Unity не гарантирует порядок между объектами сцены.
        /// Без этого подсветка слотов на старте видела ноль.
        /// </summary>
        public int Gold
        {
            get
            {
                EnsureInitialized();
                return _gold;
            }
            private set => _gold = value;
        }

        /// <summary>Изменение баланса. Для HUD и звука монет.</summary>
        public event Action<int> GoldChanged;

        private void EnsureInitialized()
        {
            if (_initialized)
                return;

            _initialized = true;

            // Бонус прибавляется здесь, а не вызовом Add: Add поднял бы
            // событие GoldChanged, а BattleStatsRecorder считает любой
            // положительный сдвиг заработком — стартовое золото попало бы
            // в «заработано» и испортило замер, который чинила партия 1.
            _gold = startingGold + HeroDefense.Meta.UpgradeApplier.StartingGoldBonus;
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            if (enemyManager != null)
                enemyManager.EnemyKilled += OnEnemyKilled;

        }

        private void OnDisable()
        {
            if (enemyManager != null)
                enemyManager.EnemyKilled -= OnEnemyKilled;

        }

        private void OnEnemyKilled(Enemy enemy)
        {
            int reward = enemy != null && enemy.Definition != null
                ? enemy.Definition.goldReward
                : fallbackGoldPerKill;

            Add(reward);
        }

        public void Add(int amount)
        {
            if (amount <= 0)
                return;

            Gold += amount;
            GoldChanged?.Invoke(Gold);
        }

        public bool CanAfford(int cost) => Gold >= cost;

        /// <summary>Попытка потратить. Возвращает false, если не хватает.</summary>
        public bool TrySpend(int cost)
        {
            if (!CanAfford(cost))
                return false;

            Gold -= cost;
            GoldChanged?.Invoke(Gold);

            return true;
        }

        /// <summary>Возврат при сносе постройки (D43).</summary>
        public void Refund(int amount)
        {
            Add(amount);
        }
    }
}
