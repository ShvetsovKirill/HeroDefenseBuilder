using UnityEngine;
using HeroDefense.Core;

namespace HeroDefense.Meta
{
    /// <summary>
    /// Домик поселения (D112) — вторая цель защиты.
    ///
    /// Не занимает слот застройки, не стреляет, не пополняется. Даёт немного
    /// золота по ходу и престиж в конце, если уцелел.
    ///
    /// Зачем вообще: сейчас игроку оптимально сжаться вокруг ратуши и отдать
    /// периметр — потери нет, всё ценное в центре. Домики дают повод держать
    /// внешнее кольцо и превращают «защищаешь абстрактную точку» в
    /// «защищаешь поселение».
    ///
    /// Отдельный компонент, а не Building: у построек есть слот, цена, лимит
    /// и реестр (D86) — домику всё это не нужно, он не строится игроком.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class NeutralHouse : MonoBehaviour
    {
        [Header("Доход")]
        [Tooltip("Сколько золота приносит за тик. Немного: домик — цель " +
                 "для защиты, а не замена экономическому зданию.")]
        [SerializeField] private int goldPerTick = 1;

        [Min(1f)]
        [Tooltip("Как часто капает золото, в секундах.")]
        [SerializeField] private float tickInterval = 10f;

        [Header("Разрушение")]
        [Tooltip("Что остаётся на месте разрушенного домика. Пусто — исчезает.")]
        [SerializeField] private GameObject ruinsPrefab;

        private Health _health;
        private float _timer;

        public bool IsAlive => _health != null && _health.Current > 0f;

        private void Awake()
        {
            _health = GetComponent<Health>();
        }

        private void OnEnable()
        {
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Died -= OnDied;
        }

        private void Update()
        {
            // Доход капает только в игре: на паузе и после поражения
            // золото копиться не должно.
            if (GameState.Current == null || !GameState.Current.IsPlaying)
                return;

            _timer += Time.deltaTime;

            if (_timer < tickInterval)
                return;

            _timer = 0f;

            SceneContext.Current?.Wallet?.Add(goldPerTick);
        }

        private void OnDied()
        {
            if (ruinsPrefab != null)
                Instantiate(ruinsPrefab, transform.position, transform.rotation, transform.parent);

            Destroy(gameObject);
        }
    }
}
