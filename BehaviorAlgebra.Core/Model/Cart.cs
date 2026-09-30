namespace BehaviorAlgebra.Core.Model
{
    /// <summary>
    /// Автокар — транспортное средство для перевозки контейнеров в порту.
    /// Каждый автокар имеет ограничение по грузоподъёмности и текущий статус.
    /// </summary>
    public sealed class Cart
    {
        /// <summary>Уникальный номер автокара.</summary>
        public int Id { get; set; }

        /// <summary>Максимальный вес груза, который автокар может везти.</summary>
        public int Capacity { get; set; }

        /// <summary>Текущее местоположение (причал).</summary>
        public string Location { get; set; }

        /// <summary>Текущий статус: Free / Busy / Broken / Delayed.</summary>
        public CartStatus Status { get; set; }

        /// <summary>
        /// Время (в шагах симуляции), когда статус последний раз менялся.
        /// Используется для отсчёта таймеров ремонта и восстановления связи.
        /// </summary>
        public int StatusChangedAt { get; set; }

        public Cart(int id, int capacity, string location)
        {
            Id = id;
            Capacity = capacity;
            Location = location;
            Status = CartStatus.Free;
            StatusChangedAt = 0;
        }

        public override string ToString() => $"Cart #{Id} ({Status})";
    }

    /// <summary>
    /// Статус автокара.
    /// Free     — свободен, готов взять контейнер.
    /// Busy     — везёт контейнер.
    /// Broken   — сломан, идёт ремонт (5 шагов).
    /// Delayed  — потеря связи, восстановление автоматическое (3 шага).
    /// </summary>
    public enum CartStatus { Free, Busy, Broken, Delayed }
}