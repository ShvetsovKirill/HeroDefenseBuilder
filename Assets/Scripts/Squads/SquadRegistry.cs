using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroDefense.Squads
{
    /// <summary>
    /// Реестр казарм и отрядов.
    ///
    /// Решает две задачи, которые раньше делались поиском по сцене:
    ///
    /// 1. Порядок казарм для клавиш 1–4. FindObjectsByType возвращает объекты
    ///    в неопределённом порядке — после перестройки казармы клавиши могли
    ///    поменяться местами, и игрок переставлял не тот флаг.
    ///
    /// 2. Счёт живых бойцов для HUD. Раньше он сканировал всю сцену каждый
    ///    кадр. Здесь сумма считается по событиям.
    ///
    /// Регистрация в порядке постройки: первая построенная казарма — всегда
    /// клавиша 1, и так остаётся до конца забега.
    /// </summary>
    public static class SquadRegistry
    {
        private static readonly List<Barracks> Barracks = new();

        /// <summary>Состав изменился: построена казарма, разрушена, погиб боец.</summary>
        public static event Action Changed;

        /// <summary>Казармы в порядке постройки. Индекс = номер клавиши минус один.</summary>
        public static IReadOnlyList<Barracks> All => Barracks;

        public static int Count => Barracks.Count;

        /// <summary>Суммарное число живых бойцов во всех отрядах.</summary>
        public static int TotalAliveUnits
        {
            get
            {
                int total = 0;

                for (int i = 0; i < Barracks.Count; i++)
                {
                    if (Barracks[i] != null && Barracks[i].Squad != null)
                        total += Barracks[i].Squad.AliveCount;
                }

                return total;
            }
        }

        public static void Register(Barracks barracks)
        {
            if (barracks == null || Barracks.Contains(barracks))
                return;

            Barracks.Add(barracks);
            Changed?.Invoke();
        }

        public static void Unregister(Barracks barracks)
        {
            if (barracks == null || !Barracks.Remove(barracks))
                return;

            Changed?.Invoke();
        }

        public static Barracks GetAt(int index)
        {
            return index >= 0 && index < Barracks.Count ? Barracks[index] : null;
        }

        /// <summary>Сообщить об изменении состава отряда — для HUD.</summary>
        public static void NotifyChanged()
        {
            Changed?.Invoke();
        }

        /// <summary>
        /// Полная очистка. Обязательна при смене карты: реестр статический
        /// и переживёт выгрузку сцены, если его не сбросить.
        /// </summary>
        public static void Clear()
        {
            Barracks.Clear();
            Changed?.Invoke();
        }
    }
}
