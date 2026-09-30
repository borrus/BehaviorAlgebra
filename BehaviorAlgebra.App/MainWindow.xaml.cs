using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Shapes;
using BehaviorAlgebra.Core;
using BehaviorAlgebra.Core.Algebra;
using BehaviorAlgebra.Core.Model;
using BehaviorAlgebra.Core.Simulation;

namespace BehaviorAlgebra.App
{
    /// <summary>
    /// Главное окно приложения.
    /// Содержит три вкладки:
    ///   1. «Порт» — симуляция порта (автокары, контейнеры, маршруты).
    ///   2. «Отношения (алгебра)» — таблицы ∪, ∩, ∘ над отношениями.
    ///   3. «LTS (верификация)» — граф поведения и model checking.
    /// </summary>
    public partial class MainWindow : Window
    {
        // ============================================================
        // Поля для LTS-вкладки (бонус: формальная верификация)
        // ============================================================
        private Canvas _ltsCanvas = null!;
        private LabeledTransitionSystem _lts = null!;
        private Verifier _verifier = null!;
        private TextBox _ltsLog = null!;
        private readonly Dictionary<Behavior, Point> _positions = new();
        private const double NodeRadius = 25;
        private const double HSpacing = 180;
        private const double VSpacing = 110;
        private const double EdgeMargin = 60;

        // ============================================================
        // Поля для вкладки «Порт»
        // ============================================================
        private Port _port = null!;
        private DataGrid _cartsGrid = null!;         // таблица автокаров
        private DataGrid _containersGrid = null!;    // таблица контейнеров
        private DataGrid _routesGrid = null!;        // таблица маршрутов
        private DataGrid _capacityRelGrid = null!;   // отношение Capacity
        private DataGrid _destRelGrid = null!;       // отношение Destination
        private DataGrid _composedRelGrid = null!;   // результат композиции ∘
        private TextBox _portLog = null!;            // лог событий порта
        private Label _timeLabel = null!;            // текущее виртуальное время

        public MainWindow()
        {
            InitializeComponent();
            BuildUI();
            InitPort();
            VisualizeLts();
        }

        // ==================================================================
        // ============================ UI ==================================
        // ==================================================================

        /// <summary>
        /// Создаёт всё окно программно, без XAML.
        /// Сверху — TabControl с тремя вкладками.
        /// </summary>
        private void BuildUI()
        {
            Title = "Behavior Algebra: порт + LTS";
            Width = 1500;
            Height = 950;

            var tabs = new TabControl();

            var portTab = new TabItem { Header = "Порт" };
            portTab.Content = BuildPortTab();
            tabs.Items.Add(portTab);

            var relTab = new TabItem { Header = "Отношения (алгебра)" };
            relTab.Content = BuildRelationsTab();
            tabs.Items.Add(relTab);

            var ltsTab = new TabItem { Header = "LTS (верификация)" };
            ltsTab.Content = BuildLtsTab();
            tabs.Items.Add(ltsTab);

            Content = tabs;
        }

