using System.Collections.Generic;
using System.Linq;

namespace BehaviorAlgebra.Core.Algebra
{
    /// <summary>
    /// Математическое отношение — множество кортежей (A, B).
    ///
    /// Это базовая структура алгебры динамических отношений.
    /// Над отношениями определены три операции:
    ///   ∪ — объединение,
    ///   ∩ — пересечение,
    ///   ∘ — композиция.
    ///
    /// Пример: отношение Capacity ⊆ Cart × Container
    /// содержит пары (cart, container), где автокар может везти контейнер.
    /// </summary>
    public sealed class Relation<T1, T2> where T2 : notnull
    {
        private readonly HashSet<(T1, T2)> _tuples;

        public Relation()
        {
            _tuples = new HashSet<(T1, T2)>();
        }

        public Relation(IEnumerable<(T1, T2)> tuples)
        {
            _tuples = new HashSet<(T1, T2)>(tuples);
        }

        /// <summary>Все кортежи отношения.</summary>
        public IReadOnlyCollection<(T1, T2)> Tuples => _tuples;

        /// <summary>Мощность отношения — сколько кортежей.</summary>
        public int Count => _tuples.Count;

        /// <summary>Добавить кортеж (a, b) в отношение.</summary>
        public void Add(T1 a, T2 b) => _tuples.Add((a, b));

        /// <summary>Проверить, содержится ли кортеж (a, b) в отношении.</summary>
        public bool Contains(T1 a, T2 b) => _tuples.Contains((a, b));

        /// <summary>
        /// ОПЕРАЦИЯ 1: ОБЪЕДИНЕНИЕ (∪).
        /// R ∪ S = { x | x ∈ R  или  x ∈ S }.
        /// Возвращает новое отношение, содержащее кортежи из обоих.
        /// </summary>
        public Relation<T1, T2> Union(Relation<T1, T2> other)
        {
            var result = new HashSet<(T1, T2)>(_tuples);
            result.UnionWith(other._tuples);
            return new Relation<T1, T2>(result);
        }

        /// <summary>
        /// ОПЕРАЦИЯ 2: ПЕРЕСЕЧЕНИЕ (∩).
        /// R ∩ S = { x | x ∈ R  и  x ∈ S }.
        /// Возвращает новое отношение, содержащее только общие кортежи.
        /// </summary>
        public Relation<T1, T2> Intersect(Relation<T1, T2> other)
        {
            var result = new HashSet<(T1, T2)>(_tuples);
            result.IntersectWith(other._tuples);
            return new Relation<T1, T2>(result);
        }

        /// <summary>
        /// ОПЕРАЦИЯ 3: КОМПОЗИЦИЯ (∘).
        /// R ∘ S = { (a, c) | ∃b: (a, b) ∈ R  и  (b, c) ∈ S }.
        ///
        /// Пример: Capacity ⊆ Cart × Container, Destination ⊆ Container × Warehouse.
        /// Capacity ∘ Destination ⊆ Cart × Warehouse — кто может доставить куда.
        /// </summary>
        public Relation<T1, T3> Compose<T3>(Relation<T2, T3> other) where T3 : notnull
        {
            var result = new HashSet<(T1, T3)>();

            // Индексируем правое отношение по первому элементу,
            // чтобы композиция работала за O(|R| + |S|), а не за O(|R| * |S|).
            var rightIndex = other._tuples
                .GroupBy(t => t.Item1)
                .ToDictionary(g => g.Key, g => g.Select(t => t.Item2).ToList());

            foreach (var (a, b) in _tuples)
            {
                if (rightIndex.TryGetValue(b, out var cs))
                {
                    foreach (var c in cs)
                        result.Add((a, c));
                }
            }

            return new Relation<T1, T3>(result);
        }

        public override string ToString() =>
            "{" + string.Join(", ", _tuples.Select(t => $"({t.Item1},{t.Item2})")) + "}";
    }
}