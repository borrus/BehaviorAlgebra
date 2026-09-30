namespace BehaviorAlgebra.Core.Model
{
    /// <summary>
    /// Маршрут — результат операции композиции (∘):
    /// связка «Автокар → Контейнер → Склад».
    /// </summary>
    public sealed class Route
    {
        /// <summary>ID автокара, назначенного на маршрут.</summary>
        public int CartId { get; set; }

        /// <summary>ID перевозимого контейнера.</summary>
        public int ContainerId { get; set; }

        /// <summary>ID склада назначения.</summary>
        public int WarehouseId { get; set; }

        /// <summary>Статус маршрута.</summary>
        public RouteStatus Status { get; set; }

        /// <summary>Время (в шагах), когда маршрут был начат.</summary>
        public int StartedAt { get; set; }

        public Route(int cartId, int containerId, int warehouseId)
        {
            CartId = cartId;
            ContainerId = containerId;
            WarehouseId = warehouseId;
            Status = RouteStatus.Planned;
            StartedAt = 0;
        }

        public override string ToString() => $"Route(cart={CartId}, cont={ContainerId})";
    }

    /// <summary>
    /// Статус маршрута.
    /// Planned    — запланирован, но ещё не отправлен.
    /// InTransit  — выполняется (автокар везёт контейнер).
    /// Completed  — доставка завершена.
    /// Cancelled  — отменён из-за поломки автокара.
    /// Delayed    — задержан из-за потери связи.
    /// </summary>
    public enum RouteStatus { Planned, InTransit, Completed, Cancelled, Delayed }
}