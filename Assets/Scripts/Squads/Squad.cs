using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroDefense.Squads
{
    /// <summary>
    /// Отряд: горстка бойцов, привязанных к флагу (D11).
    ///
    /// Флаг — единственный канал управления (D12). Переставили —
    /// отряд немедленно бросает всё и марширует к новой точке (D14).
    ///
    /// Отряд не командует боем: каждый боец сам решает, драться ему
    /// или возвращаться. Отряд только раздаёт позиции.
    /// </summary>
    public sealed class Squad : MonoBehaviour
    {
        [Header("Состав")]
        [Tooltip("Сколько бойцов в полном отряде. Растёт с апгрейдом постройки.")]
        [SerializeField] private int maxUnits = 6;

        [Header("Построение")]
        [Tooltip("Радиус кольца, в котором бойцы стоят вокруг флага.")]
        [SerializeField] private float formationRadius = 1.5f;

        [Header("Командир")]
        [Tooltip("Зарезервировано под трейты командира (D19). Пока не используется.")]
        [SerializeField] private string commanderId = string.Empty;

        private readonly List<SquadUnit> _units = new();

        private Vector3 _flagPosition;
        private bool _hasFlag;
        private bool _isDisbanding;

        public int MaxUnits => maxUnits;
        public int AliveCount => _units.Count;
        public bool IsFull => _units.Count >= maxUnits;
        public bool IsEmpty => _units.Count == 0;
        public bool HasFlag => _hasFlag;
        public Vector3 FlagPosition => _flagPosition;

        /// <summary>Отряд уничтожен полностью. Постройка начнёт собирать заново (D21b).</summary>
        public event Action<Squad> Wiped;

        /// <summary>Боец погиб — постройка запустит шкалу пополнения (D21a).</summary>
        public event Action<Squad> UnitLost;

        private void OnDestroy()
        {
            // Отписываемся от всех, кто ещё жив: иначе при выгрузке карты
            // события полетят в уничтоженный отряд.
            UnsubscribeAll();
        }

        // ---------- Флаг ----------

        /// <summary>
        /// Поставить или переставить флаг. Бойцы получают новые позиции
        /// сразу — марш начинается в этом же кадре.
        /// </summary>
        public void SetFlag(Vector3 position)
        {
            _flagPosition = position;
            _hasFlag = true;

            AssignFormationPositions();
        }

        /// <summary>Убрать флаг. Отряд пойдёт к точке сбора у своей постройки.</summary>
        public void ClearFlag(Vector3 fallbackPosition)
        {
            _flagPosition = fallbackPosition;
            _hasFlag = false;

            AssignFormationPositions();
        }

        // ---------- Состав ----------

        public void AddUnit(SquadUnit unit)
        {
            if (unit == null || _units.Contains(unit))
                return;

            _units.Add(unit);
            unit.Died += OnUnitDied;

            AssignFormationPositions();

            // Прибыл новобранец — HUD пересчитает армию по событию.
            SquadRegistry.NotifyChanged();
        }

        private void OnUnitDied(SquadUnit unit)
        {
            // Во время роспуска события смерти игнорируем:
            // отряд уже расформирован, пополнять нечего.
            if (_isDisbanding)
                return;

            unit.Died -= OnUnitDied;
            _units.Remove(unit);

            UnitLost?.Invoke(this);

            if (_units.Count == 0)
                Wiped?.Invoke(this);
            else
                AssignFormationPositions();
        }

        /// <summary>
        /// Убрать всех бойцов.
        ///
        /// Флаг _isDisbanding нужен потому, что Destroy может вызвать
        /// цепочку событий в этом же кадре — и OnUnitDied попытался бы
        /// изменить список, по которому мы идём.
        /// </summary>
        public void DisbandAll()
        {
            _isDisbanding = true;

            UnsubscribeAll();

            for (int i = _units.Count - 1; i >= 0; i--)
            {
                if (_units[i] != null)
                    Destroy(_units[i].gameObject);
            }

            _units.Clear();
            _isDisbanding = false;
        }

        private void UnsubscribeAll()
        {
            for (int i = 0; i < _units.Count; i++)
            {
                if (_units[i] != null)
                    _units[i].Died -= OnUnitDied;
            }
        }

        // ---------- Построение ----------

        /// <summary>
        /// Расставляем бойцов по кольцу вокруг флага.
        ///
        /// Если дать всем одну точку, они будут толкаться в ней.
        /// Кольцо решает это без физики и даёт узнаваемый силуэт отряда.
        /// </summary>
        private void AssignFormationPositions()
        {
            int count = _units.Count;

            if (count == 0)
                return;

            if (count == 1)
            {
                _units[0].SetAnchor(_flagPosition);
                return;
            }

            for (int i = 0; i < count; i++)
            {
                if (_units[i] == null)
                    continue;

                float angle = i / (float)count * Mathf.PI * 2f;

                Vector3 offset = new Vector3(
                    Mathf.Cos(angle) * formationRadius,
                    0f,
                    Mathf.Sin(angle) * formationRadius);

                _units[i].SetAnchor(_flagPosition + offset);
            }
        }

        /// <summary>Увеличить лимит — апгрейд постройки.</summary>
        public void SetMaxUnits(int value)
        {
            maxUnits = Mathf.Max(1, value);
        }
    }
}