        /// <summary>
        /// Строит вкладку «Порт»: панель кнопок сверху,
        /// три таблицы (автокары, контейнеры, маршруты) в середине,
        /// лог событий снизу.
        /// </summary>
        private UIElement BuildPortTab()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(200) });

            // Панель кнопок + метка времени
            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10)
            };

            buttons.Children.Add(MakeButton("Построить план", () => PortBuildPlan()));
            buttons.Children.Add(MakeButton("Шаг времени →", () => PortStep()));
            buttons.Children.Add(MakeButton("Сломать автокар", () => PortBreakCart()));
            buttons.Children.Add(MakeButton("Потеря связи", () => PortLoseCommunication()));
            buttons.Children.Add(MakeButton("Перепланировать", () => PortReplan()));
            buttons.Children.Add(MakeButton("Сбросить", () => PortReset()));

            _timeLabel = new Label
            {
                Content = "t = 0",
                FontSize = 16,
                FontWeight = FontWeights.Bold,
                Foreground = Brushes.DarkBlue,
                Margin = new Thickness(20, 5, 5, 5),
                VerticalAlignment = VerticalAlignment.Center
            };
            buttons.Children.Add(_timeLabel);

            Grid.SetRow(buttons, 0);
            grid.Children.Add(buttons);

            // Три таблицы в ряд
            var tables = new Grid();
            tables.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tables.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tables.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _cartsGrid = MakeGrid();
            _containersGrid = MakeGrid();
            _routesGrid = MakeGrid();

            Grid.SetColumn(_cartsGrid, 0);
            Grid.SetColumn(_containersGrid, 1);
            Grid.SetColumn(_routesGrid, 2);

            tables.Children.Add(_cartsGrid);
            tables.Children.Add(_containersGrid);
            tables.Children.Add(_routesGrid);

            Grid.SetRow(tables, 1);
            grid.Children.Add(tables);

            // Лог событий
            _portLog = new TextBox
            {
                IsReadOnly = true,
                Margin = new Thickness(10),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.LightGreen
            };
            Grid.SetRow(_portLog, 2);
            grid.Children.Add(_portLog);

            return grid;
        }

        /// <summary>
        /// Строит вкладку «Отношения (алгебра)»:
        /// три таблицы — Capacity, Destination, Composed (результат ∘).
        /// </summary>
        private UIElement BuildRelationsTab()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });

            var top = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10)
            };
            top.Children.Add(MakeButton("Пересчитать отношения", () => RefreshRelationsTab()));
            Grid.SetRow(top, 0);
            grid.Children.Add(top);

            var tables = new Grid();
            tables.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tables.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });
            tables.ColumnDefinitions.Add(new ColumnDefinition { Width = new GridLength(1, GridUnitType.Star) });

            _capacityRelGrid = MakeGrid();
            _destRelGrid = MakeGrid();
            _composedRelGrid = MakeGrid();

            Grid.SetColumn(_capacityRelGrid, 0);
            Grid.SetColumn(_destRelGrid, 1);
            Grid.SetColumn(_composedRelGrid, 2);

            tables.Children.Add(_capacityRelGrid);
            tables.Children.Add(_destRelGrid);
            tables.Children.Add(_composedRelGrid);

            Grid.SetRow(tables, 1);
            grid.Children.Add(tables);

            return grid;
        }

        /// <summary>Фабрика кнопки с обработчиком.</summary>
        private Button MakeButton(string text, System.Action onClick)
        {
            var btn = new Button
            {
                Content = text,
                Width = 160,
                Margin = new Thickness(5),
                Padding = new Thickness(10)
            };
            btn.Click += (s, e) => onClick();
            return btn;
        }

        /// <summary>Фабрика таблицы DataGrid с авто-колонками.</summary>
        private DataGrid MakeGrid()
        {
            return new DataGrid
            {
                AutoGenerateColumns = true,
                IsReadOnly = true,
                Margin = new Thickness(5),
                CanUserAddRows = false,
                HeadersVisibility = DataGridHeadersVisibility.Column,
                FontSize = 12
            };
        }

        // ==================================================================
        // ========================== LTS-ВКЛАДКА ============================
        // ==================================================================

        /// <summary>
        /// Строит вкладку «LTS (верификация)»:
        /// кнопки, Canvas для графа и лог.
        /// </summary>
        private UIElement BuildLtsTab()
        {
            var grid = new Grid();
            grid.RowDefinitions.Add(new RowDefinition { Height = GridLength.Auto });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(1, GridUnitType.Star) });
            grid.RowDefinitions.Add(new RowDefinition { Height = new GridLength(150) });

            var buttons = new StackPanel
            {
                Orientation = Orientation.Horizontal,
                Margin = new Thickness(10)
            };
            buttons.Children.Add(MakeButton("Верифицировать", () => RunVerification()));
            buttons.Children.Add(MakeButton("Перестроить LTS", () => VisualizeLts()));
            Grid.SetRow(buttons, 0);
            grid.Children.Add(buttons);

            _ltsCanvas = new Canvas { Background = Brushes.White };
            var scroll = new ScrollViewer
            {
                Content = _ltsCanvas,
                HorizontalScrollBarVisibility = ScrollBarVisibility.Auto,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Margin = new Thickness(10)
            };
            Grid.SetRow(scroll, 1);
            grid.Children.Add(scroll);

            _ltsLog = new TextBox
            {
                IsReadOnly = true,
                Margin = new Thickness(10),
                FontFamily = new FontFamily("Consolas"),
                FontSize = 13,
                VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
                Background = new SolidColorBrush(Color.FromRgb(30, 30, 30)),
                Foreground = Brushes.LightGreen
            };
            Grid.SetRow(_ltsLog, 2);
            grid.Children.Add(_ltsLog);

            return grid;
        }

        // ==================================================================
        // ========================== ЛОГИКА ПОРТА ===========================
        // ==================================================================

        /// <summary>
        /// Создаёт начальный порт и обновляет все таблицы.
        /// </summary>
        private void InitPort()
        {
            _port = Port.CreateDefault();
            RefreshPortUI();
            RefreshRelationsTab();
            PortLog("Порт инициализирован: 4 автокара, 5 контейнеров, 3 склада.");
        }

        /// <summary>
        /// Обновляет три таблицы порта (автокары, контейнеры, маршруты)
        /// и метку текущего времени.
        /// </summary>
        private void RefreshPortUI()
        {
            // Таблица автокаров
            _cartsGrid.ItemsSource = _port.Carts.Select(c => new
            {
                c.Id,
                c.Capacity,
                c.Location,
                Status = c.Status.ToString()
            }).ToList();

            // Таблица контейнеров
            _containersGrid.ItemsSource = _port.Containers.Select(c => new
            {
                c.Id,
                c.Weight,
                c.Destination,
                c.Priority,
                Status = c.Status.ToString()
            }).ToList();

            // Таблица маршрутов
            _routesGrid.ItemsSource = _port.Routes.Select(r => new
            {
                Cart = r.CartId,
                Container = r.ContainerId,
                Warehouse = r.WarehouseId,
                Started = r.StartedAt,
                Status = r.Status.ToString()
            }).ToList();

            _timeLabel.Content = $"t = {_port.Time}";
        }

        /// <summary>
        /// Пересчитывает и обновляет три таблицы отношений:
        ///   Capacity    ⊆ Cart × Container
        ///   Destination ⊆ Container × Warehouse
        ///   Composed    = Capacity ∘ Destination (результат композиции)
        /// </summary>
        private void RefreshRelationsTab()
        {
            // Отношение Capacity
            var cap = RelationsAlgebra.BuildCapacityRelation(_port);
            _capacityRelGrid.ItemsSource = cap.Tuples
                .OrderBy(t => t.Item1.Id).ThenBy(t => t.Item2.Id)
                .Select(t => new
                {
                    Cart = t.Item1.Id,
                    Container = t.Item2.Id,
                    Rule = $"{t.Item1.Capacity} >= {t.Item2.Weight}"
                }).ToList();

            // Отношение Destination
            var dest = RelationsAlgebra.BuildDestinationRelation(_port);
            _destRelGrid.ItemsSource = dest.Tuples
                .OrderBy(t => t.Item1.Id)
                .Select(t => new
                {
                    Container = t.Item1.Id,
                    Destination = t.Item1.Destination,
                    Warehouse = t.Item2.Id
                }).ToList();

            // Композиция ∘
            var composed = RelationsAlgebra.ComposeRoutes(_port);
            _composedRelGrid.ItemsSource = composed.Tuples
                .OrderBy(t => t.Item1.Id).ThenBy(t => t.Item2.Id)
                .Select(t => new
                {
                    Cart = t.Item1.Id,
                    Warehouse = t.Item2.Name,
                    Via = "Capacity ∘ Destination"
                }).ToList();
        }

        /// <summary>
        /// Кнопка «Построить план». Применяет операции ∩ и ∘.
        /// </summary>
        private void PortBuildPlan()
        {
            PortLog($"=== Построение плана (t={_port.Time}) ===");
            PortLog("∩ активные ∩ Capacity, затем ∘ Destination");
            PortSimulator.BuildPlan(_port);
            FlushPortEvents();
            RefreshPortUI();
            RefreshRelationsTab();
        }

        /// <summary>
        /// Кнопка «Шаг времени». Продвигает симуляцию на 1 шаг,
        /// обрабатывает таймеры ремонта/восстановления и доставку.
        /// </summary>
        private void PortStep()
        {
            PortSimulator.Step(_port);
            PortLog($"--- Шаг времени: t = {_port.Time} ---");
            FlushPortEvents();
            RefreshPortUI();
        }

        /// <summary>
        /// Кнопка «Сломать автокар». Находит первый Busy-автокар,
        /// ломает его, отменяет маршрут, возвращает контейнер в очередь.
        /// </summary>
        private void PortBreakCart()
        {
            var cart = _port.Carts.FirstOrDefault(c => c.Status == CartStatus.Busy);
            if (cart == null)
            {
                PortLog("Нет занятых автокаров для сбоя.");
                return;
            }
            PortLog($"=== СБОЙ (t={_port.Time}) ===");
            PortSimulator.BreakCart(_port, cart.Id);
            FlushPortEvents();
            RefreshPortUI();
        }

        /// <summary>
        /// Кнопка «Потеря связи». Находит первый Busy-автокар,
        /// помечает его как Delayed, маршрут — задержан.
        /// </summary>
        private void PortLoseCommunication()
        {
            var cart = _port.Carts.FirstOrDefault(c => c.Status == CartStatus.Busy);
            if (cart == null)
            {
                PortLog("Нет занятых автокаров для сбоя.");
                return;
            }
            PortLog($"=== СБОЙ (t={_port.Time}) ===");
            PortSimulator.LoseCommunication(_port, cart.Id);
            FlushPortEvents();
            RefreshPortUI();
        }

        /// <summary>
        /// Кнопка «Перепланировать». Удаляет отменённые и задержанные
        /// маршруты, строит новый план на освободившихся ресурсах.
        /// </summary>
        private void PortReplan()
        {
            PortLog("=== Перепланирование ===");
            PortSimulator.Replan(_port);
            FlushPortEvents();
            RefreshPortUI();
            RefreshRelationsTab();
        }

        /// <summary>Кнопка «Сбросить». Возвращает порт в начальное состояние.</summary>
        private void PortReset()
        {
            _port.Reset();
            PortLog("Сброс порта.");
            RefreshPortUI();
            RefreshRelationsTab();
        }

        /// <summary>Пишет строку в лог порта с отметкой времени.</summary>
        private void PortLog(string msg)
        {
            _portLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            _portLog.ScrollToEnd();
        }

        /// <summary>
        /// Переносит накопленные события из port.Events в лог интерфейса
        /// и очищает очередь событий.
        /// </summary>
        private void FlushPortEvents()
        {
            foreach (var e in _port.Events)
                PortLog("  " + e);
            _port.Events.Clear();
        }

        // ==================================================================
        // ========================== ЛОГИКА LTS =============================
        // ==================================================================

        /// <summary>
        /// Строит LTS из поведения порта и рисует граф.
        /// </summary>
        private void VisualizeLts()
        {
            if (_ltsCanvas == null) return;

            _ltsCanvas.Children.Clear();
            _positions.Clear();

            var behavior = PortModel.BuildCartBehavior();
            _lts = new LabeledTransitionSystem(behavior);
            _verifier = new Verifier(_lts);

            ComputePositions();
            DrawAllTransitions();
            DrawAllNodes();

            double maxX = _positions.Values.Max(p => p.X) + EdgeMargin;
            double maxY = _positions.Values.Max(p => p.Y) + EdgeMargin;
            _ltsCanvas.Width = Math.Max(maxX, 1000);
            _ltsCanvas.Height = Math.Max(maxY, 600);

            if (_ltsLog != null)
                LtsLog($"LTS построен: {_lts.StateCount} состояний, {_lts.TransitionCount} переходов.");
        }

        /// <summary>
        /// Раскладка узлов LTS по уровням (BFS).
        /// Узлы одного уровня — на одной горизонтали.
        /// </summary>
        private void ComputePositions()
        {
            var levels = new Dictionary<Behavior, int>();
            var queue = new Queue<Behavior>();
            queue.Enqueue(_lts.InitialState);
            levels[_lts.InitialState] = 0;

            while (queue.Count > 0)
            {
                var current = queue.Dequeue();
                foreach (var t in _lts.GetTransitions(current))
                {
                    if (!levels.ContainsKey(t.To))
                    {
                        levels[t.To] = levels[current] + 1;
                        queue.Enqueue(t.To);
                    }
                }
            }

            foreach (var group in levels.GroupBy(kv => kv.Value).OrderBy(g => g.Key))
            {
                int level = group.Key;
                var nodes = group.Select(kv => kv.Key).ToList();

                for (int i = 0; i < nodes.Count; i++)
                {
                    double x = EdgeMargin + 200 + i * HSpacing;
                    double y = EdgeMargin + level * VSpacing;
                    _positions[nodes[i]] = new Point(x, y);
                }
            }
        }

        /// <summary>
        /// Рисует все узлы LTS: кружок + подпись.
        /// Цвет зависит от типа состояния (Δ, 0, ⊥, Choice, Prefix).
        /// </summary>
        private void DrawAllNodes()
        {
            foreach (var state in _lts.States)
            {
                if (!_positions.TryGetValue(state, out var pos)) continue;

                Brush fill;
                if (state is Success) fill = Brushes.LightGreen;
                else if (state is Deadlock) fill = Brushes.IndianRed;
                else if (state is Undefined) fill = Brushes.Orange;
                else if (state is Choice) fill = Brushes.LightSkyBlue;
                else fill = Brushes.LightYellow;

                var ellipse = new Ellipse
                {
                    Width = NodeRadius * 2,
                    Height = NodeRadius * 2,
                    Fill = fill,
                    Stroke = Brushes.Black,
                    StrokeThickness = 2
                };
                Canvas.SetLeft(ellipse, pos.X - NodeRadius);
                Canvas.SetTop(ellipse, pos.Y - NodeRadius);
                _ltsCanvas.Children.Add(ellipse);

                string label = state switch
                {
                    Success => "Δ",
                    Deadlock => "0",
                    Undefined => "⊥",
                    Choice => "⊕",
                    Prefix p => p.Action.Name,
                    _ => "?"
                };

                var text = new TextBlock
                {
                    Text = label,
                    FontSize = 11,
                    FontWeight = FontWeights.Bold,
                    Foreground = Brushes.Black
                };
                text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
                Canvas.SetLeft(text, pos.X - text.DesiredSize.Width / 2);
                Canvas.SetTop(text, pos.Y - text.DesiredSize.Height / 2);
                _ltsCanvas.Children.Add(text);
            }
        }

        /// <summary>
        /// Рисует все рёбра LTS (со стрелками и подписями действий).
        /// </summary>
        private void DrawAllTransitions()
        {
            foreach (var state in _lts.States)
            {
                foreach (var t in _lts.GetTransitions(state))
                {
                    if (!_positions.TryGetValue(t.From, out var from)) continue;
                    if (!_positions.TryGetValue(t.To, out var to)) continue;
                    DrawArrow(from, to, t.Action.Name);
                }
            }
        }

        /// <summary>
        /// Рисует одну стрелку от узла к узлу с подписью действия.
        /// </summary>
        private void DrawArrow(Point from, Point to, string label)
        {
            var direction = to - from;
            var length = direction.Length;
            if (length < 0.01) return;

            var unit = new Vector(direction.X / length, direction.Y / length);
            var start = from + unit * NodeRadius;
            var end = to - unit * NodeRadius;

            _ltsCanvas.Children.Add(new Line
            {
                X1 = start.X,
                Y1 = start.Y,
                X2 = end.X,
                Y2 = end.Y,
                Stroke = Brushes.DimGray,
                StrokeThickness = 1.5
            });

            var arrowSize = 9.0;
            var perp = new Vector(-unit.Y, unit.X);
            var p1 = end;
            var p2 = end - unit * arrowSize + perp * arrowSize * 0.5;
            var p3 = end - unit * arrowSize - perp * arrowSize * 0.5;

            _ltsCanvas.Children.Add(new Polygon
            {
                Points = new PointCollection { p1, p2, p3 },
                Fill = Brushes.DimGray
            });

            var mid = new Point((start.X + end.X) / 2, (start.Y + end.Y) / 2);
            var text = new TextBlock
            {
                Text = label,
                FontSize = 10,
                Foreground = Brushes.DarkBlue,
                Background = Brushes.White
            };
            text.Measure(new Size(double.PositiveInfinity, double.PositiveInfinity));
            Canvas.SetLeft(text, mid.X - text.DesiredSize.Width / 2);
            Canvas.SetTop(text, mid.Y - text.DesiredSize.Height / 2 - 12);
            _ltsCanvas.Children.Add(text);
        }

        /// <summary>
        /// Запускает model checking: проверяет достижимость Δ и 0,
        /// живость системы и находит кратчайший путь к успеху.
        /// </summary>
        private void RunVerification()
        {
            _ltsLog.Clear();
            LtsLog("=== Верификация ===");
            LtsLog($"Δ достижим:           {_verifier.IsSuccessReachable()}");
            LtsLog($"0 (тупик) достижим:   {_verifier.IsDeadlockReachable()}");
            LtsLog($"Система жива:         {_verifier.IsLive()}");
            LtsLog($"Тупиковых состояний:  {_verifier.FindDeadlockStates().Count}");

            var path = _verifier.FindShortestPathToSuccess();
            if (path != null)
                LtsLog("Кратчайший путь к Δ: " +
                       string.Join(" → ", path.Select(t => t.Action.Name)));
            else
                LtsLog("Путь к Δ не найден.");
        }

        /// <summary>Пишет строку в лог LTS-вкладки.</summary>
        private void LtsLog(string msg)
        {
            _ltsLog.AppendText($"[{DateTime.Now:HH:mm:ss}] {msg}\n");
            _ltsLog.ScrollToEnd();
        }
    }
}