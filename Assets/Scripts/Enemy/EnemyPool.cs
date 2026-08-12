using System.Collections.Generic;
using UnityEngine;

namespace HeroDefense.Enemies
{
    /// <summary>
    /// Пул врагов.
    ///
    /// Зачем: Instantiate и Destroy на сотнях юнитов в секунду дают
    /// аллокации и работу сборщику мусора, а в WebGL сборка мусора
    /// особенно болезненна — она видна как рывки.
    /// Поэтому объекты создаются один раз и переиспользуются.
    /// </summary>
    public sealed class EnemyPool : MonoBehaviour
    {
        [SerializeField] private Enemy enemyPrefab;

        [Tooltip("Сколько врагов создать заранее, при старте. " +
                 "Лучше сразу с запасом — рост пула в бою даёт рывок.")]
        [SerializeField] private int prewarmCount = 300;

        private readonly Stack<Enemy> _available = new();
        private Transform _root;

        private void Awake()
        {
            _root = transform;
            Prewarm();
        }

        private void Prewarm()
        {
            if (enemyPrefab == null)
            {
                Debug.LogError("[EnemyPool] Не назначен enemyPrefab.", this);
                enabled = false;
                return;
            }

            for (int i = 0; i < prewarmCount; i++)
                _available.Push(CreateInstance());
        }

        private Enemy CreateInstance()
        {
            Enemy enemy = Instantiate(enemyPrefab, _root);
            enemy.gameObject.SetActive(false);

            return enemy;
        }

        /// <summary>Взять врага из пула. Если свободных нет — пул вырастет.</summary>
        public Enemy Rent()
        {
            return _available.Count > 0 ? _available.Pop() : CreateInstance();
        }

        /// <summary>Вернуть врага в пул. Объект деактивируется, но не уничтожается.</summary>
        public void Return(Enemy enemy)
        {
            if (enemy == null)
                return;

            enemy.gameObject.SetActive(false);
            _available.Push(enemy);
        }

        /// <summary>Сколько объектов сейчас свободно. Для отладки.</summary>
        public int AvailableCount => _available.Count;
    }
}
