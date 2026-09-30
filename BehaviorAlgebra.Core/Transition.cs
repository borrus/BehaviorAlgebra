using System;

namespace BehaviorAlgebra.Core
{
    /// <summary>
    /// Один переход в системе: из состояния From по действию Action в состояние To.
    /// </summary>
    public sealed class Transition : IEquatable<Transition>
    {
        public Behavior From { get; }
        public Action Action { get; }
        public Behavior To { get; }

        public Transition(Behavior from, Action action, Behavior to)
        {
            From = from ?? throw new ArgumentNullException(nameof(from));
            Action = action ?? throw new ArgumentNullException(nameof(action));
            To = to ?? throw new ArgumentNullException(nameof(to));
        }

        public bool Equals(Transition? other)
        {
            if (other is null) return false;
            if (ReferenceEquals(this, other)) return true;
            return ReferenceEquals(From, other.From)
                && Action.Equals(other.Action)
                && ReferenceEquals(To, other.To);
        }

        public override bool Equals(object? obj) => Equals(obj as Transition);
        public override int GetHashCode() => HashCode.Combine(From, Action, To);
        public override string ToString() => $"{From} --{Action}--> {To}";
    }
}