using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Windows;
using System.Windows.Controls;

namespace SimplexMethod
{
    public partial class MainWindow : Window
    {
        private SimplexSolver simplexSolver;
        public MainWindow()
        {
            InitializeComponent();
        }

        // Обработчик для кнопки "Задать целевую функцию"
        private void OnSetObjectiveFunctionClick(object sender, RoutedEventArgs e)
        {
            // Здесь нужно собрать данные для целевой функции
            string objectiveText = ObjectiveFunctionTextBox.Text;
            bool isMaximization = MaximizeCheckBox.IsChecked.GetValueOrDefault();

            // Преобразуем строку в коэффициенты
            List<double> objectiveCoefficients = objectiveText.Split(',').Select(double.Parse).ToList();

            // Создаем целевую функцию
            ObjectiveFunction objectiveFunction = new ObjectiveFunction(objectiveCoefficients, isMaximization);

            // Инициализируем решатель
            simplexSolver = new SimplexSolver(objectiveFunction, new List<Constraint>());

            MessageBox.Show("Целевая функция задана.");
        }

        // Обработчик для добавления ограничения
        private void OnSetConstraintsClick(object sender, RoutedEventArgs e)
        {
            // Очистка текущих элементов
            ConstraintsStackPanel.Children.Clear();

            // Получаем количество ограничений
            if (int.TryParse(ConstraintsCountTextBox.Text, out int numConstraints) && numConstraints > 0)
            {
                for (int i = 0; i < numConstraints; i++)
                {
                    // Создаем панель для каждого ограничения
                    StackPanel constraintPanel = new StackPanel { Orientation = Orientation.Horizontal };

                    // Текстовые поля для коэффициентов
                    TextBox coefficientTextBox1 = new TextBox { Width = 50, Margin = new Thickness(5) };
                    constraintPanel.Children.Add(coefficientTextBox1);

                    TextBox coefficientTextBox2 = new TextBox { Width = 50, Margin = new Thickness(5) };
                    constraintPanel.Children.Add(coefficientTextBox2);

                    // Комбо-бокс для знака неравенства
                    ComboBox inequalityComboBox = new ComboBox { Width = 70, Margin = new Thickness(5) };
                    inequalityComboBox.Items.Add("<=");
                    inequalityComboBox.Items.Add(">=");
                    inequalityComboBox.Items.Add("=");
                    inequalityComboBox.SelectedIndex = 0; // По умолчанию "<="
                    constraintPanel.Children.Add(inequalityComboBox);

                    // Текстовое поле для правой части
                    TextBox rhsTextBox = new TextBox { Width = 50, Margin = new Thickness(5) };
                    constraintPanel.Children.Add(rhsTextBox);

                    // Добавляем панель с элементами для текущего ограничения в StackPanel
                    ConstraintsStackPanel.Children.Add(constraintPanel);
                }
            }
            else
            {
                MessageBox.Show("Введите корректное количество ограничений.");
            }
        }



        // Обработчик для решения задачи
        private void OnSolveClick(object sender, RoutedEventArgs e)
        {
            // Инициализация и решение задачи
            simplexSolver.Initialize();
            simplexSolver.Solve();

            // Вывод решения в DataGrid
            SolutionDataGrid.ItemsSource = simplexSolver.GetSolution();
        }

        // Обработчик для экспорта результата в файл
        private void OnExportClick(object sender, RoutedEventArgs e)
        {
            string filePath = "solution.txt";
            simplexSolver.ExportSolutionToTxt(filePath);
            MessageBox.Show($"Решение экспортировано в файл: {filePath}");
        }


        // Класс для представления переменной
        public class Variable
        {
            public string Name { get; set; }
            public double Value { get; set; }
            public bool IsBasic { get; set; } // Базовая или небазовая переменная
        }

        // Класс для представления ограничений
        public class Constraint
        {
            public List<double> Coefficients { get; set; }
            public double RHS { get; set; } // Правая часть
            public string Inequality { get; set; } // Тип: ≤, ≥, =

            public Constraint(List<double> coefficients, double rhs, string inequality)
            {
                Coefficients = coefficients;
                RHS = rhs;
                Inequality = inequality;
            }
        }

