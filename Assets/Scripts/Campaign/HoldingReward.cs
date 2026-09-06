using System;
using UnityEngine;
using HeroDefense.Building;

namespace HeroDefense.Campaign
{
    /// <summary>Чем владение платит за помощь.</summary>
    public enum RewardKind
    {
        /// <summary>Престиж: универсальная валюта армии и улучшений.</summary>
        Prestige = 0,

        /// <summary>Новый командир. Даёт только баронство или замок.</summary>
        Commander = 1,

        /// <summary>Новый тип постройки в колоду до конца кампании.</summary>
        BuildingCard = 2,

        /// <summary>Постоянное усиление отряда или короля на весь поход.</summary>
        Boon = 3
    }

    /// <summary>
    /// Награда за пройденное владение.
    ///
    /// Отдельный сериализуемый класс, а не поля в дефиниции: у владения
    /// может быть несколько наград сразу — престиж плюс командир, — и
    /// список читается понятнее, чем набор необязательных полей.
    /// </summary>
    [Serializable]
    public sealed class HoldingReward
    {
        [Tooltip("Что даём. От типа зависит, какое из полей ниже читается.")]
        public RewardKind kind = RewardKind.Prestige;

        [Min(0)]
        [Tooltip("Сколько престижа. Читается только для награды «Престиж».")]
        public int prestige = 20;

        [Tooltip("Какой тип постройки уходит в колоду. Только для награды «Карта».")]
        public BuildingDefinition buildingCard;

        [Tooltip("Ключ усиления. Только для награды «Усиление». " +
                 "Список усилений пока не составлен — поле задел на будущее.")]
        public string boonId;

        /// <summary>Короткая строка для экрана карты: что тут дадут.</summary>
        public string Describe()
        {
            return kind switch
            {
                RewardKind.Prestige => $"+{prestige} престижа",
                RewardKind.Commander => "новый командир",
                RewardKind.BuildingCard => buildingCard != null
                    ? $"постройка: {buildingCard.DisplayName}"
                    : "новая постройка",
                RewardKind.Boon => string.IsNullOrEmpty(boonId) ? "усиление" : boonId,
                _ => string.Empty
            };
        }
    }
}
