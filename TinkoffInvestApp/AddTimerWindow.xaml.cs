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
        ObservableCollection<string> selectedTickersList = new ObservableCollection<string>();
        MainWindow mainWindow;

        public AddTimerWindow()
        {
            mainWindow = (MainWindow)Application.Current.MainWindow;
            InitializeComponent();
            SelectedStrategyComboBox.ItemsSource = Enum.GetValues(typeof(AvailableStrategiesEnum)).Cast<AvailableStrategiesEnum>(); 
            SelectedStrategyComboBox.SelectedIndex = 0;
            TimersListBox.ItemsSource = timers;
            TickersListBox.ItemsSource = mainWindow.bot.Shares;
            SelectedTickersListBox.ItemsSource = selectedTickersList;

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
            if (selectedTickersList.Count == 0) return;
            if (timers.Any(t => t.ExecutionTime == TimerTimePicker.Value.Value.TimeOfDay))  
            {
                timers.First(t => t.ExecutionTime == TimerTimePicker.Value.Value.TimeOfDay).TickersList.AddRange(selectedTickersList.ToList());
                TimersListBox.Items.Refresh();
            }
            else
            {
                timers.Add(new Timer
                {
                    TickersList = selectedTickersList.ToList(),
                    ExecutionTime = TimerTimePicker.Value.Value.TimeOfDay,
                    SendToTelegram = false,
                    Strategy = (AvailableStrategiesEnum)SelectedStrategyComboBox.SelectedIndex,
                    CandleInterval = Tinkoff.InvestApi.V1.CandleInterval._4Hour, // Доделать
                    LotsQuantity = 1,  // Доделать
                });
            }

            selectedTickersList.Clear();
        }

        private void GoBackButton_Click(object sender, RoutedEventArgs e)
        {
            mainWindow.Visibility = Visibility.Visible;
            this.Visibility = Visibility.Collapsed;
        }

        private async void Trade(Timer timer, int firstParameter, int secondParameter)
        {
            foreach (var ticker in timer.TickersList)
            {
                var candles = await mainWindow.bot.GetCandlesListAsync(ticker, timer.CandleInterval);
                if (TinkoffInvestBot.ModifiedCalculateHeikinAshi(candles, firstParameter, secondParameter))
                {
                    await Tinkoff_telegramm.MyTelegramBot.SendJaroslavMessage($"КУПИТЬ {ticker}");
                    await Tinkoff_telegramm.MyTelegramBot.SendMeMessage($"КУПИТЬ {ticker}");
                    await mainWindow.bot.PlaceOrderAsync(mainWindow.SelectedAccount, ticker, timer.LotsQuantity, OrderDirection.Buy);
                }
                else
                {
                    await Tinkoff_telegramm.MyTelegramBot.SendJaroslavMessage($"Продать {ticker}");
                    await Tinkoff_telegramm.MyTelegramBot.SendMeMessage($"Продать {ticker}");
                    await mainWindow.bot.PlaceOrderAsync(mainWindow.SelectedAccount, ticker, timer.LotsQuantity, OrderDirection.Sell);
                }
            }
            
        }

        private void DeleteTimerButton_Click(object sender, RoutedEventArgs e)
        {
            timers.Remove(timers[TimersListBox.SelectedIndex]);
        }

        private void SelectTickersButton_Click(object sender, RoutedEventArgs e)
        {
            foreach (var item in TickersListBox.SelectedItems)
            {
                var share = item as Share;
                var future = item as Future;
                var fund = item as Etf;
                if (share != null && mainWindow.bot.Shares.Any(t => t.Ticker == share.Ticker))
                {
                    if (selectedTickersList.Any(t => t == share.Ticker))
                        selectedTickersList.Remove(share.Ticker);
                    else
                        selectedTickersList.Add(share.Ticker);
                }
                else if (future != null && mainWindow.bot.Futures.Any(t => t.Ticker == future.Ticker))
                {
                    if (selectedTickersList.Any(t => t == future.Ticker))
                        selectedTickersList.Remove(future.Ticker);
                    else
                        selectedTickersList.Add(future.Ticker);
                }
                else if (fund != null && mainWindow.bot.Funds.Any(t => t.Ticker == fund.Ticker))
                {
                    if (selectedTickersList.Any(t => t == fund.Ticker))
                        selectedTickersList.Remove(fund.Ticker);
                    else
                        selectedTickersList.Add(fund.Ticker);
                }
            }            

            TickersListBox.SelectedItems.Clear();
            
        }

        private void SharesButton_Click(object sender, RoutedEventArgs e)
        {
            TickersListBox.ItemsSource = mainWindow.bot.Shares;
        }

        private void FuturesButton_Click(object sender, RoutedEventArgs e)
        {
            TickersListBox.ItemsSource = mainWindow.bot.Futures;
        }

        private void FundsButton_Click(object sender, RoutedEventArgs e)
        {
            TickersListBox.ItemsSource = mainWindow.bot.Funds;
        }
    }
}
