using System;
using System.Linq;
using BehaviorAlgebra.Core;

namespace BehaviorAlgebra.ConsoleTest
{
    internal class Program
    {
        static void Main()
        {
            Console.OutputEncoding = System.Text.Encoding.UTF8;

            Console.WriteLine("=== Алгебра поведения: верификация LTS ===");
            Console.WriteLine();

            // 1. Строим LTS
            var cartBehavior = PortModel.BuildCartBehavior();
            var lts = new LabeledTransitionSystem(cartBehavior);

            Console.WriteLine($"Состояний:  {lts.StateCount}");
            Console.WriteLine($"Переходов:  {lts.TransitionCount}");
            Console.WriteLine();

            // 2. Верификация
            var verifier = new Verifier(lts);

            Console.WriteLine("=== Проверка свойств ===");
            Console.WriteLine($"Δ достижим:          {verifier.IsSuccessReachable()}");
            Console.WriteLine($"0 (тупик) достижим:  {verifier.IsDeadlockReachable()}");
            Console.WriteLine($"Система жива:        {verifier.IsLive()}");
            Console.WriteLine();

            // 3. Тупиковые состояния
            var deadlocks = verifier.FindDeadlockStates();
            Console.WriteLine($"Тупиковых состояний: {deadlocks.Count}");
            foreach (var d in deadlocks)
                Console.WriteLine($"  - {Shorten(d)}");
            Console.WriteLine();

            // 4. Кратчайший путь к успеху
            var path = verifier.FindShortestPathToSuccess();
            if (path != null)
            {
                Console.WriteLine($"Кратчайший путь к Δ ({path.Count} шагов):");
                foreach (var t in path)
                    Console.WriteLine($"  --{t.Action}-->");
            }
            else
            {
                Console.WriteLine("Путь к Δ не найден.");
            }

            Console.WriteLine();
            Console.WriteLine("Нажмите любую клавишу...");
            Console.ReadKey();
        }

        private static string Shorten(Behavior b)
        {
            var s = b.ToString();
            return s.Length > 50 ? s.Substring(0, 47) + "..." : s;
        }
    }
}