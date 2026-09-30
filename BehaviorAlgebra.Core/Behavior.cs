namespace BehaviorAlgebra.Core
{
    /// <summary>
    /// Базовый класс для всех поведений системы.
    /// Поведение — это то, как система может действовать.
    /// </summary>
    public abstract class Behavior
    {
        // Аксиома: u + 0 = u. 0 — это тупик, который можно опустить.
        public static Behavior operator +(Behavior left, Behavior right)
        {
            if (left is Deadlock) return right;
            if (right is Deadlock) return left;
            return new Choice(left, right);
        }
    }
}