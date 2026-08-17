using System;
using UnityEngine;
using HeroDefense.Core;
using HeroDefense.Economy;

namespace HeroDefense.Building
{
    /// <summary>
    /// Пассивный доход постройки.
    ///
    /// Один компонент на всё, что приносит золото: ратуша и экономические
    /// здания. Раньше эта логика жила внутри TownHall — при добавлении
    /// второго источника её пришлось бы копировать.
    ///
    /// Здание с доходом уязвимо, и в этом весь смысл (D4): слот, отданный
    /// экономике, не защищает, а само здание враги могут снести — тогда
    /// инвестиция не окупится. Именно это делает застройку решением,
    /// а не накоплением.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class GoldIncome : MonoBehaviour
    {
        [Header("Доход")]
        [Tooltip("Сколько золота приносит за один тик.")]
        [Min(1)]
        [SerializeField] private int goldPerTick = 10;

        [Tooltip("Интервал между начислениями, секунды.")]
        [Min(1f)]
        [SerializeField] private float interval = 20f;

        [Header("Запуск")]
        [Tooltip("Задержка перед первым начислением.\n\n" +
                 "Ставится намеренно: без неё здание окупалось бы " +
                 "с первой же секунды, и решение «вложиться сейчас ради " +
                 "выгоды потом» превращалось бы в «вложиться и сразу получить».")]
        [SerializeField] private float startupDelay = 5f;

        [Header("Работа под огнём")]
        [Tooltip("Останавливать ли доход, когда здание повреждено ниже порога.\n\n" +
                 "Даёт ещё одну причину защищать экономику, а не только " +
                 "отстраивать её заново.")]
        [SerializeField] private bool stopWhenDamaged;

        [Range(0f, 1f)]
        [SerializeField] private float damagedThreshold = 0.35f;

        private Health _health;
        private float _timer;

        /// <summary>Начислено золото. Аргумент — сумма. Для всплывающей цифры над зданием.</summary>
        public event Action<int> IncomeGenerated;

        /// <summary>Сколько золота приносит за минуту. Для подсказки на карточке.</summary>
        public float GoldPerMinute => interval > 0f ? goldPerTick * (60f / interval) : 0f;

        /// <summary>Прогресс до следующего начисления, 0..1. Для индикатора.</summary>
        public float Progress => interval > 0f ? Mathf.Clamp01(_timer / interval) : 0f;

        /// <summary>Работает ли сейчас. Ложь, если здание слишком повреждено.</summary>
        public bool IsProducing => _health.IsAlive && !IsTooDamaged;

        private bool IsTooDamaged => stopWhenDamaged && _health.Fraction < damagedThreshold;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _timer = -startupDelay;
        }

        private void Update()
        {
            if (!IsGameRunning || !IsProducing)
                return;

            TickIncome(Time.deltaTime);
        }

        private static bool IsGameRunning =>
            GameState.Current == null || GameState.Current.IsPlaying;

        private void TickIncome(float deltaTime)
        {
            _timer += deltaTime;

            if (_timer < interval)
                return;

            _timer = 0f;

            Payout();
        }

        /// <summary>
        /// Начисляем напрямую в кошелёк из контекста, а не через подписку:
        /// зданий с доходом может быть много, и заставлять кошелёк
        /// подписываться на каждое — лишняя связность.
        /// </summary>
        private void Payout()
        {
            Wallet wallet = SceneContext.Current?.Wallet;

            if (wallet == null)
                return;

            wallet.Add(goldPerTick);
            IncomeGenerated?.Invoke(goldPerTick);
        }

        /// <summary>Настроить извне — понадобится для апгрейдов здания.</summary>
        public void Configure(int newGoldPerTick, float newInterval)
        {
            goldPerTick = Mathf.Max(1, newGoldPerTick);
            interval = Mathf.Max(1f, newInterval);
        }
    }
}
