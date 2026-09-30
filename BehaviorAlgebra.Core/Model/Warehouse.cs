namespace BehaviorAlgebra.Core.Model
{
    /// <summary>
    /// Склад — место выгрузки контейнеров.
    /// Имеет ограниченную вместимость и отслеживает текущую загрузку.
    /// </summary>
    public sealed class Warehouse
    {
        /// <summary>Уникальный номер склада.</summary>
        public int Id { get; set; }

        /// <summary>Имя склада (используется как «адрес» в контейнере).</summary>
        public string Name { get; set; }

        /// <summary>Максимальная вместимость (в кг).</summary>
        public int Capacity { get; set; }

        /// <summary>Текущая загрузка (сумма весов доставленных контейнеров).</summary>
        public int CurrentLoad { get; set; }

        public Warehouse(int id, string name, int capacity)
        {
            Id = id;
            Name = name;
            Capacity = capacity;
            CurrentLoad = 0;
        }

        /// <summary>Сколько ещё можно принять (кг).</summary>
        public int FreeSpace => Capacity - CurrentLoad;

        public override string ToString() => $"{Name} ({CurrentLoad}/{Capacity})";
    }
}