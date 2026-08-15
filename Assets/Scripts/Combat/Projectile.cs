using System;
using UnityEngine;
using HeroDefense.Core;
using HeroDefense.Enemies;

namespace HeroDefense.Combat
{
    /// <summary>
    /// Летящий снаряд: стрела, болт, магический шар.
    ///
    /// Зачем вместо мгновенного урона: игрок должен видеть, кто в кого стреляет.
    /// При десятках юнитов на экране хитскан читается как «все умирают сами
    /// по себе» — непонятно, работает башня или нет.
    ///
    /// Цель хранится ссылкой, а не точкой: стрела должна догонять движущегося
    /// врага, иначе на быстрых раннерах промахи будут постоянными.
    /// Но если цель умерла в полёте — снаряд летит в последнюю известную точку
    /// и там гаснет, а не исчезает в воздухе.
    /// </summary>
    public sealed class Projectile : MonoBehaviour
    {
        [Header("Полёт")]
        [SerializeField] private float speed = 18f;

        [Tooltip("Через сколько секунд снаряд исчезнет, если ни во что не попал.")]
        [SerializeField] private float lifetime = 3f;

        [Tooltip("Дистанция, на которой считается попадание.")]
        [SerializeField] private float hitDistance = 0.4f;

        [Header("Вид")]
        [Tooltip("Разворачивать ли снаряд по направлению полёта. " +
                 "Для стрелы — да, для шара незачем.")]
        [SerializeField] private bool alignToDirection = true;

        [Tooltip("Что остаётся в точке попадания. Необязательно.")]
        [SerializeField] private GameObject impactEffect;

        private Enemy _target;
        private int _targetVersion;
        private Health _healthTarget;
        private Health _source;

        private Vector3 _lastKnownPosition;
        private float _damage;
        private float aimHeight;
        private float _timeLeft;
        private bool _isFlying;

        /// <summary>Снаряд отработал и готов вернуться в пул.</summary>
        public event Action<Projectile> Finished;

        /// <summary>
        /// Запустить снаряд. Источник передаётся, чтобы враг мог ответить
        /// тому, кто в него попал — но только если стрелок этого заслуживает.
        /// </summary>
        /// <summary>
        /// Запуск по защитнику: бойцу, постройке, ратуше.
        ///
        /// Отдельно от версии с Enemy, потому что у врага есть поколение
        /// (он живёт в пуле и переиспользуется), а у построек и бойцов
        /// его нет — они создаются и уничтожаются честно.
        /// </summary>
        public void LaunchAtHealth(Health target, float damage, Health source)
        {
            _healthTarget = target;
            _target = null;
            _targetVersion = 0;

            InitFlight(damage, source);

            if (target != null)
            {
                _lastKnownPosition = target.transform.position + Vector3.up * aimHeight;

                //if (alignToDirection)
                //    AlignTo(_lastKnownPosition - transform.position);
            }

            gameObject.SetActive(true);
        }

        private void InitFlight(float damage, Health source)
        {
            _damage = damage;
            _source = source;
            _timeLeft = lifetime;
            _isFlying = true;
        }

        public void Launch(Enemy target, float damage, Health source)
        {
            _healthTarget = null;
            _target = target;
            _targetVersion = target != null ? target.Version : 0;
            _damage = damage;
            _source = source;
            _timeLeft = lifetime;
            _isFlying = true;

            if (target != null)
                _lastKnownPosition = AimPoint(target);

            gameObject.SetActive(true);
        }

        private void Update()
        {
            if (!_isFlying)
                return;

            _timeLeft -= Time.deltaTime;

            if (_timeLeft <= 0f)
            {
                Finish();
                return;
            }

            UpdateAim();
            MoveTowardsAim();
        }

        /// <summary>
        /// Пока цель жива — целимся в неё. Умерла или переиспользована
        /// из пула — летим в последнюю точку и гаснем там.
        /// </summary>
        private void UpdateAim()
        {
            if (IsTargetValid())
            {
                _lastKnownPosition = AimPoint(_target);
                return;
            }

            if (IsHealthTargetValid())
            {
                _lastKnownPosition = _healthTarget.transform.position + Vector3.up * aimHeight;
                return;
            }

            _target = null;
            _healthTarget = null;
        }

        private bool IsHealthTargetValid()
        {
            return _healthTarget != null && _healthTarget.IsAlive;
        }

        private bool IsTargetValid()
        {
            return _target != null
                && _target.IsAlive
                && _target.Version == _targetVersion;
        }

        private void MoveTowardsAim()
        {
            Vector3 delta = _lastKnownPosition - transform.position;
            float distance = delta.magnitude;

            if (distance <= hitDistance)
            {
                HitOrFizzle();
                return;
            }

            Vector3 direction = delta / distance;

            transform.position += direction * (speed * Time.deltaTime);

            if (alignToDirection)
                transform.rotation = Quaternion.LookRotation(direction, Vector3.up);
        }

        private void HitOrFizzle()
        {
            // Урон наносим, только если цель ещё та самая: иначе стрела,
            // выпущенная в убитого, добивала бы случайного соседа.
            if (IsTargetValid())
                _target.TakeDamage(_damage, _source);
            else if (IsHealthTargetValid())
                _healthTarget.TakeDamage(_damage);

            SpawnImpact();
            Finish();
        }

        private void SpawnImpact()
        {
            if (impactEffect != null)
                Instantiate(impactEffect, transform.position, transform.rotation);
        }

        private void Finish()
        {
            _isFlying = false;
            _target = null;
            _healthTarget = null;
            _source = null;

            gameObject.SetActive(false);
            Finished?.Invoke(this);
        }

        /// <summary>
        /// Целимся в середину врага, а не в основание: иначе стрела
        /// втыкается в землю у него под ногами.
        /// </summary>
        private static Vector3 AimPoint(Enemy enemy)
        {
            return enemy.transform.position + Vector3.up * 0.6f;
        }
    }
}
