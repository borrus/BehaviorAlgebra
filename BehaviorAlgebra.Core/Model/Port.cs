using System.Collections.Generic;
using System.Linq;

namespace BehaviorAlgebra.Core.Model
{
    /// <summary>
    /// Состояние порта — центральная структура данных.
    /// Содержит все автокары, контейнеры, склады и построенные маршруты.
    /// Также хранит виртуальное время симуляции и очередь событий для лога.
    /// </summary>
    public sealed class Port
    {
        /// <summary>Список всех автокаров порта.</summary>
        public List<Cart> Carts { get; } = new();

        /// <summary>Список всех контейнеров.</summary>
        public List<Container> Containers { get; } = new();

        /// <summary>Список всех складов.</summary>
        public List<Warehouse> Warehouses { get; } = new();

        /// <summary>История построенных маршрутов (включая отменённые).</summary>
        public List<Route> Routes { get; } = new();

        /// <summary>Виртуальное время симуляции (в «шагах»).</summary>
        public int Time { get; set; } = 0;

        /// <summary>
        /// Очередь событий текущего шага.
        /// Заполняется симулятором, выводится в лог интерфейса и очищается.
        /// </summary>
        public List<string> Events { get; } = new();

        /// <summary>
        /// Создаёт порт с тестовыми данными:
        /// 4 автокара разной грузоподъёмности, 5 контейнеров, 3 склада.
        /// </summary>
        public static Port CreateDefault()
        {
            var p = new Port();

            // Автокары: capacity — максимальный вес груза
            p.Carts.Add(new Cart(1, 100, "Pier A"));
            p.Carts.Add(new Cart(2, 50, "Pier B"));
            p.Carts.Add(new Cart(3, 200, "Pier C"));
            p.Carts.Add(new Cart(4, 80, "Pier A"));

            // Контейнеры: id, weight, destination, priority (1 — важнее всех)
            p.Containers.Add(new Container(101, 80, "WH-Alpha", 1));
            p.Containers.Add(new Container(102, 40, "WH-Beta", 2));
            p.Containers.Add(new Container(103, 150, "WH-Alpha", 1));
            p.Containers.Add(new Container(104, 30, "WH-Gamma", 3));
            p.Containers.Add(new Container(105, 90, "WH-Beta", 2));

            // Склады: id, name, capacity
            p.Warehouses.Add(new Warehouse(1, "WH-Alpha", 500));
            p.Warehouses.Add(new Warehouse(2, "WH-Beta", 300));
            p.Warehouses.Add(new Warehouse(3, "WH-Gamma", 200));

            return p;
        }

        /// <summary>
        /// Сбрасывает порт в исходное состояние:
        /// все автокары свободны, контейнеры ждут, склады пусты, маршруты удалены.
        /// </summary>
        public void Reset()
        {
            Time = 0;
            Events.Clear();
            foreach (var c in Carts) { c.Status = CartStatus.Free; c.StatusChangedAt = 0; }
            foreach (var c in Containers) c.Status = ContainerStatus.Waiting;
            foreach (var w in Warehouses) w.CurrentLoad = 0;
            Routes.Clear();
        }
    }
}