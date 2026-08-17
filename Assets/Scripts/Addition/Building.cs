using System;
using UnityEngine;
using HeroDefense.Core;

namespace HeroDefense.Building
{
    /// <summary>
    /// Постройка на слоте (D40–D44).
    ///
    /// Знает, из какого ассета она сделана и на каком слоте стоит —
    /// без этого при разрушении не освободить лимит и не показать руины.
    ///
    /// Ратуша тоже постройка, но с флагом «критическая»: разрушение
    /// заканчивает игру. Всё остальное просто ломается.
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class Building : MonoBehaviour
    {
        [Header("Руины")]
        [Tooltip("Что остаётся на месте разрушенной постройки (D42). " +
                 "Пусто — постройка просто исчезает.")]
        [SerializeField] private GameObject ruinsPrefab;

        [Tooltip("Доля цены, за которую постройка восстанавливается из руин. " +
                 "Мягче по темпу, чем полная потеря вложения.")]
        [Range(0f, 1f)]
        [SerializeField] private float rebuildCostFraction = 0.5f;

        private Health _health;
        private BuildingDefinition _definition;
        private BuildSlot _slot;

        public Health Health => _health;
        public BuildingDefinition Definition => _definition;

        /// <summary>Цена восстановления из руин.</summary>
        public int RebuildCost => _definition != null
            ? Mathf.CeilToInt(_definition.cost * rebuildCostFraction)
            : 0;

        /// <summary>Постройка разрушена. Слот и реестр реагируют.</summary>
        public event Action<Building> Destroyed;

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

        /// <summary>
        /// Вызывается слотом сразу после создания: постройка узнаёт,
        /// чем она является и где стоит.
        /// </summary>
        public void Initialize(BuildingDefinition definition, BuildSlot slot)
        {
            _definition = definition;
            _slot = slot;
        }

        private void OnDied()
        {
            SpawnRuins();

            // Освобождаем лимит: место под этот тип снова доступно (D86).
            BuildRegistry.Current?.RegisterRemoved(_definition);

            Destroyed?.Invoke(this);

            HeroDefense.Diagnostics.BattleStats.RegisterBuildingLost();

            if (_slot != null)
                _slot.OnBuildingDestroyed();

            Destroy(gameObject);
        }

        private void SpawnRuins()
        {
            if (ruinsPrefab == null)
                return;

            Instantiate(ruinsPrefab, transform.position, transform.rotation, transform.parent);
        }
    }
}
