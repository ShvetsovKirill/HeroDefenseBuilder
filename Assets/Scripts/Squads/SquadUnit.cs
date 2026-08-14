using System;
using UnityEngine;
using HeroDefense.Combat;
using HeroDefense.Core;
using HeroDefense.Enemies;

namespace HeroDefense.Squads
{
    /// <summary>
    /// Один боец отряда.
    ///
    /// Стоит у назначенной точки, идёт драться с врагом, зашедшим в радиус,
    /// возвращается обратно. Приказов не принимает — всё управление
    /// идёт через флаг отряда (D12).
    ///
    /// Здоровье — общий Health (D40). Стрельба — общий AutoAttacker.
    /// Мечник и лучник отличаются только числами engageRadius и attackRange (D13).
    /// </summary>
    [RequireComponent(typeof(Health))]
    public sealed class SquadUnit : MonoBehaviour
    {
        [Header("Движение")]
        [SerializeField] private float moveSpeed = 3.5f;

        [Tooltip("Радиус, в котором боец покидает свою точку ради врага. " +
                 "Маленький у всех типов: это якорь, а не зона патрулирования.")]
        [SerializeField] private float engageRadius = 4f;

        [Tooltip("Насколько близко подходить к врагу. Для мечника — вплотную.")]
        [SerializeField] private float meleeDistance = 1.2f;

        [Tooltip("Допуск при возврате. Не должен быть слишком мал — " +
                 "иначе бойцы дёргаются на месте.")]
        [SerializeField] private float arriveTolerance = 0.6f;

        [Header("Поиск цели")]
        [Tooltip("Как часто искать новую цель, в секундах. " +
                 "Каждый кадр не нужно: это полный перебор по всем врагам, " +
                 "а при трёх отрядах таких переборов было бы 18 за кадр.")]
        [SerializeField] private float retargetInterval = 0.25f;

        [Header("Расталкивание")]
        [Tooltip("Личное пространство между бойцами. Без него отряд " +
                 "слипается при движении, даже несмотря на построение кольцом.")]
        [SerializeField] private float separationRadius = 0.7f;

        [SerializeField] private float separationStrength = 0.8f;

        [Tooltip("Слой бойцов. Без маски OverlapSphere ловит вообще всё — " +
                 "землю, врагов, постройки, коллайдер короля — и отсеивает " +
                 "их дорогим GetComponentInParent каждый кадр на каждом бойце.")]
        [SerializeField] private LayerMask unitLayer = ~0;

        private static readonly Collider[] NeighbourBuffer = new Collider[8];

        private Health _health;
        private AutoAttacker _attacker;

        private Vector3 _anchor;
        private Enemy _currentTarget;
        private int _targetVersion;
        private float _retargetTimer;

        /// <summary>Боец погиб. Отряд снимет его со счёта и запросит пополнение.</summary>
        public event Action<SquadUnit> Died;

        public Health Health => _health;
        public bool IsAlive => _health != null && _health.IsAlive;

        private void Awake()
        {
            _health = GetComponent<Health>();
            _attacker = GetComponent<AutoAttacker>();

            _anchor = transform.position;

            // Стрельбой управляет боец: иначе AutoAttacker искал бы цель
            // сам, и юнит бежал бы к одному врагу, а стрелял в другого.
            if (_attacker != null)
                _attacker.TakeTargetControl();
        }

        private void OnEnable()
        {
            _health.Died += OnDied;
        }

        private void OnDisable()
        {
            _health.Died -= OnDied;
        }

        private void OnDied()
        {
            Died?.Invoke(this);
        }

        /// <summary>
        /// Куда боец должен возвращаться. Задаётся отрядом: это точка
        /// возле флага, а не сам флаг — иначе все встанут в одну точку.
        /// </summary>
        public void SetAnchor(Vector3 position)
        {
            _anchor = position;
        }

        private void Update()
        {
            if (!IsAlive || !IsGameRunning)
                return;

            UpdateTarget(Time.deltaTime);
            UpdateMovement(Time.deltaTime);
        }

