using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Shapes;
using TinkoffInvestLib;

namespace TinkoffInvestApp
{
    /// <summary>
    /// Логика взаимодействия для AddTimerWindow.xaml
    /// </summary>
    public partial class AddTimerWindow : Window
    {
        ObservableCollection<Timer> timers = new ObservableCollection<Timer>();
        MainWindow mainWindow;

        public AddTimerWindow()
        {
            mainWindow = (MainWindow)Application.Current.MainWindow;
            InitializeComponent();
            SelectedStrategyComboBox.ItemsSource = Enum.GetValues(typeof(AvailableStrategiesEnum)).Cast<AvailableStrategiesEnum>(); 
            SelectedStrategyComboBox.SelectedIndex = 0;
            TimersListBox.ItemsSource = timers;

            Task.Run(() =>
            {
                while (true)
                {
                    foreach (var timer in timers)
                    {
                        if (DateTime.Now.TimeOfDay.Hours == timer.ExecutionTime.Hours && DateTime.Now.TimeOfDay.Minutes == timer.ExecutionTime.Minutes)
                        {
                            MessageBox.Show("ТАЙМЕР!!!" + timer.Ticker);
                        }
                    }
                    Thread.Sleep(60000); //60 секунд простоя
                }
                
                
            });
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void AddTimerButton_Click(object sender, RoutedEventArgs e)
        {
            timers.Add(new Timer
            {
                Ticker = SelectedTickerTextBox.Text,
                ExecutionTime = TimerTimePicker.Value.Value.TimeOfDay,
                SendToTelegram = false,
                Strategy = (AvailableStrategiesEnum)SelectedStrategyComboBox.SelectedIndex,
            });
        }

        private void GoBackButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.Visibility = Visibility.Visible;
            this.Visibility = Visibility.Collapsed;
        }
    }
}
