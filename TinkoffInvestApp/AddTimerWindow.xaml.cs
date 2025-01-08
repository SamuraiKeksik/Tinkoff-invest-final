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
using Tinkoff.InvestApi.V1;
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
                            Dispatcher.Invoke(new Action(() =>
                            {
                                int.TryParse(FirstStrategyValueTextBox.Text, out int firstParam);
                                int.TryParse(SecondStrategyValueTextBox.Text, out int secondParam);

                                Trade(timer, firstParam, secondParam);
                            }));
                        }
                    }
                    Thread.Sleep(10000); //60 секунд простоя
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
                CandleInterval = Tinkoff.InvestApi.V1.CandleInterval._4Hour, // Доделать
                LotsQuantity = 1,  // Доделать
            });
        }

        private void GoBackButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.Visibility = Visibility.Visible;
            this.Visibility = Visibility.Collapsed;
        }

        private async void Trade(Timer timer, int firstParameter, int secondParameter)
        {
            var candles = await mainWindow.bot.GetCandlesListAsync(timer.Ticker, timer.CandleInterval);
            if(TinkoffInvestBot.ModifiedCalculateHeikinAshi(candles, firstParameter, secondParameter))
            {
                await Tinkoff_telegramm.MyTelegramBot.SendJaroslavMessage($"КУПИТЬ {timer.Ticker}");
                await Tinkoff_telegramm.MyTelegramBot.SendMeMessage($"КУПИТЬ {timer.Ticker}");
                await mainWindow.bot.PlaceOrderAsync(mainWindow.SelectedAccount, timer.Ticker, timer.LotsQuantity, OrderDirection.Buy);
            }
            else
            {
                await Tinkoff_telegramm.MyTelegramBot.SendJaroslavMessage($"Продать {timer.Ticker}");
                await Tinkoff_telegramm.MyTelegramBot.SendMeMessage($"Продать {timer.Ticker}");
                await mainWindow.bot.PlaceOrderAsync(mainWindow.SelectedAccount, timer.Ticker, timer.LotsQuantity, OrderDirection.Sell);
            }
        }

        private void DeleteTimerButton_Click(object sender, RoutedEventArgs e)
        {
            timers.Remove(timers[TimersListBox.SelectedIndex]);
        }
    }
}
