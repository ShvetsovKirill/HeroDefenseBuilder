using UnityEngine;
using HeroDefense.Core;

namespace HeroDefense.Campaign
{
    /// <summary>
    /// Метка «этот боец — командир отряда».
    ///
    /// Нужна ради одного: **смерть командира необратима**. Пока метки не
    /// было, гибель командира замечалась только вместе с гибелью всего
    /// отряда — а павший в одиночку командир воскресал в следующем
    /// владении, потому что запись о нём оставалась нетронутой.
    ///
    /// Вешается кодом при рождении бойца, руками ставить не нужно.
    /// </summary>
    [RequireComponent(typeof(Health))]
    [DisallowMultipleComponent]
    public sealed class CommanderMark : MonoBehaviour
    {
        private SquadRecord _record;
        private Health _health;

        /// <summary>Привязать бойца к записи отряда в армии похода.</summary>
        public void Bind(SquadRecord record)
        {
            _record = record;
            _health = GetComponent<Health>();

            if (_health != null)
                _health.Died += OnDied;
        }

        private void OnDestroy()
        {
            if (_health != null)
                _health.Died -= OnDied;
        }

        /// <summary>
        /// Командир пал. Отряд остаётся жить и пополняться за золото,
        /// но имя и специализацию теряет: в лагере игрок решит, дать ли
        /// ему нового командира или разжаловать в ополчение.
        /// </summary>
        private void OnDied()
        {
            if (_record == null || !_record.HasCommander)
                return;

            Debug.Log($"[Кампания] Командир отряда {_record.slot} погиб.");

            _record.LoseCommander();
            CampaignRun.Save();
        }
    }
}
