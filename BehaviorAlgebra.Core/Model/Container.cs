namespace BehaviorAlgebra.Core.Model
{
    /// <summary>
    /// Контейнер — груз, который нужно доставить на склад.
    /// Имеет вес, приоритет и склад назначения.
    /// </summary>
    public sealed class Container
    {
        /// <summary>Уникальный номер контейнера.</summary>
        public int Id { get; set; }

        /// <summary>Вес контейнера (кг).</summary>
        public int Weight { get; set; }

        /// <summary>Имя склада, куда нужно доставить контейнер.</summary>
        public string Destination { get; set; }

        /// <summary>
        /// Приоритет: 1 — высокий, 2 — средний, 3 — низкий.
        /// Чем меньше число, тем раньше контейнер отправляется.
        /// </summary>
        public int Priority { get; set; }

        /// <summary>Текущий статус: Waiting / InTransit / Delivered.</summary>
        public ContainerStatus Status { get; set; }

        public Container(int id, int weight, string destination, int priority)
        {
            Id = id;
            Weight = weight;
            Destination = destination;
            Priority = priority;
            Status = ContainerStatus.Waiting;
        }

        public override string ToString() => $"Container #{Id} ({Weight}kg)";
    }

    /// <summary>
    /// Статус контейнера.
    /// Waiting    — ждёт отправки.
    /// InTransit  — едет на автокаре.
    /// Delivered  — доставлен на склад.
    /// </summary>
    public enum ContainerStatus { Waiting, InTransit, Delivered }
}