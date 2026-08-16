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
    /// Умеет лететь в двух типах целей:
    ///   • Enemy — у него есть поколение (живёт в пуле и переиспользуется);
    ///   • Health — боец, постройка, ратуша, они создаются и умирают честно.
    ///
    /// Цель хранится ссылкой, а не точкой: снаряд должен догонять движущуюся
    /// цель. Но если она умерла в полёте — летим в последнюю известную точку
    /// и там гаснем, а не исчезаем в воздухе.
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

        [Tooltip("На какой высоте от основания цели считается попадание.\n\n" +
                 "Для капсулы 0.6 нормально, но у крупной модели стрела " +
                 "полетела бы в ноги. Подбирается под средний рост цели.")]
        [SerializeField] private float aimHeight = 0.6f;

        private Enemy _target;
        private int _targetVersion;
        private Health _healthTarget;
        private Health _source;

        private Vector3 _lastKnownPosition;
        private float _damage;
        private float _timeLeft;
        private bool _isFlying;

        /// <summary>Снаряд отработал и готов вернуться в пул.</summary>
        public event Action<Projectile> Finished;

        // ---------- Запуск ----------

        /// <summary>
        /// Запуск по врагу. Источник передаётся, чтобы враг мог ответить
        /// тому, кто в него попал — но только если стрелок этого заслуживает.
        /// </summary>
        public void Launch(Enemy target, float damage, Health source)
        {
            _healthTarget = null;
            _target = target;
            _targetVersion = target != null ? target.Version : 0;

            InitFlight(damage, source);

            if (target != null)
                AimAt(AimPoint(target));

            gameObject.SetActive(true);
        }

        /// <summary>
        /// Запуск по защитнику: бойцу, постройке, ратуше.
        ///
        /// Отдельно от версии с Enemy, потому что у врага есть поколение
        /// (он живёт в пуле), а у построек и бойцов его нет.
        /// </summary>
        public void LaunchAtHealth(Health target, float damage, Health source)
        {
            _target = null;
            _targetVersion = 0;
            _healthTarget = target;

            InitFlight(damage, source);

            if (target != null)
                AimAt(HealthAimPoint(target));

            gameObject.SetActive(true);
        }

        private void InitFlight(float damage, Health source)
        {
            _damage = damage;
            _source = source;
            _timeLeft = lifetime;
            _isFlying = true;
        }

        /// <summary>
        /// Прицеливание при запуске. Разворот обязателен: снаряд пришёл
        /// из пула с поворотом от прошлого выстрела и первый кадр
        /// выглядел бы летящим боком.
        /// </summary>
        private void AimAt(Vector3 point)
        {
            _lastKnownPosition = point;

            if (alignToDirection)
                AlignTo(point - transform.position);
        }

        // ---------- Полёт ----------

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
            if (IsEnemyTargetValid())
            {
                _lastKnownPosition = AimPoint(_target);
                return;
            }

            if (IsHealthTargetValid())
            {
                _lastKnownPosition = HealthAimPoint(_healthTarget);
                return;
            }

            _target = null;
            _healthTarget = null;
        }

        private bool IsEnemyTargetValid()
        {
            return _target != null
                && _target.IsAlive
                && _target.Version == _targetVersion;
        }

        private bool IsHealthTargetValid()
        {
            return _healthTarget != null && _healthTarget.IsAlive;
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
                AlignTo(direction);
        }

        private void AlignTo(Vector3 direction)
        {
            if (direction.sqrMagnitude < 0.0001f)
                return;

            transform.rotation = Quaternion.LookRotation(direction.normalized, Vector3.up);
        }

        // ---------- Попадание ----------

        private void HitOrFizzle()
        {
            // Урон наносим, только если цель ещё та самая: иначе стрела,
            // выпущенная в убитого, добивала бы случайного соседа.
            if (IsEnemyTargetValid())
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

        // ---------- Точки прицеливания ----------

        /// <summary>
        /// Целимся в середину цели, а не в основание: иначе стрела
        /// втыкается в землю под ногами.
        /// </summary>
        private Vector3 AimPoint(Enemy enemy)
        {
            return enemy.transform.position + Vector3.up * aimHeight;
        }

        private Vector3 HealthAimPoint(Health target)
        {
            return target.transform.position + Vector3.up * aimHeight;
        }
    }
}
