using System;
using System.Collections.Generic;

namespace BehaviorAlgebra.Core
{
    /// <summary>
    /// Модель порта в терминах алгебры поведения.
    /// Определяет набор действий и поведение одного автокара.
    /// </summary>
    public static class PortModel
    {
        // === ДЕЙСТВИЯ ===
        public static readonly Action LoadContainer = new("Load", new Dictionary<string, object>());
        public static readonly Action MoveToWarehouse = new("Move", new Dictionary<string, object>());
        public static readonly Action UnloadContainer = new("Unload", new Dictionary<string, object>());
        public static readonly Action CartFail = new("Fail", new Dictionary<string, object>());
        public static readonly Action CartRecover = new("Recover", new Dictionary<string, object>());
        public static readonly Action CommLost = new("CommLost", new Dictionary<string, object>());
        public static readonly Action CommRestored = new("CommRestored", new Dictionary<string, object>());

        /// <summary>
        /// Поведение одного автокара.
        /// Система выбирает одну из четырёх стратегий:
        ///   1. Успешная доставка: Load → Move → Unload → Δ
        ///   2. Сбой и восстановление: Load → Move → Fail → Recover → Move → Unload → Δ
        ///   3. Потеря связи и восстановление: Load → CommLost → CommRestored → Move → Unload → Δ
        ///   4. Полный отказ: Load → Move → Fail → 0
        /// </summary>
        public static Behavior BuildCartBehavior()
        {
            // 1. Успешная доставка
            var success =
                new Prefix(LoadContainer,
                    new Prefix(MoveToWarehouse,
                        new Prefix(UnloadContainer,
                            Success.Instance)));

            // 2. Сбой и восстановление
            var failAndRecover =
                new Prefix(LoadContainer,
                    new Prefix(MoveToWarehouse,
                        new Prefix(CartFail,
                            new Prefix(CartRecover,
                                new Prefix(MoveToWarehouse,
                                    new Prefix(UnloadContainer,
                                        Success.Instance))))));

            // 3. Потеря связи и восстановление
            var commLost =
                new Prefix(LoadContainer,
                    new Prefix(CommLost,
                        new Prefix(CommRestored,
                            new Prefix(MoveToWarehouse,
                                new Prefix(UnloadContainer,
                                    Success.Instance)))));

            // 4. Полный отказ (тупик)
            var totalFailure =
                new Prefix(LoadContainer,
                    new Prefix(MoveToWarehouse,
                        new Prefix(CartFail,
                            Deadlock.Instance)));

            // Композиция: система выбирает любой из четырёх сценариев.
            return success + failAndRecover + commLost + totalFailure;
        }
    }
}

