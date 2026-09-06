using System;
using UnityEngine;

namespace HeroDefense.Campaign
{
    /// <summary>
    /// Отряд в составе армии — то, что переживает бой и живёт между
    /// владениями.
    ///
    /// Не путать с <c>Squad</c> из боя: тот существует, пока идёт забег,
    /// и знает про флаг, бойцов и казарму. Здесь — учётная запись:
    /// кто командует, чем вооружены, живы ли вообще.
    ///
    /// Сериализуемый класс, а не ScriptableObject: это состояние
    /// конкретной кампании, оно уходит в сохранение и меняется каждый бой.
    /// </summary>
    [Serializable]
    public sealed class SquadRecord
    {
        [Tooltip("Номер отряда: он же клавиша 1–9, которой отряд вызывается в бою.")]
        public int slot;

        [Tooltip("Специализация. Милиция означает, что командира нет.")]
        public SquadSpecialization specialization = SquadSpecialization.Militia;

        [Tooltip("Имя ассета командира. Пусто — отряд без командира.\n\n" +
                 "Храним имя, а не ссылку: сохранение должно переживать " +
                 "перезапуск игры, а ссылки на ассеты в JSON не живут.")]
        public string commanderId;

        [Min(1)]
        [Tooltip("Сколько бойцов в отряде на полном составе. Растёт от улучшений.")]
        public int size = 6;

        [Tooltip("Отряд погиб целиком в бою. Слот остаётся, людей нет.")]
        public bool wipedOut;

        /// <summary>Есть ли у отряда командир.</summary>
        public bool HasCommander => !string.IsNullOrEmpty(commanderId);

        /// <summary>
        /// Отряд потерял командира, но уцелел сам: имя и специализация
        /// сняты, судьбу решают в лагере.
        /// </summary>
        public bool IsOrphaned => !HasCommander && specialization != SquadSpecialization.Militia;

        /// <summary>
        /// Командир погиб. Отряд не распускается — он теряет имя
        /// и специализацию, а в лагере игрок решит: дать нового командира
        /// или разжаловать обратно в милицию.
        /// </summary>
        public void LoseCommander()
        {
            commanderId = string.Empty;
        }

        /// <summary>Разжаловать осиротевший отряд обратно в ополчение.</summary>
        public void DemoteToMilitia()
        {
            commanderId = string.Empty;
            specialization = SquadSpecialization.Militia;
        }

        /// <summary>Назначить командира и специализацию. Милицией так стать нельзя.</summary>
        public void Assign(string newCommanderId, SquadSpecialization newSpecialization)
        {
            if (string.IsNullOrEmpty(newCommanderId) || newSpecialization == SquadSpecialization.Militia)
            {
                Debug.LogWarning("[Армия] Назначение без командира или без специализации " +
                                 "не делает отряд именным. Для разжалования есть DemoteToMilitia.");
                return;
            }

            commanderId = newCommanderId;
            specialization = newSpecialization;
        }
    }
}
