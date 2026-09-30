namespace BehaviorAlgebra.Core
{
    /// <summary>Δ — успешное завершение поведения.</summary>
    public sealed class Success : Behavior
    {
        public static readonly Success Instance = new();
        private Success() { }
        public override string ToString() => "Δ";
    }

    /// <summary>0 — тупик, система остановилась.</summary>
    public sealed class Deadlock : Behavior
    {
        public static readonly Deadlock Instance = new();
        private Deadlock() { }
        public override string ToString() => "0";
    }

    /// <summary>⊥ — неопределённость.</summary>
    public sealed class Undefined : Behavior
    {
        public static readonly Undefined Instance = new();
        private Undefined() { }
        public override string ToString() => "⊥";
    }
}