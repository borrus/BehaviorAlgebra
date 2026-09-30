using System;
using System.Collections.Generic;
using System.Linq;

namespace BehaviorAlgebra.Core
{
    /// <summary>
    /// Размеченная система переходов (Labeled Transition System).
    /// Граф, где узлы — поведения, рёбра — переходы по действиям.
    /// Строится обходом в ширину от начального состояния.
    /// </summary>
    public sealed class LabeledTransitionSystem
    {
        private readonly Dictionary<Behavior, List<Transition>> _transitions = new();
        private readonly HashSet<Behavior> _states = new();

        /// <summary>Начальное состояние системы.</summary>
        public Behavior InitialState { get; }

        /// <summary>Все достижимые состояния.</summary>
        public IReadOnlyCollection<Behavior> States => _states;

        /// <summary>Количество состояний.</summary>
        public int StateCount => _states.Count;

        /// <summary>Количество переходов.</summary>
        public int TransitionCount => _transitions.Values.Sum(l => l.Count);

        public LabeledTransitionSystem(Behavior initialState)
        {
            InitialState = initialState ?? throw new ArgumentNullException(nameof(initialState));
            Build();
        }

        /// <summary>Возвращает все переходы из данного состояния.</summary>
        public IReadOnlyList<Transition> GetTransitions(Behavior state)
        {
            return _transitions.TryGetValue(state, out var list)
                ? list
                : (IReadOnlyList<Transition>)Array.Empty<Transition>();
        }

        // Обход в ширину: строим граф из начального состояния.
        private void Build()
        {
            var queue = new Queue<Behavior>();
            queue.Enqueue(InitialState);
            _states.Add(InitialState);

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                var outgoing = Step(current);
                _transitions[current] = outgoing;

                foreach (var t in outgoing)
                {
                    if (_states.Add(t.To))
                        queue.Enqueue(t.To);
                }
            }
        }

        /// <summary>
        /// Один шаг операционной семантики (SOS-правила):
        ///   Prefix(a, u)  --a-->  u
        ///   Choice(u, v)  --a-->  u'   если  u --a--> u'
        ///   Choice(u, v)  --a-->  v'   если  v --a--> v'
        /// Константы Δ, 0, ⊥ — конечные состояния, переходов не имеют.
        /// </summary>
        public static List<Transition> Step(Behavior behavior)
        {
            var result = new List<Transition>();

            switch (behavior)
            {
                case Prefix p:
                    result.Add(new Transition(p, p.Action, p.Next));
                    break;

                case Choice c:
                    foreach (var t in Step(c.Left))
                        result.Add(new Transition(c, t.Action, t.To));
                    foreach (var t in Step(c.Right))
                        result.Add(new Transition(c, t.Action, t.To));
                    break;

                    // Success, Deadlock, Undefined — конечные состояния.
            }

            return result;
        }
    }
}