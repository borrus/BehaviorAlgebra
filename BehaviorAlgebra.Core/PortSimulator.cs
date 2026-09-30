using System.Collections.Generic;
using System.Linq;
using BehaviorAlgebra.Core.Algebra;
using BehaviorAlgebra.Core.Model;

namespace BehaviorAlgebra.Core.Simulation
{
    /// <summary>
    /// Симулятор порта — пошаговое продвижение времени и реакция на сбои.
    ///
    /// Таймеры (в шагах):
    ///   • Восстановление связи — 3 шага.
    ///   • Ремонт сломанного автокара — 5 шагов.
    ///   • Доставка контейнера — 2 шага.
    /// </summary>
    public static class PortSimulator
    {
        /// <summary>
        /// Один шаг симуляции. Продвигает время и обрабатывает все события:
        /// восстановление связи, ремонт, доставку и автоматическое перепланирование.
        /// </summary>
        public static void Step(Port port)
        {
            port.Time++;
            port.Events.Clear();

            bool replanNeeded = false;

            // 1. Восстановление связи (была потеряна — ждём 3 шага).
            foreach (var cart in port.Carts.Where(c => c.Status == CartStatus.Delayed).ToList())
            {
                int elapsed = port.Time - cart.StatusChangedAt;
                if (elapsed >= 3)
                {
                    cart.Status = CartStatus.Free;
                    cart.StatusChangedAt = port.Time;
                    port.Events.Add($"✔ СВЯЗЬ восстановлена: автокар #{cart.Id} → Free (t={port.Time})");
                    replanNeeded = true;
                }
                else
                {
                    port.Events.Add($"📡 Автокар #{cart.Id}: без связи, восстановление через {3 - elapsed} шаг(ов)");
                }
            }

            // 2. Ремонт сломанного автокара (5 шагов).
            foreach (var cart in port.Carts.Where(c => c.Status == CartStatus.Broken).ToList())
            {
                int elapsed = port.Time - cart.StatusChangedAt;
                if (elapsed >= 5)
                {
                    cart.Status = CartStatus.Free;
                    cart.StatusChangedAt = port.Time;
                    port.Events.Add($"🔧 РЕМОНТ завершён: автокар #{cart.Id} → Free (t={port.Time})");
                    replanNeeded = true;
                }
                else
                {
                    port.Events.Add($"⏳ Автокар #{cart.Id}: в ремонте, осталось {5 - elapsed} шаг(ов)");
                }
            }

            // 3. Завершение маршрутов «в пути» (2 шага от старта).
            foreach (var route in port.Routes.Where(r => r.Status == RouteStatus.InTransit).ToList())
            {
                if (port.Time - route.StartedAt >= 2)
                {
                    route.Status = RouteStatus.Completed;

                    var cart = port.Carts.FirstOrDefault(c => c.Id == route.CartId);
                    if (cart != null && cart.Status == CartStatus.Busy)
                    {
                        cart.Status = CartStatus.Free;
                        cart.StatusChangedAt = port.Time;
                        replanNeeded = true;
                    }

                    var cont = port.Containers.FirstOrDefault(c => c.Id == route.ContainerId);
                    if (cont != null)
                    {
                        cont.Status = ContainerStatus.Delivered;
                        port.Events.Add($"📦 ДОСТАВЛЕН: контейнер #{cont.Id} → {cont.Destination} (автокар #{cart?.Id}, t={port.Time})");
                    }
                }
            }

            // 4. Автоматическое перепланирование после освобождения ресурсов.
            if (replanNeeded)
            {
                var newRoutes = RelationsAlgebra.BuildPlan(port);
                if (newRoutes.Count > 0)
                {
                    port.Events.Add($"🔄 АВТОПЛАН: {newRoutes.Count} новых маршрутов");
                    foreach (var r in newRoutes)
                        port.Events.Add($"   → автокар #{r.CartId} получил контейнер #{r.ContainerId}");
                }
            }
        }

