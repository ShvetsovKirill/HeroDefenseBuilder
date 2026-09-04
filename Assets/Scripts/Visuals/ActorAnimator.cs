using UnityEngine;

namespace HeroDefense.Visuals
{
    /// <summary>
    /// Источник состояния для анимаций. Реализуют боец, враг, король.
    ///
    /// Зачем интерфейс: логика не должна знать про Animator (D10).
    /// Мост спрашивает «что сейчас происходит», а не лезет в геймплейные классы.
    /// </summary>
    public interface IAnimatedActor
    {
        /// <summary>Скорость передвижения, 0..1. Для перехода idle → walk → run.</summary>
        float NormalizedSpeed { get; }

        /// <summary>Жив ли. Мёртвый проигрывает смерть и больше не двигается.</summary>
        bool IsAlive { get; }
    }

    /// <summary>
    /// Боевая стойка. Отдельный интерфейс, а не поле в IAnimatedActor:
    /// его реализуют не все — врагу боевой idle не нужен, он и так
    /// всегда идёт в атаку.
    /// </summary>
    public interface ICombatStance
    {
        /// <summary>
        /// Готов к бою: враг рядом, но схватка ещё не началась.
        /// Бойцы опускают копья и поднимают щиты.
        /// </summary>
        bool IsCombatReady { get; }
    }

    /// <summary>
    /// Переводит состояние юнита в параметры Animator.
    ///
    /// Ставится на КОРЕНЬ юнита, ссылается на Animator внутри визуальной части.
    /// Так модель со всеми костями и клипами меняется целиком, а логика
    /// не замечает подмены (D10, арт-пайплайн слой 3).
    ///
    /// Имена параметров вынесены в поля: разные ассеты с Asset Store
    /// используют разные соглашения, и подгонять их проще здесь,
    /// чем переименовывать в контроллере аниматора.
    /// </summary>
    public sealed class ActorAnimator : MonoBehaviour
    {
        [Header("Ссылки")]
        [Tooltip("Animator внутри визуальной части. Пусто — поищем в детях.")]
        [SerializeField] private Animator animator;

        [Header("Имена параметров")]
        [SerializeField] private string speedParameter = "Speed";
        [SerializeField] private string attackTrigger = "Attack";
        [SerializeField] private string deathTrigger = "Die";
        [SerializeField] private string hitTrigger = "Hit";

        [Tooltip("Булев параметр боевой стойки. Отряд поднят по тревоге — " +
                 "бойцы переходят из спокойного idle в боевой: опускают копья, " +
                 "поднимают щиты. Деталь из Bad North, стоит дёшево, " +
                 "а напряжение читается сразу.")]
        [SerializeField] private string combatStanceParameter = "CombatReady";

        [Header("Настройка")]
        [Tooltip("Сглаживание скорости: без него переход idle → run дёргается " +
                 "при каждой смене направления.")]
        [SerializeField] private float speedDamping = 0.15f;

        [Tooltip("Проигрывать ли реакцию на попадание. При частых ударах " +
                 "она перебивает атаку и юнит выглядит парализованным.")]
        [SerializeField] private bool playHitReaction;

        private IAnimatedActor _actor;
        private ICombatStance _stance;
        private bool _deathPlayed;

        private int _speedId;
        private int _attackId;
        private int _deathId;
        private int _hitId;
        private int _stanceId;

        private void Awake()
        {
            if (animator == null)
                animator = GetComponentInChildren<Animator>();

            _actor = GetComponent<IAnimatedActor>();
            _stance = GetComponent<ICombatStance>();

            CacheParameterIds();
        }

        /// <summary>
        /// Хеши вместо строк: поиск по имени выполняется при каждом вызове,
        /// а у нас десятки юнитов на экране.
        /// </summary>
        private void CacheParameterIds()
        {
            _speedId = Animator.StringToHash(speedParameter);
            _attackId = Animator.StringToHash(attackTrigger);
            _deathId = Animator.StringToHash(deathTrigger);
            _hitId = Animator.StringToHash(hitTrigger);
            _stanceId = Animator.StringToHash(combatStanceParameter);
        }

        private void Update()
        {
            if (animator == null || _actor == null)
                return;

            if (!_actor.IsAlive)
            {
                PlayDeathOnce();
                return;
            }

            animator.SetFloat(_speedId, _actor.NormalizedSpeed, speedDamping, Time.deltaTime);

            if (_stance != null)
                animator.SetBool(_stanceId, _stance.IsCombatReady);
        }

        /// <summary>Вызывается в момент удара или выстрела.</summary>
        public void PlayAttack()
        {
            if (animator != null && _actor != null && _actor.IsAlive)
                animator.SetTrigger(_attackId);
        }

        /// <summary>Вызывается при получении урона.</summary>
        public void PlayHit()
        {
            if (playHitReaction && animator != null && _actor != null && _actor.IsAlive)
                animator.SetTrigger(_hitId);
        }

        /// <summary>
        /// Смерть проигрывается один раз: триггер, поставленный дважды,
        /// перезапустит клип и труп дёрнется.
        /// </summary>
        private void PlayDeathOnce()
        {
            if (_deathPlayed)
                return;

            _deathPlayed = true;
            animator.SetTrigger(_deathId);
        }

        /// <summary>Сброс при возврате из пула — иначе враг воскреснет мёртвым.</summary>
        public void ResetState()
        {
            _deathPlayed = false;

            if (animator != null)
            {
                animator.ResetTrigger(_deathId);
                animator.SetFloat(_speedId, 0f);
            }
        }
    }
}
