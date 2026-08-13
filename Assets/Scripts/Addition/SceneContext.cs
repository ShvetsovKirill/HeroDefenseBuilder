using UnityEngine;
using HeroDefense.Base;
using HeroDefense.Economy;
using HeroDefense.Enemies;

namespace HeroDefense.Core
{
    /// <summary>
    /// Единая точка доступа к системам текущей карты.
    ///
    /// Зачем: FindFirstObjectByType был разбросан по семи классам.
    /// При переходе между картами (D27) он либо ничего не найдёт,
    /// либо найдёт объект со старой сцены — и всё тихо развалится.
    ///
    /// Здесь ссылки задаются явно в инспекторе и живут ровно столько,
    /// сколько живёт карта. Контекст обнуляет себя при выгрузке,
    /// поэтому «протечь» на следующую карту он не может.
    ///
    /// DefaultExecutionOrder -1000: контекст обязан быть готов
    /// раньше всех, кто им пользуется.
    /// </summary>
    [DefaultExecutionOrder(-1000)]
    public sealed class SceneContext : MonoBehaviour
    {
        [Header("Системы карты")]
        [SerializeField] private EnemyManager enemyManager;
        [SerializeField] private Wallet wallet;
        [SerializeField] private TownHall townHall;
        [SerializeField] private Transform king;

        private static SceneContext _current;

        /// <summary>
        /// Контекст активной карты. Null между картами — это нормально,
        /// потребители обязаны проверять.
        /// </summary>
        public static SceneContext Current => _current;

        public EnemyManager EnemyManager => enemyManager;
        public Wallet Wallet => wallet;
        public TownHall TownHall => townHall;
        public Transform King => king;

        private void Awake()
        {
            if (_current != null && _current != this)
            {
                Debug.LogError(
                    $"[SceneContext] На сцене уже есть контекст ({_current.name}). " +
                    "Их должно быть ровно по одному на карту.", this);

                return;
            }

            _current = this;
            ValidateReferences();
        }

        private void OnDestroy()
        {
            // Обнуляем только если это мы: при переходе между картами
            // новый контекст может успеть зарегистрироваться до нашего OnDestroy.
            if (_current == this)
                _current = null;
        }

        private void ValidateReferences()
        {
            WarnIfMissing(enemyManager, nameof(enemyManager));
            WarnIfMissing(wallet, nameof(wallet));
            WarnIfMissing(townHall, nameof(townHall));
            WarnIfMissing(king, nameof(king));
        }

        private void WarnIfMissing(Object reference, string fieldName)
        {
            if (reference == null)
                Debug.LogWarning($"[SceneContext] Не назначено поле {fieldName}.", this);
        }
    }
}