        /// <summary>Построить план вручную (по кнопке).</summary>
        public static List<Route> BuildPlan(Port port)
        {
            port.Events.Clear();
            var routes = RelationsAlgebra.BuildPlan(port);
            port.Events.Add($"📋 Построено маршрутов: {routes.Count}");
            return routes;
        }

        /// <summary>
        /// Поломка автокара: статус → Broken, его маршруты отменяются,
        /// контейнеры возвращаются в очередь ожидания, склады освобождаются.
        /// Ремонт через 5 шагов (обрабатывается в Step).
        /// </summary>
        public static void BreakCart(Port port, int cartId)
        {
            var cart = port.Carts.FirstOrDefault(c => c.Id == cartId);
            if (cart == null) return;

            cart.Status = CartStatus.Broken;
            cart.StatusChangedAt = port.Time;
            port.Events.Add($"💥 СБОЙ: автокар #{cart.Id} сломался (ремонт 5 шагов)");

            foreach (var route in port.Routes.Where(r => r.CartId == cartId &&
                          (r.Status == RouteStatus.InTransit || r.Status == RouteStatus.Planned)).ToList())
            {
                route.Status = RouteStatus.Cancelled;
                port.Events.Add($"   ✖ Маршрут (cart={route.CartId}, cont={route.ContainerId}) отменён");

                var cont = port.Containers.FirstOrDefault(c => c.Id == route.ContainerId);
                if (cont != null && cont.Status == ContainerStatus.InTransit)
                {
                    cont.Status = ContainerStatus.Waiting;
                    port.Events.Add($"   ↩ Контейнер #{cont.Id} снова ждёт");
                }

                var wh = port.Warehouses.FirstOrDefault(w => w.Id == route.WarehouseId);
                if (wh != null && cont != null) wh.CurrentLoad -= cont.Weight;
            }
        }

        /// <summary>
        /// Потеря связи с автокаром: статус → Delayed,
        /// маршруты помечаются задержанными, контейнеры возвращаются в очередь.
        /// Связь восстанавливается через 3 шага (обрабатывается в Step).
        /// </summary>
        public static void LoseCommunication(Port port, int cartId)
        {
            var cart = port.Carts.FirstOrDefault(c => c.Id == cartId);
            if (cart == null) return;

            cart.Status = CartStatus.Delayed;
            cart.StatusChangedAt = port.Time;
            port.Events.Add($"📡 СБОЙ: потеря связи с автокаром #{cart.Id} (восстановление 3 шага)");

            foreach (var route in port.Routes.Where(r => r.CartId == cartId &&
                          (r.Status == RouteStatus.InTransit || r.Status == RouteStatus.Planned)).ToList())
            {
                route.Status = RouteStatus.Delayed;
                port.Events.Add($"   ⏸ Маршрут (cart={route.CartId}, cont={route.ContainerId}) задержан");

                var cont = port.Containers.FirstOrDefault(c => c.Id == route.ContainerId);
                if (cont != null && cont.Status == ContainerStatus.InTransit)
                {
                    cont.Status = ContainerStatus.Waiting;
                    port.Events.Add($"   ↩ Контейнер #{cont.Id} снова ждёт");
                }

                var wh = port.Warehouses.FirstOrDefault(w => w.Id == route.WarehouseId);
                if (wh != null && cont != null) wh.CurrentLoad -= cont.Weight;
            }
        }

        /// <summary>
        /// Перепланирование: удаляем отменённые и задержанные маршруты,
        /// строим план заново.
        /// </summary>
        public static List<Route> Replan(Port port)
        {
            port.Events.Clear();
            port.Routes.RemoveAll(r =>
                r.Status == RouteStatus.Cancelled || r.Status == RouteStatus.Delayed);

            var routes = BuildPlan(port);
            port.Events.Add($"🔄 Перепланировано: {routes.Count} маршрутов");
            return routes;
        }

        /// <summary>Сброс порта в начальное состояние.</summary>
        public static void Reset(Port port) => port.Reset();
    }
}