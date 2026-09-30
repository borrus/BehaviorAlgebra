using System;
using System.Collections.Generic;
using System.Linq;

namespace BehaviorAlgebra.Core
{
    /// <summary>
    /// Верификатор свойств поведения на основе построенного LTS.
    /// Проверяет достижимость успеха, наличие тупиков и живость.
    /// </summary>
    public sealed class Verifier
    {
        private readonly LabeledTransitionSystem _lts;

        public Verifier(LabeledTransitionSystem lts)
        {
            _lts = lts ?? throw new ArgumentNullException(nameof(lts));
        }

        /// <summary>
        /// Достижимо ли состояние Δ из начального?
        /// Обход в ширину.
        /// </summary>
        public bool IsSuccessReachable()
        {
            return ReachableStates().Contains(Success.Instance);
        }

        /// <summary>
        /// Достижим ли тупик 0 из начального?
        /// </summary>
        public bool IsDeadlockReachable()
        {
            return ReachableStates().Contains(Deadlock.Instance);
        }

        /// <summary>
        /// Все ли пути рано или поздно ведут к Δ?
        /// (Проверка живости: нет состояния, из которого нельзя добраться до Δ.)
        /// </summary>
        public bool IsLive()
        {
            // Шаг 1: находим все состояния, из которых Δ достижимо.
            var canReachSuccess = new HashSet<Behavior>();
            var queue = new Queue<Behavior>();

            // Начинаем с обратного обхода: находим всё, что ведёт в Δ.
            // Сначала соберём обратный граф.
            var reverseEdges = new Dictionary<Behavior, List<Behavior>>();
            foreach (var state in _lts.States)
            {
                foreach (var t in _lts.GetTransitions(state))
                {
                    if (!reverseEdges.TryGetValue(t.To, out var preds))
                    {
                        preds = new List<Behavior>();
                        reverseEdges[t.To] = preds;
                    }
                    preds.Add(t.From);
                }
            }

            // Стартуем с Δ и идём назад.
            if (_lts.States.Contains(Success.Instance))
            {
                queue.Enqueue(Success.Instance);
                canReachSuccess.Add(Success.Instance);
            }

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (!reverseEdges.TryGetValue(current, out var preds)) continue;

                foreach (var pred in preds)
                {
                    if (canReachSuccess.Add(pred))
                        queue.Enqueue(pred);
                }
            }

            // Шаг 2: живое, если из КАЖДОГО состояния можно добраться до Δ.
            return _lts.States.All(s => canReachSuccess.Contains(s));
        }

        /// <summary>
        /// Возвращает все тупиковые состояния LTS.
        /// Тупик — состояние без исходящих переходов, и это не Δ.
        /// </summary>
        public IReadOnlyList<Behavior> FindDeadlockStates()
        {
            return _lts.States
                .Where(s => _lts.GetTransitions(s).Count == 0 && !(s is Success))
                .ToList();
        }

        /// <summary>
        /// Возвращает кратчайший путь от начального состояния до Δ.
        /// Если пути нет — null.
        /// </summary>
        public IReadOnlyList<Transition>? FindShortestPathToSuccess()
        {
            var visited = new HashSet<Behavior> { _lts.InitialState };
            var parent = new Dictionary<Behavior, Transition>();
            var queue = new Queue<Behavior>();
            queue.Enqueue(_lts.InitialState);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                if (current is Success)
                {
                    // Восстанавливаем путь.
                    var path = new List<Transition>();
                    var node = current;
                    while (parent.ContainsKey(node))
                    {
                        var t = parent[node];
                        path.Insert(0, t);
                        node = t.From;
                    }
                    return path;
                }

                foreach (var t in _lts.GetTransitions(current))
                {
                    if (visited.Add(t.To))
                    {
                        parent[t.To] = t;
                        queue.Enqueue(t.To);
                    }
                }
            }

            return null;
        }

        // Вспомогательный метод: все достижимые состояния.
        private HashSet<Behavior> ReachableStates()
        {
            return new HashSet<Behavior>(_lts.States);
        }
    }
}