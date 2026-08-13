using System.Collections.Generic;
using UnityEngine;

namespace HeroDefense.Enemies
{
    /// <summary>
    /// Равномерная сетка для поиска ближайших соседей.
    ///
    /// Зачем: наивное расталкивание сравнивает каждого с каждым — при 300 юнитах
    /// это 90 000 проверок в кадр. Сетка сводит задачу к просмотру своей ячейки
    /// и восьми соседних, то есть к десяткам проверок вместо тысяч.
    ///
    /// Размер ячейки берётся равным радиусу расталкивания: тогда все, кто может
    /// помешать, гарантированно лежат в соседних ячейках.
    ///
    /// Пересобирается каждый кадр. Это дешевле, чем поддерживать её инкрементально,
    /// потому что двигаются почти все агенты сразу.
    /// </summary>
    public sealed class SpatialGrid
    {
        private readonly float _cellSize;
        private readonly Dictionary<long, List<int>> _cells = new();

        /// <summary>Пул списков, чтобы не аллоцировать при каждой пересборке.</summary>
        private readonly Stack<List<int>> _listPool = new();

        public SpatialGrid(float cellSize)
        {
            _cellSize = Mathf.Max(0.1f, cellSize);
        }

        public void Clear()
        {
            foreach (List<int> cell in _cells.Values)
            {
                cell.Clear();
                _listPool.Push(cell);
            }

            _cells.Clear();
        }

        /// <summary>Положить индекс агента в ячейку по его позиции.</summary>
        public void Insert(int index, Vector3 position)
        {
            long key = GetKey(position);

            if (!_cells.TryGetValue(key, out List<int> cell))
            {
                cell = _listPool.Count > 0 ? _listPool.Pop() : new List<int>();
                _cells[key] = cell;
            }

            cell.Add(index);
        }

        /// <summary>
        /// Собрать индексы агентов из ячейки точки и восьми соседних.
        /// Результат добавляется в переданный список — вызывающий переиспользует его.
        /// </summary>
        public void QueryNeighbours(Vector3 position, List<int> results)
        {
            int cellX = Mathf.FloorToInt(position.x / _cellSize);
            int cellZ = Mathf.FloorToInt(position.z / _cellSize);

            for (int dx = -1; dx <= 1; dx++)
            {
                for (int dz = -1; dz <= 1; dz++)
                {
                    long key = PackKey(cellX + dx, cellZ + dz);

                    if (_cells.TryGetValue(key, out List<int> cell))
                        results.AddRange(cell);
                }
            }
        }

        private long GetKey(Vector3 position)
        {
            int cellX = Mathf.FloorToInt(position.x / _cellSize);
            int cellZ = Mathf.FloorToInt(position.z / _cellSize);

            return PackKey(cellX, cellZ);
        }

        /// <summary>Две координаты ячейки в один ключ — чтобы не плодить объекты.</summary>
        private static long PackKey(int x, int z)
        {
            return ((long)x << 32) ^ (uint)z;
        }
    }
}
