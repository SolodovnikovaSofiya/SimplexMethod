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
        public MainWindow()
        {
            InitializeComponent();
        }

        private void Solve_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                int numVariables = int.Parse(VariablesBox.Text);
                int numConstraints = int.Parse(ConstraintsBox.Text);
                double[] objectiveCoefficients = VariablesBox.Text.Split(' ').Select(double.Parse).ToArray();
                objectiveCoefficients = ObjectiveBox.Text.Split(' ').Select(double.Parse).ToArray();
                double[,] constraintsCoefficients = new double[numConstraints, numVariables];
                double[] rhs = RhsBox.Text.Split(' ').Select(double.Parse).ToArray();

                string[] constraintLines = ConstraintsInput.Text.Split(new[] { '\r', '\n' }, StringSplitOptions.RemoveEmptyEntries);
                for (int i = 0; i < numConstraints; i++)
                {
                    string[] parts = constraintLines[i].Split(' ');
                    for (int j = 0; j < numVariables; j++)
                    {
                        constraintsCoefficients[i, j] = double.Parse(parts[j]);
                    }
                }

                string result = Simplex(objectiveCoefficients, constraintsCoefficients, rhs);
                ResultBlock.Text = result;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Ошибка: " + ex.Message);
            }
        }

        private string Simplex(double[] objectiveCoefficients, double[,] constraintsCoefficients, double[] rhs)
        {
            int numVariables = objectiveCoefficients.Length;
            int numConstraints = rhs.Length;
            double[,] tableau = new double[numConstraints + 1, numVariables + numConstraints + 1];

            for (int i = 0; i < numConstraints; i++)
            {
                for (int j = 0; j < numVariables; j++)
                    tableau[i, j] = constraintsCoefficients[i, j];
                tableau[i, numVariables + i] = 1;
                tableau[i, numVariables + numConstraints] = rhs[i];
            }

            for (int j = 0; j < numVariables; j++)
                tableau[numConstraints, j] = -objectiveCoefficients[j];

            while (true)
            {
                int enteringColumn = -1;
                double minValue = 0;
                for (int j = 0; j < numVariables + numConstraints; j++)
                {
                    if (tableau[numConstraints, j] < minValue)
                    {
                        minValue = tableau[numConstraints, j];
                        enteringColumn = j;
                    }
                }

                if (enteringColumn == -1)
                    break;

                int leavingRow = -1;
                double minRatio = double.MaxValue;
                for (int i = 0; i < numConstraints; i++)
                {
                    if (tableau[i, enteringColumn] > 0)
                    {
                        double ratio = tableau[i, numVariables + numConstraints] / tableau[i, enteringColumn];
                        if (ratio < minRatio)
                        {
                            minRatio = ratio;
                            leavingRow = i;
                        }
                    }
                }

                if (leavingRow == -1)
                    return "Задача не ограничена.";

                double pivot = tableau[leavingRow, enteringColumn];
                for (int j = 0; j <= numVariables + numConstraints; j++)
                    tableau[leavingRow, j] /= pivot;

                for (int i = 0; i <= numConstraints; i++)
                {
                    if (i != leavingRow)
                    {
                        double factor = tableau[i, enteringColumn];
                        for (int j = 0; j <= numVariables + numConstraints; j++)
                            tableau[i, j] -= factor * tableau[leavingRow, j];
                    }
                }
            }

            StringBuilder sb = new StringBuilder("Оптимальное решение:\n");
            for (int j = 0; j < numVariables; j++)
            {
                bool isBasic = true;
                int basicRow = -1;
                for (int i = 0; i < numConstraints; i++)
                {
                    if (tableau[i, j] == 1)
                    {
                        if (basicRow == -1)
                            basicRow = i;
                        else
                        {
                            isBasic = false;
                            break;
                        }
                    }
                    else if (tableau[i, j] != 0)
                    {
                        isBasic = false;
                        break;
                    }
                }
                if (isBasic && basicRow != -1)
                    sb.AppendLine($"x{j + 1} = {Math.Round(tableau[basicRow, numVariables + numConstraints], 4)}");
                else
                    sb.AppendLine($"x{j + 1} = 0");
            }

            sb.AppendLine($"Максимальное значение целевой функции: {Math.Round(tableau[numConstraints, numVariables + numConstraints], 4)}");
            return sb.ToString();
        }
        private void SaveToFile_Click(object sender, RoutedEventArgs e)
        {
            Microsoft.Win32.SaveFileDialog saveFileDialog = new Microsoft.Win32.SaveFileDialog();
            saveFileDialog.Filter = "Text file (*.txt)|*.txt";
            saveFileDialog.FileName = "результат_симплекс_метод.txt";

            if (saveFileDialog.ShowDialog() == true)
            {
                try
                {
                    File.WriteAllText(saveFileDialog.FileName, ResultBlock.Text);
                    MessageBox.Show("Результат успешно сохранён.", "Сохранение", MessageBoxButton.OK, MessageBoxImage.Information);
                }
                catch (Exception ex)
                {
                    MessageBox.Show("Ошибка при сохранении: " + ex.Message, "Ошибка", MessageBoxButton.OK, MessageBoxImage.Error);
                }
            }
        }

        private void Clear_Click(object sender, RoutedEventArgs e)
        {
            // Очистка результатов
            ResultBlock.Text = "";
            VariablesBox.Text = "";
            ConstraintsBox.Text = "";
            ObjectiveBox.Text = "";
            ConstraintsInput.Text = "";
            RhsBox.Text = "";
        }
    }
}