        private static bool IsGameRunning =>
            GameState.Current == null || GameState.Current.IsPlaying;

        // ---------- Выбор цели ----------

        /// <summary>
        /// Ищем врагов вокруг ЯКОРЯ, а не вокруг себя.
        /// Иначе боец, погнавшись за одним, увидел бы следующего уже
        /// с новой позиции и утянулся бы через всю карту.
        /// </summary>
        private void UpdateTarget(float deltaTime)
        {
            _retargetTimer -= deltaTime;

            if (IsTargetStillValid() && _retargetTimer > 0f)
                return;

            _retargetTimer = retargetInterval;

            EnemyManager manager = SceneContext.Current != null
                ? SceneContext.Current.EnemyManager
                : null;

            _currentTarget = manager != null
                ? manager.FindNearest(_anchor, engageRadius)
                : null;

            _targetVersion = _currentTarget != null ? _currentTarget.Version : 0;

            // Отдаём цель стрелку — он больше не ищет её сам.
            if (_attacker != null)
                _attacker.SetTarget(_currentTarget);
        }

        private bool IsTargetStillValid()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
                return false;

            // Объект тот же, но из пула его выдали под другого врага.
            if (_currentTarget.Version != _targetVersion)
                return false;

            Vector3 delta = _currentTarget.transform.position - _anchor;
            delta.y = 0f;

            return delta.sqrMagnitude <= engageRadius * engageRadius;
        }

        // ---------- Перемещение ----------

        private void UpdateMovement(float deltaTime)
        {
            Vector3 desired = _currentTarget != null
                ? ResolveChaseDirection()
                : ResolveReturnDirection();

            Vector3 separation = ComputeSeparation() * separationStrength;
            Vector3 total = desired + separation;

            if (total.sqrMagnitude < 0.0001f)
                return;

            total.Normalize();

            FaceDirection(desired.sqrMagnitude > 0.0001f ? desired : total);
            transform.position += total * (moveSpeed * deltaTime);
        }

        private Vector3 ResolveChaseDirection()
        {
            Vector3 toTarget = _currentTarget.transform.position - transform.position;
            toTarget.y = 0f;

            // Дошёл до дистанции удара — стоим и бьём.
            if (toTarget.sqrMagnitude <= meleeDistance * meleeDistance)
            {
                FaceDirection(toTarget);
                return Vector3.zero;
            }

            return toTarget.normalized;
        }

        private Vector3 ResolveReturnDirection()
        {
            Vector3 toAnchor = _anchor - transform.position;
            toAnchor.y = 0f;

            if (toAnchor.sqrMagnitude <= arriveTolerance * arriveTolerance)
                return Vector3.zero;

            return toAnchor.normalized;
        }

        /// <summary>
        /// Расталкивание между бойцами.
        ///
        /// Союзников десятки, а не сотни, поэтому пространственная сетка
        /// как у врагов здесь избыточна — хватает OverlapSphere
        /// с общим буфером без аллокаций.
        /// </summary>
        private Vector3 ComputeSeparation()
        {
            if (separationRadius <= 0f)
                return Vector3.zero;

            int count = Physics.OverlapSphereNonAlloc(
                transform.position, separationRadius, NeighbourBuffer, unitLayer);

            Vector3 push = Vector3.zero;

            for (int i = 0; i < count; i++)
            {
                Collider other = NeighbourBuffer[i];

                if (other == null || other.transform == transform)
                    continue;

                if (other.GetComponentInParent<SquadUnit>() == null)
                    continue;

                Vector3 delta = transform.position - other.transform.position;
                delta.y = 0f;

                float distanceSqr = delta.sqrMagnitude;

                if (distanceSqr < 0.0001f || distanceSqr >= separationRadius * separationRadius)
                    continue;

                float distance = Mathf.Sqrt(distanceSqr);
                push += delta / distance * (1f - distance / separationRadius);
            }

            return push;
        }

        private void FaceDirection(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return;

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }
    }
}
