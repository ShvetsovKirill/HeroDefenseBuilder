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
    public sealed class SquadUnit : MonoBehaviour, HeroDefense.Visuals.IAnimatedActor
    {
        [Header("Движение")]
        [SerializeField] private float moveSpeed = 3.5f;

        [Tooltip("Поводок: насколько далеко боец может уйти от якоря. " +
                 "Это не радиус зрения — врагов он замечает вокруг СЕБЯ, " +
                 "а поводок только не даёт утянуться через всю карту.")]
        [SerializeField] private float engageRadius = 4f;

        [Tooltip("Радиус, в котором боец замечает врага. Считается от него " +
                 "самого, а не от якоря.\n\n" +
                 "Раньше поиск шёл от якоря, и боец у казармы не реагировал " +
                 "на врага, который ломал постройку в двух метрах — тот " +
                 "оказывался вне радиуса от точки сбора.")]
        [SerializeField] private float sightRadius = 5f;

        [Tooltip("Дистанция, на которой боец отвечает ДАЖЕ БЕЗ тревоги отряда. " +
                 "Это самозащита: враг вплотную, стоять столбом глупо.")]
        [SerializeField] private float selfDefenceRadius = 2f;

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

        [Header("Смерть")]
        [Tooltip("Через сколько секунд убрать тело.\n\n" +
                 "Не сразу: должна успеть проиграться анимация смерти. " +
                 "Но и не навсегда — трупы участвуют в расталкивании " +
                 "и живые обходят их как препятствия.")]
        [SerializeField] private float corpseLifetime = 3f;

        [Header("Расталкивание")]
        [Tooltip("Личное пространство между бойцами. Без него отряд " +
                 "слипается при движении, даже несмотря на построение кольцом.")]
        [SerializeField] private float separationRadius = 0.7f;

        [SerializeField] private float separationStrength = 0.8f;

        private static readonly Collider[] NeighbourBuffer = new Collider[8];

        private Health _health;
        private AutoAttacker _attacker;
        private Squad _squad;

        private Vector3 _anchor;
        private Enemy _currentTarget;
        private int _targetVersion;
        private float _retargetTimer;

        /// <summary>Боец погиб. Отряд снимет его со счёта и запросит пополнение.</summary>
        public event Action<SquadUnit> Died;

        public Health Health => _health;
        public bool IsAlive => _health != null && _health.IsAlive;

        /// <summary>
        /// Насколько быстро боец сейчас движется, 0..1. Читает ActorAnimator.
        /// Считается по факту перемещения, а не по намерению: если боец
        /// упёрся в поводок, ноги не должны продолжать бежать.
        /// </summary>
        public float NormalizedSpeed { get; private set; }

        private void Awake()
        {
            _health = GetComponent<Health>();
            _attacker = GetComponent<AutoAttacker>();

            _anchor = transform.position;

            // Стрельбой управляет боец: иначе AutoAttacker искал бы цель
            // сам, и юнит бежал бы к одному врагу, а стрелял в другого.
            if (_attacker != null)
            {
                _attacker.TakeTargetControl();

                // Бойцы держат врагов на себе — это их роль (затычка).
                // Ставим здесь, а не галочкой на префабе: забытая галочка
                // сломала бы поведение молча.
                _attacker.SetProvokeRetaliation(true);
            }
        }

        private void OnEnable()
        {
            _health.Died += OnDied;
            _health.Damaged += OnDamaged;
        }

        private void OnDisable()
        {
            _health.Died -= OnDied;
            _health.Damaged -= OnDamaged;
        }

        /// <summary>
        /// По нам бьют — поднимаем тревогу всему отряду немедленно.
        ///
        /// Без этого боец, которого атакуют вне радиуса самозащиты, стоял бы
        /// и умирал, пока рядом не наберётся достаточно врагов для порога.
        /// А атакующий враг — это уже достаточное основание.
        /// </summary>
        /// <summary>
        /// По нам бьют — поднимаем тревогу всему отряду немедленно.
        ///
        /// Без этого боец, которого атакуют вне радиуса самозащиты, стоял бы
        /// и умирал, пока рядом не наберётся достаточно врагов для порога.
        /// А атакующий враг — это уже достаточное основание.
        ///
        /// Отдельно важно для стрелков: они бьют издалека, порог по количеству
        /// у флага может вообще не набраться, и отряд стоял бы под обстрелом
        /// в полном неведении.
        /// </summary>
        private void OnDamaged(float amount)
        {
            if (_squad != null)
                _squad.RaiseAlert();
        }

        private void OnDied()
        {
            Died?.Invoke(this);

            Audio.Sfx.PlayAt(Audio.SoundId.UnitDeath, transform.position);

            // Отключаем расталкивание и поиск целей сразу, а объект убираем
            // с задержкой: иначе тело толкало бы живых и мешало строю.
            enabled = false;

            DisableCollider();
            Destroy(gameObject, corpseLifetime);
        }

        /// <summary>
        /// Коллидер выключаем отдельно: он используется для расталкивания,
        /// и без этого труп остался бы препятствием на все три секунды.
        /// </summary>
        private void DisableCollider()
        {
            var ownCollider = GetComponent<Collider>();

            if (ownCollider != null)
                ownCollider.enabled = false;
        }

        /// <summary>Кто им командует. Задаётся отрядом при зачислении.</summary>
        public void SetSquad(Squad squad)
        {
            _squad = squad;
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
        /// Врагов ищем вокруг СЕБЯ, а удаление от якоря ограничиваем отдельно.
        ///
        /// Раньше поиск шёл от якоря — и боец, стоящий у казармы, не видел
        /// врага, который ломал её в двух метрах: тот был вне радиуса
        /// от точки сбора, хотя вплотную к самому бойцу.
        ///
        /// Теперь engageRadius работает как поводок: боец замечает всё вокруг,
        /// но не уходит за пределы привязки к флагу.
        /// </summary>
        private void UpdateTarget(float deltaTime)
        {
            _retargetTimer -= deltaTime;

            if (IsTargetStillValid() && _retargetTimer > 0f)
                return;

            _retargetTimer = retargetInterval;
            _currentTarget = FindReachableEnemy();
            _targetVersion = _currentTarget != null ? _currentTarget.Version : 0;

            // Отдаём цель стрелку — он больше не ищет её сам.
            if (_attacker != null)
                _attacker.SetTarget(_currentTarget);
        }

        /// <summary>
        /// Ближайший враг, до которого можно дойти, не порвав поводок.
        ///
        /// Проверяем не только «вижу ли», но и «дотянусь ли»: иначе боец
        /// побежал бы к врагу, упёрся в границу поводка и застрял бы
        /// на полпути, не стреляя и не возвращаясь.
        /// </summary>
        private Enemy FindReachableEnemy()
        {
            EnemyManager manager = SceneContext.Current != null
                ? SceneContext.Current.EnemyManager
                : null;

            if (manager == null)
                return null;

            // Без тревоги отряда боец реагирует только на то, что вплотную:
            // так отряд входит в бой группой, а не растаскивается по одному
            // на каждого пробегающего мимо врага.
            float radius = IsSquadAlerted ? sightRadius : selfDefenceRadius;

            Enemy candidate = manager.FindNearest(transform.position, radius);

            return IsWithinLeash(candidate) ? candidate : null;
        }

        /// <summary>
        /// Влезает ли враг в поводок. Дальность оружия учитывается:
        /// лучнику не нужно подходить вплотную, поэтому он достаёт дальше,
        /// не сходя с места (D13).
        /// </summary>
        private bool IsWithinLeash(Enemy enemy)
        {
            if (enemy == null)
                return false;

            Vector3 delta = enemy.transform.position - _anchor;
            delta.y = 0f;

            float reach = engageRadius + AttackReach;

            return delta.sqrMagnitude <= reach * reach;
        }

        /// <summary>
        /// Поднят ли отряд по тревоге. Если отряда нет (боец сам по себе) —
        /// считаем, что разрешено всё: незачем делать одиночку беспомощным.
        /// </summary>
        private bool IsSquadAlerted => _squad == null || _squad.IsAlerted;

        /// <summary>Дальность оружия — сколько боец достаёт, стоя на месте.</summary>
        private float AttackReach => _attacker != null ? _attacker.Range : meleeDistance;

        /// <summary>
        /// Насколько далеко боец достаёт от якоря: поводок плюс оружие.
        /// Отряд использует это, чтобы понять, на сколько сдвинуть строй
        /// к дальнему стрелку.
        /// </summary>
        public float TotalReach => engageRadius + AttackReach;

        private bool IsTargetStillValid()
        {
            if (_currentTarget == null || !_currentTarget.IsAlive)
                return false;

            // Объект тот же, но из пула его выдали под другого врага.
            if (_currentTarget.Version != _targetVersion)
                return false;

            return IsWithinLeash(_currentTarget);
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
            {
                NormalizedSpeed = 0f;
                return;
            }

            total.Normalize();

            FaceDirection(desired.sqrMagnitude > 0.0001f ? desired : total);

            Vector3 before = transform.position;

            transform.position += total * (moveSpeed * deltaTime);

            ClampToLeash();

            UpdateNormalizedSpeed(before, deltaTime);
        }

        /// <summary>
        /// Не даём уйти за поводок физически.
        ///
        /// Проверки при выборе цели недостаточно: враг может отойти уже
        /// после того, как боец побежал. Без ограничения отряд растянулся бы
        /// за отступающими врагами и оголил направление — а он якорь,
        /// а не преследователь.
        /// </summary>
        private void ClampToLeash()
        {
            Vector3 fromAnchor = transform.position - _anchor;
            fromAnchor.y = 0f;

            float maxDistance = engageRadius;

            if (fromAnchor.sqrMagnitude <= maxDistance * maxDistance)
                return;

            Vector3 clamped = _anchor + fromAnchor.normalized * maxDistance;

            clamped.y = transform.position.y;
            transform.position = clamped;
        }

        /// <summary>
        /// Скорость по факту смещения: боец, упёршийся в поводок,
        /// должен стоять, а не перебирать ногами на месте.
        /// </summary>
        private void UpdateNormalizedSpeed(Vector3 previousPosition, float deltaTime)
        {
            if (deltaTime <= 0f || moveSpeed <= 0f)
            {
                NormalizedSpeed = 0f;
                return;
            }

            float travelled = (transform.position - previousPosition).magnitude;

            NormalizedSpeed = Mathf.Clamp01(travelled / (moveSpeed * deltaTime));
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
                transform.position, separationRadius, NeighbourBuffer);

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
