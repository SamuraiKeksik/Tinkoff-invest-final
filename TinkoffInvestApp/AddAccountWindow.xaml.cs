using Google.Type;
using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Linq;
using System.Security.Policy;
using System.Security.Principal;
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
using System.Windows.Shapes;
using Tinkoff.InvestApi.V1;
using TinkoffInvestLib;

namespace TinkoffInvestApp
{
    /// <summary>
    /// Логика взаимодействия для AddAccountWindow.xaml
    /// </summary>
    public partial class AddAccountWindow : Window
    {
        MainWindow mainWindow;
        public Account CurrentAccount { get; set; } //Счет выбранный в ListBox
        public Account SelectedAccount { get; set; } //Счет выбранный для работы в главном окне

        public AddAccountWindow()
        {
            InitializeComponent();
            StartWindow();
            AccountsListBox.ItemsSource = mainWindow.bot.Accounts;

            SelectedAccount = mainWindow.SelectedAccount;
            SelectedAccountComboBox.ItemsSource = mainWindow.bot.Accounts;
            SelectedAccountComboBox.SelectedItem = mainWindow.SelectedAccount != null ? mainWindow.SelectedAccount : null;

            var task = Task.Run(() =>
            {
                while (true)
                {
                    Dispatcher.Invoke(new Action(() => {
                        AccountsListBox.Items.Refresh();
                    }));
                    Thread.Sleep(3000);
                }
            });
        }

        private async void StartWindow()
        {
            mainWindow = (MainWindow)Application.Current.MainWindow;
            await mainWindow.bot.UpdateAccountsAsync();
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void GoBackButton_Click(object sender, RoutedEventArgs e)
        {
            if(SelectedAccount != null) {mainWindow.SelectedAccount = SelectedAccount; }
            mainWindow.AccountNameTextBlock.Text = mainWindow.SelectedAccount.Name;
            mainWindow.Visibility = Visibility.Visible;
            this.Visibility = Visibility.Hidden;
        }                   

        private async void AccountsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PortfolioTextBlock.Text = "";
            CurrentAccount = mainWindow.bot.Accounts[AccountsListBox.SelectedIndex];
            var portfolio = await mainWindow.bot.GetPortfolioInstrumentsAsync(CurrentAccount);

            if(portfolio.Any(p => p.InstrumentUid == "a92e2e25-a698-45cc-a781-167cf465257c"))
            {
                var money = portfolio.First(p => p.InstrumentUid == "a92e2e25-a698-45cc-a781-167cf465257c");
                portfolio.Remove(money);
                WalletTextBlock.Text = $"{money.Quantity.Units} Р.";
            }
            else
                WalletTextBlock.Text = $"0 Р.";
            foreach (var item in portfolio)
            {
                PortfolioTextBlock.Text += $"{item.InstrumentUid} - {item.Quantity}" + "\n";
            }
            
        }

        private async void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            if(NewAccountNameTextBox.Text != string.Empty)
            {
                if (mainWindow.bot.Accounts.Count >= 10) { return; } //Если в песочнице 10 аккаунтов, то API не даст создать новые и выдаст ошибку "InvalidArgument 35001"
                await ((TinkoffInvestSandboxBot)mainWindow.bot).CreateSandboxAccountAsync(NewAccountNameTextBox.Text);

                if (NewAccountMoneyTextBox.Text != string.Empty)
                {
                    decimal money = Convert.ToDecimal(NewAccountMoneyTextBox.Text);
                    await ((TinkoffInvestSandboxBot)mainWindow.bot).PayInSandboxAccountAsync(mainWindow.bot.Accounts.First(a => a.Name == NewAccountNameTextBox.Text), money);
                }

                await mainWindow.bot.UpdateAccountsAsync();
            }
        }

        private void NewAccountMoneyTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private async void DeleteAccountButton_Click(object sender, RoutedEventArgs e)
        {
            await ((TinkoffInvestSandboxBot)mainWindow.bot).DeleteSandboxAccountAsync(CurrentAccount);
            await mainWindow.bot.UpdateAccountsAsync();
        }
        
        private void SelectedAccountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedAccount = mainWindow.bot.Accounts[SelectedAccountComboBox.SelectedIndex];
        }
    }
}
