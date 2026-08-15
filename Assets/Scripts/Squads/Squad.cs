using System;
using System.Collections.Generic;
using UnityEngine;
using HeroDefense.Enemies;

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

        [Header("Тревога")]
        [Tooltip("Сколько врагов рядом, чтобы отряд снялся с места ВСЕМ составом.\n\n" +
                 "Одиночку добивает тот, кто ближе — иначе шестеро срывались бы " +
                 "на каждого пробегающего, оголяя позицию по десять раз за волну.")]
        [Min(1)]
        [SerializeField] private int alertThreshold = 2;

        [Tooltip("Радиус, в котором отряд оценивает угрозу. Считается от флага.")]
        [SerializeField] private float alertRadius = 6f;

        [Tooltip("Задержка перед выходом: первый заметивший как бы подаёт сигнал, " +
                 "остальные подтягиваются. Без неё отряд трогается синхронно, " +
                 "как один организм, и это выглядит механически.")]
        [SerializeField] private float alertDelay = 0.4f;

        [Tooltip("Сколько секунд тревога держится после того, как враги кончились. " +
                 "Без выдержки отряд дёргался бы туда-обратно на границе радиуса.")]
        [SerializeField] private float alertHoldTime = 2f;

        [Tooltip("Как часто отряд оценивает обстановку.")]
        [SerializeField] private float scanInterval = 0.3f;

        [Tooltip("Насколько далеко строй сдвигается к угрозе при тревоге.\n\n" +
                 "Каждый боец привязан к своей точке в кольце, поэтому враг " +
                 "с одной стороны оказывался в пределах поводка только у части " +
                 "отряда — остальные упирались в свой поводок и стояли. " +
                 "Сдвиг всего строя решает это, не растягивая отряд.")]
        [SerializeField] private float rallyShift = 2.5f;

        [Header("Командир")]
        [Tooltip("Зарезервировано под трейты командира (D19). Пока не используется.")]
        [SerializeField] private string commanderId = string.Empty;

        private readonly List<SquadUnit> _units = new();

        private Vector3 _flagPosition;
        private bool _hasFlag;
        private bool _isDisbanding;

        private float _scanTimer;
        private float _alertTimer;
        private float _alertStartedAt = -1f;

        private Vector3 _threatDirection;
        private bool _formationShifted;

        public int MaxUnits => maxUnits;
        public int AliveCount => _units.Count;
        public bool IsFull => _units.Count >= maxUnits;
        public bool IsEmpty => _units.Count == 0;
        public bool HasFlag => _hasFlag;
        public Vector3 FlagPosition => _flagPosition;

        /// <summary>
        /// Отряд поднят по тревоге — бойцам разрешено выходить на врага.
        ///
        /// Пока тревоги нет, боец дерётся, только если враг уже вплотную:
        /// так отряд держит строй, а не растаскивается по одному.
        /// </summary>
        public bool IsAlerted => _alertTimer > 0f && HasAlertDelayPassed;

        /// <summary>
        /// Поднять тревогу извне. Вызывается, когда бойца атакуют:
        /// ждать, пока рядом наберётся достаточно врагов, — значит стоять
        /// и умирать, пока порог не набрался.
        ///
        /// Здесь задержки нет: по нам уже бьют, подтягиваться поздно.
        /// </summary>
        public void RaiseAlert()
        {
            _alertTimer = alertHoldTime;

            // Сдвигаем момент сигнала в прошлое, чтобы задержка
            // считалась пройденной и отряд среагировал в этом же кадре.
            _alertStartedAt = Time.time - alertDelay;

            // Направление угрозы могло устареть с прошлого скана:
            // нас бьют прямо сейчас, и разворачиваться надо туда.
            EnemyManager manager = HeroDefense.Core.SceneContext.Current != null
                ? HeroDefense.Core.SceneContext.Current.EnemyManager
                : null;

            if (manager != null)
                UpdateThreatDirection(manager);
        }

        /// <summary>
        /// Прошла ли задержка после сигнала. Даёт эффект «подтягиваются»:
        /// отряд трогается не мгновенно и не синхронно.
        /// </summary>
        private bool HasAlertDelayPassed =>
            _alertStartedAt >= 0f && Time.time - _alertStartedAt >= alertDelay;

        /// <summary>Отряд уничтожен полностью. Постройка начнёт собирать заново (D21b).</summary>
        public event Action<Squad> Wiped;

        /// <summary>Боец погиб — постройка запустит шкалу пополнения (D21a).</summary>
        public event Action<Squad> UnitLost;

        private void Update()
        {
            TickAlert(Time.deltaTime);
            UpdateFormationShift();
        }

        /// <summary>
        /// Оценка обстановки. Считается редко: точное число врагов не нужно,
        /// важен только факт «их больше порога».
        /// </summary>
        private void TickAlert(float deltaTime)
        {
            // Казарма создаёт отряд сразу, а бойцы появляются позже.
            // Пустому отряду сканировать нечего, а перебор по всем врагам
            // стоит денег — при четырёх казармах это четыре лишних скана.
            if (_units.Count == 0)
                return;

            _alertTimer -= deltaTime;
            _scanTimer -= deltaTime;

            if (_scanTimer > 0f)
                return;

            _scanTimer = scanInterval;

            EnemyManager manager = HeroDefense.Core.SceneContext.Current != null
                ? HeroDefense.Core.SceneContext.Current.EnemyManager
                : null;

            if (manager == null)
                return;

            // Считаем от флага И от каждого бойца: враги идут растянутой
            // цепочкой, и вокруг флага порог может не набраться, хотя
            // рядом с крайним бойцом уже трое.
            int nearby = manager.CountNearby(_flagPosition, alertRadius, alertThreshold);

            for (int i = 0; i < _units.Count && nearby < alertThreshold; i++)
            {
                if (_units[i] == null)
                    continue;

                nearby = Mathf.Max(nearby, manager.CountNearby(
                    _units[i].transform.position, alertRadius, alertThreshold));
            }

            if (nearby < alertThreshold)
                return;

            UpdateThreatDirection(manager);

            // Момент сигнала запоминаем только при подъёме тревоги,
            // иначе задержка сбрасывалась бы каждый скан.
            if (_alertTimer <= 0f)
                _alertStartedAt = Time.time;

            _alertTimer = alertHoldTime;
        }

        /// <summary>
        /// Куда сдвинуть строй. Направление берём на ближайшего врага
        /// от флага: отряд разворачивается к угрозе как единое целое,
        /// и это заодно показывает игроку, откуда пришли.
        /// </summary>
        private void UpdateThreatDirection(EnemyManager manager)
        {
            Enemy nearest = manager.FindNearest(_flagPosition, alertRadius);

            if (nearest == null)
                return;

            Vector3 delta = nearest.transform.position - _flagPosition;
            delta.y = 0f;

            if (delta.sqrMagnitude < 0.01f)
                return;

            _threatDirection = delta.normalized;
        }

        /// <summary>
        /// Центр построения. При тревоге сдвинут к угрозе, иначе — флаг.
        ///
        /// Именно центр, а не отдельные бойцы: если растягивать поводок
        /// каждому по отдельности, отряд перестаёт быть отрядом.
        /// </summary>
        private Vector3 FormationCenter => IsAlerted
            ? _flagPosition + _threatDirection * rallyShift
            : _flagPosition;

        /// <summary>
        /// Строй пересобирается только при смене состояния тревоги,
        /// а не каждый кадр: постоянный пересчёт позиций заставлял бы
        /// бойцов дрожать на месте.
        /// </summary>
        private void UpdateFormationShift()
        {
            bool shouldShift = IsAlerted && _threatDirection.sqrMagnitude > 0.01f;

            if (shouldShift == _formationShifted)
                return;

            _formationShifted = shouldShift;
            AssignFormationPositions();
        }

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
            unit.SetSquad(this);

            AssignFormationPositions();
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

            Vector3 center = FormationCenter;

            if (count == 1)
            {
                _units[0].SetAnchor(center);
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

                _units[i].SetAnchor(center + offset);
            }
        }

        /// <summary>Увеличить лимит — апгрейд постройки.</summary>
        public void SetMaxUnits(int value)
        {
            maxUnits = Mathf.Max(1, value);
        }
    }
}