        // Класс для целевой функции
        public class ObjectiveFunction
        {
            public List<double> Coefficients { get; set; }
            public bool IsMaximization { get; set; } // Максимизация или минимизация

            public ObjectiveFunction(List<double> coefficients, bool isMaximization)
            {
                Coefficients = coefficients;
                IsMaximization = isMaximization;
            }
        }

        public class SimplexTable
        {
            public List<List<double>> Matrix { get; set; } // Симплекс-таблица
            public List<Variable> Variables { get; set; } // Переменные, включая искусственные и базисные
            public ObjectiveFunction Objective { get; set; }

            public SimplexTable(ObjectiveFunction objective)
            {
                Matrix = new List<List<double>>();
                Variables = new List<Variable>();
                Objective = objective;
            }

            public void PrintTable()
            {
                // Метод для печати таблицы в консоль или отображение в UI
                StringBuilder sb = new StringBuilder();
                foreach (var row in Matrix)
                {
                    sb.AppendLine(string.Join("\t", row));
                }
                Console.WriteLine(sb.ToString());
            }

            public void PrintTableInUI()
            {
                StringBuilder sb = new StringBuilder();

                // Выводим заголовки столбцов
                sb.AppendLine("Переменные\tЗначение\tПравые части");

                // Выводим строки таблицы
                foreach (var row in Matrix)
                {
                    sb.AppendLine(string.Join("\t", row));
                }

                // Для отображения в UI, например в TextBlock:
                Console.WriteLine(sb.ToString());
            }


        }

        public class SimplexSolver
        {
            private ObjectiveFunction _objective;
            private List<Constraint> _constraints;
            private SimplexTable _currentTable;
            private MainWindow _mainWindow;
            private MainWindow mainWindow;

            public SimplexSolver(ObjectiveFunction objective, List<Constraint> constraints)
            {
                _objective = objective;
                _constraints = constraints;
                _mainWindow = mainWindow;
            }

            public void Initialize()
            {
                // Преобразуем задачу в каноническую форму
                foreach (var constraint in _constraints)
                {
                    if (constraint.Inequality == ">=")
                    {
                        constraint.Coefficients.Add(-1); // Добавление дополнительной переменной
                    }
                    else if (constraint.Inequality == "=")
                    {
                        constraint.Coefficients.Add(0); // В случае = добавляем искусственную переменную
                    }
                }

                // Создание начальной симплекс-таблицы
                _currentTable = new SimplexTable(_objective);
                FillTable();
            }

            public void FillTable()
            {
                List<List<double>> matrix = new List<List<double>>();

                // Для целевой функции
                matrix.Add(new List<double>(_objective.Coefficients));

                // Для ограничений
                foreach (var constraint in _constraints)
                {
                    matrix.Add(constraint.Coefficients);
                }

                _currentTable.Matrix = matrix;
            }

            public void Solve()
            {
                while (true)
                {
                    int pivotColumn = GetPivotColumn();

                    if (pivotColumn == -1)
                    {
                        // Если не найден столбец для вращения, задача решена
                        break;
                    }

                    int pivotRow = GetPivotRow(pivotColumn);

                    if (pivotRow == -1)
                    {
                        // Если не найден ряд для вращения, задача некорректна
                        MessageBox.Show("Не удалось найти подходящий ряд для вращения.");
                        break;
                    }

                    // Логируем значения pivotRow и pivotColumn для отладки
                    Console.WriteLine($"Pivoting at row {pivotRow}, column {pivotColumn}");

                    PerformPivoting(pivotRow, pivotColumn);

                    _currentTable.PrintTable();
                }
            }


            private int GetPivotColumn()
            {
                // Проверяем, если все коэффициенты в строке целевой функции ≤ 0
                bool isOptimal = true;
                for (int i = 0; i < _currentTable.Matrix[0].Count; i++)
                {
                    if (_currentTable.Matrix[0][i] > 0) // Если найдено положительное число
                    {
                        isOptimal = false;
                        break;
                    }
                }

                if (isOptimal)
                {
                    MessageBox.Show("Задача решена. Оптимальное решение найдено.");
                    return -1; // Возвращаем -1, чтобы выйти из цикла решения
                }

                // Ищем столбец для вращения
                for (int i = 0; i < _currentTable.Matrix[0].Count; i++)
                {
                    if (_currentTable.Matrix[0][i] > 0)
                    {
                        return i;
                    }
                }

                return -1; // Если не найден подходящий столбец
            }



