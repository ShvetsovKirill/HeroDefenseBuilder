using System;
using System.Collections.Generic;
using UnityEngine;

namespace HeroDefense.Campaign
{
    /// <summary>
    /// Карта кампании: владения и дороги между ними.
    ///
    /// Устроена как слои слева направо. Внутри слоя владения не связаны
    /// между собой — игрок всегда идёт вперёд, и в этом суть: **назад
    /// дороги нет**, свернул на развилке — вторая ветка потеряна вместе
    /// со всем, что на ней лежало.
    ///
    /// Слои, а не свободный граф: так карта читается с одного взгляда,
    /// её нельзя случайно замкнуть в петлю, и «сколько ещё идти» видно
    /// без подсчётов.
    /// </summary>
    [CreateAssetMenu(fileName = "CampaignMap", menuName = "HeroDefense/Карта кампании")]
    public sealed class CampaignMapDefinition : ScriptableObject
    {
        /// <summary>Одно владение на карте вместе с его связями вперёд.</summary>
        [Serializable]
        public sealed class Node
        {
            [Tooltip("Что за владение здесь стоит.")]
            public HoldingDefinition holding;

            [Tooltip("Слой слева направо, с нуля. Игрок проходит слои по порядку.")]
            [Min(0)]
            public int layer;

            [Tooltip("Смещение по вертикали внутри слоя, для раскладки на экране. " +
                     "Чисто визуальное: на выбор не влияет.")]
            public float offset;

            [Tooltip("Индексы узлов следующего слоя, куда отсюда можно пойти.\n\n" +
                     "Пусто у последнего слоя. Если пусто в середине карты — " +
                     "владение станет тупиком, и кампания там оборвётся.")]
            public int[] next = Array.Empty<int>();
        }

        [Header("Владения")]
        [Tooltip("Все узлы карты. Порядок в массиве — это их индексы, " +
                 "на которые ссылается поле «next». Менять порядок после " +
                 "сборки карты нельзя: связи разъедутся.")]
        public Node[] nodes = Array.Empty<Node>();

        [Header("Начало и конец")]
        [Tooltip("Индексы узлов, с которых начинается путь. Обычно один.")]
        public int[] entryNodes = { 0 };

        [Tooltip("Владение последней битвы. Проходится в конце и решает исход кампании.")]
        public HoldingDefinition finalBattle;

        /// <summary>Сколько слоёв на карте. Ноль — карта пуста.</summary>
        public int LayerCount
        {
            get
            {
                int max = -1;

                foreach (Node node in nodes)
                {
                    if (node != null && node.layer > max)
                        max = node.layer;
                }

                return max + 1;
            }
        }

        /// <summary>Узел по индексу. Null, если индекс за границами.</summary>
        public Node GetNode(int index)
        {
            return index >= 0 && index < nodes.Length ? nodes[index] : null;
        }

        /// <summary>
        /// Куда можно пойти из узла. Пустой список означает конец пути —
        /// дальше только последняя битва.
        /// </summary>
        public void GetNextNodes(int index, List<int> result)
        {
            result.Clear();

            Node node = GetNode(index);

            if (node?.next == null)
                return;

            foreach (int next in node.next)
            {
                if (GetNode(next) != null)
                    result.Add(next);
            }
        }

#if UNITY_EDITOR
        /// <summary>
        /// Ошибки карты дороже прочих: их не видно до того, как игрок
        /// упрётся в тупик посреди кампании.
        /// </summary>
        private void OnValidate()
        {
            for (int i = 0; i < nodes.Length; i++)
            {
                Node node = nodes[i];

                if (node == null)
                    continue;

                if (node.holding == null)
                    Debug.LogWarning($"[Карта] Узел {i}: не назначено владение.", this);

                foreach (int next in node.next)
                {
                    Node target = GetNode(next);

                    if (target == null)
                    {
                        Debug.LogWarning($"[Карта] Узел {i} ссылается на несуществующий {next}.", this);
                        continue;
                    }

                    // Дорога только вперёд: связь вбок или назад ломает
                    // необратимость выбора, на которой всё держится.
                    if (target.layer <= node.layer)
                        Debug.LogWarning(
                            $"[Карта] Узел {i} (слой {node.layer}) ведёт в узел {next} " +
                            $"(слой {target.layer}). Дороги должны идти только вперёд.", this);
                }
            }

            if (finalBattle == null)
                Debug.LogWarning("[Карта] Не назначена последняя битва.", this);
        }
#endif
    }
}
