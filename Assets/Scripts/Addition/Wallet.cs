using System;
using UnityEngine;
using HeroDefense.Base;
using HeroDefense.Enemies;

namespace HeroDefense.Economy
{
    /// <summary>
    /// Золото игрока. Единственный ресурс в игре (D31).
    ///
    /// Два источника: убийства и пассивный доход ратуши.
    /// Позже добавятся экономические постройки.
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
        [Tooltip("Золото за одного убитого врага. Позже возьмётся из данных врага.")]
        [SerializeField] private int goldPerKill = 2;

        [Header("Ссылки")]
        [SerializeField] private EnemyManager enemyManager;
        [SerializeField] private TownHall townHall;

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
            _gold = startingGold;
        }

        private void Awake()
        {
            EnsureInitialized();
        }

        private void OnEnable()
        {
            if (enemyManager != null)
                enemyManager.EnemyKilled += OnEnemyKilled;

            if (townHall != null)
                townHall.IncomeGenerated += Add;
        }

        private void OnDisable()
        {
            if (enemyManager != null)
                enemyManager.EnemyKilled -= OnEnemyKilled;

            if (townHall != null)
                townHall.IncomeGenerated -= Add;
        }

        private void OnEnemyKilled(Enemy enemy)
        {
            Add(goldPerKill);
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
