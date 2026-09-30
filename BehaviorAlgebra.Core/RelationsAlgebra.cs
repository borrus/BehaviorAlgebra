using System.Collections.Generic;
using System.Linq;
using BehaviorAlgebra.Core.Model;

namespace BehaviorAlgebra.Core.Algebra
{
    /// <summary>
    /// Алгебра динамических отношений порта.
    ///
    /// Ключевая идея:
    ///   1. Описываем порт через три отношения — Capacity, Destination, Active.
    ///   2. Применяем к ним операции ∪, ∩, ∘.
    ///   3. Получаем план доставки как композицию Capacity ∘ Destination.
    ///
    /// Схема построения плана:
    ///   Active ∩ Capacity  → пары (свободный автокар, ждущий контейнер), подходящие по весу
    ///   затем ∘ Destination → маршруты до склада
    /// </summary>
    public static class RelationsAlgebra
    {
        /// <summary>
        /// Отношение Capacity ⊆ Cart × Container.
        /// Содержит пары (автокар, контейнер), где capacity ≥ weight.
        /// </summary>
        public static Relation<Cart, Container> BuildCapacityRelation(Port port)
        {
            var rel = new Relation<Cart, Container>();
            foreach (var cart in port.Carts)
                foreach (var cont in port.Containers)
                    if (cart.Capacity >= cont.Weight)
                        rel.Add(cart, cont);
            return rel;
        }

        /// <summary>
        /// Отношение Destination ⊆ Container × Warehouse.
        /// Содержит пары (контейнер, склад), куда этот контейнер должен ехать.
        /// </summary>
        public static Relation<Container, Warehouse> BuildDestinationRelation(Port port)
        {
            var rel = new Relation<Container, Warehouse>();
            foreach (var cont in port.Containers)
            {
                var wh = port.Warehouses.FirstOrDefault(w => w.Name == cont.Destination);
                if (wh != null)
                    rel.Add(cont, wh);
            }
            return rel;
        }

        /// <summary>
        /// КОМПОЗИЦИЯ (∘): Capacity ∘ Destination ⊆ Cart × Warehouse.
        /// Показывает, какой автокар может доставить груз на какой склад.
        /// </summary>
        public static Relation<Cart, Warehouse> ComposeRoutes(Port port)
        {
            var cap = BuildCapacityRelation(port);
            var dest = BuildDestinationRelation(port);
            return cap.Compose(dest);
        }

        /// <summary>
        /// Построение плана доставки.
        ///
        /// Шаги:
        ///   1. Строим отношение Active = (Free Carts) × (Waiting Containers).
        ///   2. Пересечение: Active ∩ Capacity — пары, где автокар свободен И подходит по весу.
        ///   3. Сортируем по приоритету контейнера (1 — важнее).
        ///   4. Для каждой пары строим маршрут (композиция ∘ с Destination).
        ///
        /// Возвращает список новых маршрутов; сами маршруты также добавляются в port.Routes.
        /// </summary>
        public static List<Route> BuildPlan(Port port)
        {
            var cap = BuildCapacityRelation(port);

            // Отношение Active: свободные автокары × ждущие контейнеры.
            var active = new Relation<Cart, Container>();
            foreach (var cart in port.Carts.Where(c => c.Status == CartStatus.Free))
                foreach (var cont in port.Containers.Where(c => c.Status == ContainerStatus.Waiting))
                    active.Add(cart, cont);

            // Пересечение ∩ — только подходящие по грузоподъёмности.
            var pairs = active.Intersect(cap);

            var routes = new List<Route>();
            var usedCarts = new HashSet<int>();
            var usedContainers = new HashSet<int>();

            // Сортировка: сначала контейнеры с высоким приоритетом, потом — тяжёлые.
            var sorted = pairs.Tuples
                .OrderBy(t => t.Item2.Priority)
                .ThenByDescending(t => t.Item2.Weight)
                .ThenBy(t => t.Item1.Id)
                .ToList();

            foreach (var (cart, container) in sorted)
            {
                // Один автокар и один контейнер могут участвовать только в одном маршруте.
                if (usedCarts.Contains(cart.Id)) continue;
                if (usedContainers.Contains(container.Id)) continue;

                var warehouse = port.Warehouses.FirstOrDefault(w => w.Name == container.Destination);
                if (warehouse == null) continue;

                // Нельзя перегружать склад.
                if (warehouse.CurrentLoad + container.Weight > warehouse.Capacity) continue;

                var route = new Route(cart.Id, container.Id, warehouse.Id);
                route.StartedAt = port.Time;
                route.Status = RouteStatus.InTransit;

                routes.Add(route);
                usedCarts.Add(cart.Id);
                usedContainers.Add(container.Id);

                cart.Status = CartStatus.Busy;
                cart.StatusChangedAt = port.Time;
                container.Status = ContainerStatus.InTransit;
                warehouse.CurrentLoad += container.Weight;
                port.Routes.Add(route);
            }

            return routes;
        }
    }
}