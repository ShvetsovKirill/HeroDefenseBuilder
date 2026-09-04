using System.Collections.Generic;
using UnityEngine;

namespace HeroDefense.Enemies
{
    /// <summary>
    /// Пул врагов, по отдельной очереди на каждый префаб.
    ///
    /// Зачем пул: Instantiate и Destroy на сотнях юнитов в секунду дают
    /// аллокации и работу сборщику мусора, а в WebGL сборка видна как рывки.
    ///
    /// Зачем очереди по типам: раньше пул создавал всех из одного префаба,
    /// и поле prefabOverride в EnemyDefinition не использовалось — все враги
    /// выглядели одинаково, различаясь только числами. Разные модели с разными
    /// мешами всё равно нельзя переиспользовать друг под друга.
    /// </summary>
    public sealed class EnemyPool : MonoBehaviour
    {
        [Header("Префаб по умолчанию")]
        [Tooltip("Используется, если у типа врага не задан свой префаб.")]
        [SerializeField] private Enemy defaultPrefab;

        [Header("Прогрев")]
        [Tooltip("Сколько врагов создать заранее на каждый тип. " +
                 "Рост пула в бою даёт рывок, поэтому лучше с запасом.")]
        [SerializeField] private int prewarmPerType = 60;

        [Tooltip("Типы, которые прогреть при старте. Обычно — все, что " +
                 "встречаются в уровне. Не заданные создадутся на лету.")]
        [SerializeField] private HeroDefense.Waves.EnemyDefinition[] prewarmTypes;

        private readonly Dictionary<Enemy, Stack<Enemy>> _pools = new();
        private readonly Dictionary<Enemy, Enemy> _origins = new();

        private Transform _root;

        private void Awake()
        {
            _root = transform;
            PrewarmAll();
        }

        private void PrewarmAll()
        {
            if (prewarmTypes == null)
                return;

            for (int i = 0; i < prewarmTypes.Length; i++)
            {
                Enemy prefab = ResolvePrefab(prewarmTypes[i]);

                if (prefab != null)
                    Prewarm(prefab);
            }
        }

        private void Prewarm(Enemy prefab)
        {
            Stack<Enemy> pool = GetPool(prefab);

            for (int i = 0; i < prewarmPerType; i++)
                pool.Push(CreateInstance(prefab));
        }

        // ---------- Аренда и возврат ----------

        /// <summary>
        /// Взять врага нужного типа. Если у типа нет своего префаба,
        /// используется общий.
        /// </summary>
        public Enemy Rent(HeroDefense.Waves.EnemyDefinition definition)
        {
            Enemy prefab = ResolvePrefab(definition);

            if (prefab == null)
            {
                Debug.LogError("[EnemyPool] Нет ни своего префаба у типа, ни общего.", this);
                return null;
            }

            Stack<Enemy> pool = GetPool(prefab);

            return pool.Count > 0 ? pool.Pop() : CreateInstance(prefab);
        }

        /// <summary>
        /// Вернуть врага. Кладём в очередь того префаба, из которого он сделан —
        /// иначе модели перемешались бы между типами.
        /// </summary>
        public void Return(Enemy enemy)
        {
            if (enemy == null)
                return;

            enemy.gameObject.SetActive(false);

            if (_origins.TryGetValue(enemy, out Enemy prefab))
                GetPool(prefab).Push(enemy);
            else
                Destroy(enemy.gameObject);
        }

        // ---------- Внутреннее ----------

        private Enemy ResolvePrefab(HeroDefense.Waves.EnemyDefinition definition)
        {
            if (definition == null)
                return defaultPrefab;

            if (definition.prefabOverride == null)
                return defaultPrefab;

            Enemy fromDefinition = definition.prefabOverride.GetComponent<Enemy>();

            if (fromDefinition == null)
            {
                Debug.LogError(
                    $"[EnemyPool] На префабе типа «{definition.displayName}» нет компонента Enemy.",
                    this);

                return defaultPrefab;
            }

            return fromDefinition;
        }

        private Stack<Enemy> GetPool(Enemy prefab)
        {
            if (_pools.TryGetValue(prefab, out Stack<Enemy> pool))
                return pool;

            pool = new Stack<Enemy>();
            _pools[prefab] = pool;

            return pool;
        }

        private Enemy CreateInstance(Enemy prefab)
        {
            Enemy enemy = Instantiate(prefab, _root);

            enemy.gameObject.SetActive(false);

            // Запоминаем происхождение: без этого при возврате непонятно,
            // в какую очередь класть.
            _origins[enemy] = prefab;

            return enemy;
        }

        /// <summary>Сколько экземпляров свободно всего. Для отладки.</summary>
        public int AvailableCount
        {
            get
            {
                int total = 0;

                foreach (Stack<Enemy> pool in _pools.Values)
                    total += pool.Count;

                return total;
            }
        }
    }
}
