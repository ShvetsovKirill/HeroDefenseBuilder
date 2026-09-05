using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroDefense.Waves
{
    /// <summary>
    /// Какое условие действует на текущую волну.
    ///
    /// Статический, как <c>RunLoadout</c>: условие выбирается экраном между
    /// волнами, а читают его спавн врагов, раннер волн и кошелёк — то есть
    /// три системы, которым незачем знать друг о друге.
    ///
    /// Множители читаются через свойства, а не через ссылку на ассет:
    /// «условия нет» — обычное состояние (первая волна, игрок не выбрал),
    /// и каждый потребитель не должен проверять это сам.
    /// </summary>
    public static class WaveModifiers
    {
        /// <summary>Имя ассета с набором условий внутри Resources.</summary>
        private const string SetPath = "WaveModifierSet";

        private static WaveModifierSet _set;
        private static bool _setLoaded;

        /// <summary>Условие сменилось. HUD показывает его игроку.</summary>
        public static event Action<WaveModifier> Changed;

        /// <summary>Условие текущей волны. Null — волна идёт как задумана.</summary>
        public static WaveModifier Active { get; private set; }

        /// <summary>Множитель числа врагов в группе.</summary>
        public static float EnemyCount => Active != null ? Active.enemyCount : 1f;

        /// <summary>Множитель интервала между появлениями.</summary>
        public static float SpawnInterval => Active != null ? Active.spawnInterval : 1f;

        /// <summary>Множитель здоровья врага.</summary>
        public static float EnemyHealth => Active != null ? Active.enemyHealth : 1f;

        /// <summary>Множитель скорости врага.</summary>
        public static float EnemySpeed => Active != null ? Active.enemySpeed : 1f;

        /// <summary>Множитель золота за убийство.</summary>
        public static float GoldReward => Active != null ? Active.goldReward : 1f;

        /// <summary>Взять условие на следующую волну. Null снимает условие.</summary>
        public static void Apply(WaveModifier modifier)
        {
            if (Active == modifier)
                return;

            Active = modifier;
            Changed?.Invoke(modifier);
        }

        /// <summary>
        /// Снять условие. Зовётся после волны: условие живёт ровно одну волну,
        /// иначе выбранное на пятой продолжало бы действовать на двадцатой,
        /// и игрок давно забыл бы, за что расплачивается.
        /// </summary>
        public static void Clear() => Apply(null);

        /// <summary>
        /// Набор условий, из которого предлагается выбор.
        /// Может быть null — тогда выбора просто не будет.
        /// </summary>
        public static WaveModifierSet Set
        {
            get
            {
                if (_setLoaded)
                    return _set;

                _setLoaded = true;
                _set = Resources.Load<WaveModifierSet>(SetPath);

                return _set;
            }
        }

        /// <summary>
        /// Набрать варианты для показа. Возвращает false, если предлагать
        /// нечего — экран выбора тогда не появляется вовсе.
        /// </summary>
        public static bool TryPick(int count, List<WaveModifier> result)
        {
            result.Clear();

            if (Set == null || Set.modifiers == null)
                return false;

            // Копия списка, из которой варианты вынимаются без повторов:
            // два одинаковых условия рядом выглядят как ошибка.
            var pool = new List<WaveModifier>();

            foreach (WaveModifier modifier in Set.modifiers)
            {
                if (modifier != null)
                    pool.Add(modifier);
            }

            for (int i = 0; i < count && pool.Count > 0; i++)
            {
                int index = UnityEngine.Random.Range(0, pool.Count);

                result.Add(pool[index]);
                pool.RemoveAt(index);
            }

            // Слева безобидное, справа злое: игроку нужна видимая шкала риска,
            // а не три равнозначных прямоугольника.
            result.Sort((a, b) => a.Danger.CompareTo(b.Danger));

            return result.Count > 0;
        }

#if UNITY_EDITOR
        /// <summary>
        /// Сброс статики при выходе из Play Mode: без него условие прошлого
        /// забега досталось бы следующему.
        /// </summary>
        [RuntimeInitializeOnLoadMethod(RuntimeInitializeLoadType.SubsystemRegistration)]
        private static void ResetStatics()
        {
            Active = null;
            Changed = null;
            _set = null;
            _setLoaded = false;
        }
#endif
    }
}
