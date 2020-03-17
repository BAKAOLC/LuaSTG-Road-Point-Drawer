using System;
using System.Collections;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using System.Text;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;

namespace LuaSTGRoadDrawer
{
    /// <summary>
    /// MainWindow.xaml 的交互逻辑
    /// </summary>
    public partial class MainWindow : Window
    {
        LuaSTGWorldMap LSTGMap = new LuaSTGWorldMap();
        RoadCalculator Calculator = new RoadCalculator();

        public MainWindow()
        {
            InitializeComponent();
            codeText.Text = Calculator.LuaTask;
            codeText.TextChanged += (sender, e) =>
            {
                Calculator.LuaTask = codeText.Text;
            };
            StartPoint_X.TextChanged += (sender, e) =>
            {
                if (double.TryParse(StartPoint_X.Text, out double value))
                    Calculator.StartPoint.X = value;
            };
            StartPoint_Y.TextChanged += (sender, e) =>
            {
                if (double.TryParse(StartPoint_Y.Text, out double value))
                    Calculator.StartPoint.Y = value;
            };
            EndPoint_X.TextChanged += (sender, e) =>
            {
                if (double.TryParse(EndPoint_X.Text, out double value))
                    Calculator.EndPoint.X = value;
            };
            EndPoint_Y.TextChanged += (sender, e) =>
            {
                if (double.TryParse(EndPoint_Y.Text, out double value))
                    Calculator.EndPoint.Y = value;
            };
            PointCount.TextChanged += (sender, e) =>
            {
                if (int.TryParse(PointCount.Text, out int value))
                    Calculator.PointCount = value;
            };
        }

        private void UpdateList()
        {
            ConsolePointList.ItemsSource = (ArrayList)Calculator.PointList.Clone();
        }

        private void Insert_Console_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                var x = double.Parse(Console_X.Text);
                var y = double.Parse(Console_Y.Text);

                Calculator.InsertConsolePoint(new System.Windows.Point(x, y));
                UpdateList();
            }
            catch
            {
            }
        }

        private void Remove_Console_Click(object sender, RoutedEventArgs e)
        {
            try
            {
                Calculator.RemoveConsolePoint(ConsolePointList.SelectedIndex);
                UpdateList();
            }
            catch
            {
            }
        }

        private void Clear_Console_Click(object sender, RoutedEventArgs e)
        {
            Calculator.ClearConsolePoint();
            UpdateList();
        }

        private async void Start_Click(object sender, RoutedEventArgs e)
        {
            StartPoint_X.IsEnabled = false;
            StartPoint_Y.IsEnabled = false;
            EndPoint_X.IsEnabled = false;
            EndPoint_Y.IsEnabled = false;
            PointCount.IsEnabled = false;
            MoveMode.IsEnabled = false;
            Console_X.IsEnabled = false;
            Console_Y.IsEnabled = false;
            ConsolePointList.IsEnabled = false;
            Insert_Console.IsEnabled = false;
            Remove_Console.IsEnabled = false;
            Clear_Console.IsEnabled = false;
            codeText.IsEnabled = false;
            Start.IsEnabled = false;
            await Task.Run(() =>
            {
                try
                {
                    var result = Calculator.Calculate();
                    Bitmap img = LSTGMap.GetImage(Calculator.PointList, result, Calculator.StartPoint, Calculator.EndPoint);
                    App.Current.Dispatcher.Invoke(() =>
                    {
                        ResultWindow rw = new ResultWindow(img);
                        Closing += (_sender, _e) =>
                        {
                            try
                            {
                                if (rw.IsLoaded)
                                    rw.Close();
                            }
                            catch
                            {
                            }
                        };
                        rw.Show();
                    });
                }
                catch (Exception ex)
                {
                    MessageBox.Show(ex.Message + "\n" + ex.StackTrace);
                }
            });
            StartPoint_X.IsEnabled = true;
            StartPoint_Y.IsEnabled = true;
            EndPoint_X.IsEnabled = true;
            EndPoint_Y.IsEnabled = true;
            PointCount.IsEnabled = true;
            MoveMode.IsEnabled = true;
            Console_X.IsEnabled = true;
            Console_Y.IsEnabled = true;
            ConsolePointList.IsEnabled = true;
            Insert_Console.IsEnabled = true;
            Remove_Console.IsEnabled = true;
            Clear_Console.IsEnabled = true;
            codeText.IsEnabled = true;
            Start.IsEnabled = true;
        }

        private void MoveMode_Loaded(object sender, RoutedEventArgs e)
        {
            MoveMode.ItemsSource = Enum.GetNames(typeof(MOVETYPE));
            MoveMode.SelectedIndex = 0;
        }

        private void MoveMode_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            try
            {
                Calculator.MoveType = (MOVETYPE)MoveMode.SelectedIndex;
            }
            catch
            {
            }
        }
    }
}
