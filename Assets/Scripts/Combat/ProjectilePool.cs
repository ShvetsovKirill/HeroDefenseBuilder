using System.Collections.Generic;
using UnityEngine;

namespace HeroDefense.Combat
{
    /// <summary>
    /// Пул снарядов, общий на сцену.
    ///
    /// Зачем: при десятке лучников и башен стрелы создаются по несколько раз
    /// в секунду. Instantiate/Destroy в таком темпе даёт мусор, а сборка
    /// мусора в WebGL видна как рывки.
    ///
    /// Пул по типам снарядов: стрела и магический шар — разные префабы,
    /// смешивать их в одной куче нельзя.
    /// </summary>
    [DefaultExecutionOrder(-600)]
    public sealed class ProjectilePool : MonoBehaviour
    {
        [Tooltip("Сколько снарядов каждого типа создать заранее.")]
        [SerializeField] private int prewarmPerType = 16;

        private readonly Dictionary<Projectile, Stack<Projectile>> _pools = new();
        private readonly Dictionary<Projectile, Projectile> _origins = new();

        private static ProjectilePool _current;

        public static ProjectilePool Current => _current;

        private void Awake()
        {
            if (_current != null && _current != this)
            {
                Debug.LogError("[ProjectilePool] На сцене уже есть пул снарядов.", this);
                return;
            }

            _current = this;
        }

        private void OnDestroy()
        {
            if (_current == this)
                _current = null;
        }

        /// <summary>
        /// Выпустить снаряд. Если пула под этот префаб ещё нет — создаётся
        /// на лету вместе с прогревом.
        /// </summary>
        public Projectile Launch(
            Projectile prefab,
            Vector3 position,
            HeroDefense.Enemies.Enemy target,
            float damage,
            HeroDefense.Core.Health source)
        {
            if (prefab == null || target == null)
                return null;

            Projectile projectile = Rent(prefab);

            projectile.transform.position = position;
            projectile.Launch(target, damage, source);

            return projectile;
        }

        /// <summary>Выпустить снаряд по защитнику: бойцу, постройке, ратуше.</summary>
        public Projectile LaunchAtHealth(
            Projectile prefab,
            Vector3 position,
            HeroDefense.Core.Health target,
            float damage,
            HeroDefense.Core.Health source)
        {
            if (prefab == null || target == null)
                return null;

            Projectile projectile = Rent(prefab);

            projectile.transform.position = position;
            projectile.LaunchAtHealth(target, damage, source);

            return projectile;
        }

        private Projectile Rent(Projectile prefab)
        {
            if (!_pools.TryGetValue(prefab, out Stack<Projectile> pool))
            {
                pool = new Stack<Projectile>();
                _pools[prefab] = pool;

                Prewarm(prefab, pool);
            }

            return pool.Count > 0 ? pool.Pop() : Create(prefab);
        }

        private void Prewarm(Projectile prefab, Stack<Projectile> pool)
        {
            for (int i = 0; i < prewarmPerType; i++)
                pool.Push(Create(prefab));
        }

        private Projectile Create(Projectile prefab)
        {
            Projectile instance = Instantiate(prefab, transform);

            instance.gameObject.SetActive(false);
            instance.Finished += Return;

            // Запоминаем, из какого префаба сделан экземпляр:
            // без этого при возврате непонятно, в какой пул его класть.
            _origins[instance] = prefab;

            return instance;
        }

        private void Return(Projectile projectile)
        {
            if (projectile == null || !_origins.TryGetValue(projectile, out Projectile prefab))
                return;

            if (_pools.TryGetValue(prefab, out Stack<Projectile> pool))
                pool.Push(projectile);
        }
    }
}
