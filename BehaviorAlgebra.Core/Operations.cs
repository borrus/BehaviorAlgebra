using System;

namespace BehaviorAlgebra.Core
{
    /// <summary>Префиксинг (a.u): сначала действие, потом поведение.</summary>
    public sealed class Prefix : Behavior
    {
        public Action Action { get; }
        public Behavior Next { get; }

        public Prefix(Action action, Behavior next)
        {
            Action = action ?? throw new ArgumentNullException(nameof(action));
            Next = next ?? throw new ArgumentNullException(nameof(next));
        }

        public override string ToString() => $"{Action}.{Next}";
    }

    /// <summary>Недетерминированный выбор (u + v): либо u, либо v.</summary>
    public sealed class Choice : Behavior
    {
        public Behavior Left { get; }
        public Behavior Right { get; }

        public Choice(Behavior left, Behavior right)
        {
            Left = left ?? throw new ArgumentNullException(nameof(left));
            Right = right ?? throw new ArgumentNullException(nameof(right));
        }

        public override string ToString() => $"({Left} + {Right})";
    }
}