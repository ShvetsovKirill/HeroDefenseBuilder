using UnityEngine;

namespace HeroDefense.Squads
{
    /// <summary>
    /// Описание постройки, производящей отряд.
    /// Казарма → копейщики, стрельбище → лучники, арена → мечники (D21).
    ///
    /// Тип отряда задаётся постройкой, а не выбирается потом:
    /// решение принимается заранее, с риском.
    /// </summary>
    [CreateAssetMenu(fileName = "Barracks", menuName = "HeroDefense/Barracks Definition")]
    public sealed class BarracksDefinition : ScriptableObject
    {
        [Header("Описание")]
        public string displayName = "Казарма";

        [Header("Отряд")]
        [Tooltip("Префаб бойца с компонентами SquadUnit, Health и AutoAttacker.")]
        public GameObject unitPrefab;

        [Min(1)]
        [Tooltip("Полный размер отряда. По плану 6–8.")]
        public int squadSize = 6;

        [Header("Пополнение")]
        [Min(0)]
        [Tooltip("Цена одного бойца. По правилу баланса — около 10% дохода за волну.")]
        public int unitCost = 10;

        [Min(0.1f)]
        [Tooltip("Сколько секунд заполняется шкала одного бойца.")]
        public float unitBuildTime = 4f;

        [Min(1)]
        [Tooltip("Сколько новобранцев выходит за раз. " +
                 "По одному они скармливаются врагу поштучно на марше — " +
                 "группой доходят живыми.")]
        public int unitsPerBatch = 2;

        [Header("Флаг")]
        [Tooltip("Цвет знамени этого отряда. Единственный способ отличить отряды на карте.")]
        public Color flagColor = Color.blue;
    }
}