            private int GetPivotRow(int pivotColumn)
            {
                double minRatio = double.MaxValue;
                int pivotRow = -1;

                for (int i = 1; i < _currentTable.Matrix.Count; i++) // Пропускаем первую строку (целевая функция)
                {
                    if (_currentTable.Matrix[i][pivotColumn] > 0) // Если элемент в столбце положительный
                    {
                        double ratio = _currentTable.Matrix[i].Last() / _currentTable.Matrix[i][pivotColumn];
                        if (ratio < minRatio)
                        {
                            minRatio = ratio;
                            pivotRow = i;
                        }
                    }
                }

                // Если не найден подходящий ряд
                if (pivotRow == -1)
                {
                    MessageBox.Show("Не удалось найти подходящий ряд для вращения. Возможно задача уже решена.");
                }

                return pivotRow;
            }



            public void PerformPivoting(int pivotRow, int pivotColumn)
            {
                if (pivotRow >= _currentTable.Matrix.Count || pivotColumn >= _currentTable.Matrix[0].Count)
                {
                    // Логируем ошибку, если индексы выходят за пределы
                    Console.WriteLine("Ошибка: индексы выходят за пределы таблицы.");
                    return;
                }

                double pivotValue = _currentTable.Matrix[pivotRow][pivotColumn];
                if (pivotValue == 0)
                {
                    Console.WriteLine("Ошибка: значение опорного элемента равно 0.");
                    return;
                }

                for (int i = 0; i < _currentTable.Matrix[pivotRow].Count; i++)
                {
                    _currentTable.Matrix[pivotRow][i] /= pivotValue;
                }

                for (int i = 0; i < _currentTable.Matrix.Count; i++)
                {
                    if (i != pivotRow)
                    {
                        double factor = _currentTable.Matrix[i][pivotColumn];
                        for (int j = 0; j < _currentTable.Matrix[i].Count; j++)
                        {
                            _currentTable.Matrix[i][j] -= factor * _currentTable.Matrix[pivotRow][j];
                        }
                    }
                }
            }
            public void UpdateUIWithTable()
            {
                StringBuilder sb = new StringBuilder();

                // Выводим таблицу
                foreach (var row in _currentTable.Matrix)
                {
                    sb.AppendLine(string.Join("\t", row));
                }

                // Обновляем TextBlock через ссылку на MainWindow
                _mainWindow.SolutionTextBlock.Text = sb.ToString();
            }

            public void DisplayVariableValues()
            {
                StringBuilder sb = new StringBuilder();

                foreach (var variable in _currentTable.Variables)
                {
                    sb.AppendLine($"{variable.Name}: {variable.Value}");
                }

                // Обновляем TextBlock через ссылку на MainWindow
                _mainWindow.VariablesTextBlock.Text = sb.ToString();
            }
            public void DisplaySolution()
            {
                StringBuilder sb = new StringBuilder();

                sb.AppendLine("Оптимальное решение:");

                for (int i = 0; i < _currentTable.Variables.Count; i++)
                {
                    var variable = _currentTable.Variables[i];
                    sb.AppendLine($"Переменная {variable.Name}: {variable.Value}");
                }

                sb.AppendLine("Значение целевой функции: " + _currentTable.Matrix[0].Last());

                // Обновляем TextBlock в MainWindow
                _mainWindow.SolutionTextBlock.Text = sb.ToString();  // Используем ссылку на MainWindow
            }
            public List<List<double>> GetSolution()
            {
                // Возвращаем решение из симплекс-таблицы
                return _currentTable.Matrix;
            }

            public void ExportSolutionToTxt(string path)
            {
                StringBuilder sb = new StringBuilder();

                foreach (var row in _currentTable.Matrix)
                {
                    sb.AppendLine(string.Join("\t", row));
                }

                // Записываем в файл
                File.WriteAllText(path, sb.ToString());
            }

            public void AddConstraint(Constraint constraint)
            {
                _constraints.Add(constraint);
            }
        }
    }
}
