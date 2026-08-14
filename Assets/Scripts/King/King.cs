using System;
using UnityEngine;

namespace HeroDefense.King
{
    /// <summary>
    /// Владелец характеристик короля. До сих пор король был размазан
    /// между HeroMotor, AutoAttacker и FlagController — каждый знал
    /// про него кусочек, а целого владельца не было.
    ///
    /// Теперь так: этот компонент держит KingStats, остальные читают
    /// оттуда. Прокачка меняет статы — компоненты подхватывают
    /// по событию, ничего не зная про то, откуда взялся бонус.
    /// </summary>
    [DefaultExecutionOrder(-500)]
    public sealed class King : MonoBehaviour
    {
        [Header("Кто это")]
        [SerializeField] private KingDefinition definition;

        [Header("Визуал")]
        [Tooltip("Корень сменной части: модель, анимации, материалы (D63). " +
                 "Логика сюда не заглядывает — объект меняется целиком.")]
        [SerializeField] private Transform visualRoot;

        private KingStats _stats;

        /// <summary>Действующие характеристики. Null до Awake.</summary>
        public KingStats Stats => _stats;

        public KingDefinition Definition => definition;

        /// <summary>Статы пересчитаны. Компоненты обновляют свой кеш.</summary>
        public event Action StatsChanged;

        private void Awake()
        {
            if (definition == null)
            {
                Debug.LogError("[King] Не назначен KingDefinition.", this);
                enabled = false;
                return;
            }

            _stats = new KingStats(definition);
            _stats.Changed += OnStatsChanged;

            SpawnVisual();
        }

        private void OnDestroy()
        {
            if (_stats != null)
                _stats.Changed -= OnStatsChanged;
        }

        private void OnStatsChanged()
        {
            StatsChanged?.Invoke();
        }

        /// <summary>
        /// Ставим визуал из дефиниции, если он задан.
        /// Так смена короля меняет и внешность, и числа одним ассетом.
        /// </summary>
        private void SpawnVisual()
        {
            if (definition.visualPrefab == null || visualRoot == null)
                return;

            // Чистим то, что могло остаться от превью в редакторе.
            for (int i = visualRoot.childCount - 1; i >= 0; i--)
                Destroy(visualRoot.GetChild(i).gameObject);

            Instantiate(definition.visualPrefab, visualRoot);
        }

        /// <summary>Сменить короля в рантайме — для выбора перед забегом.</summary>
        public void SetDefinition(KingDefinition newDefinition)
        {
            if (newDefinition == null)
                return;

            definition = newDefinition;

            if (_stats != null)
                _stats.Changed -= OnStatsChanged;

            _stats = new KingStats(definition);
            _stats.Changed += OnStatsChanged;

            SpawnVisual();
            StatsChanged?.Invoke();
        }
    }
}
