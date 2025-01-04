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
        public ObservableCollection<Account> AccountsList { get; set; } = new ObservableCollection<Account>();
        public Account CurrentAccount { get; set; } //Счет выбранный в ListBox
        public Account SelectedAccount { get; set; } //Счет выбранный для работы в главном окне

        public AddAccountWindow()
        {
            InitializeComponent();
            mainWindow = ((MainWindow)Application.Current.MainWindow);
            GetAccounts();
            AccountsListBox.ItemsSource = AccountsList;

            SelectedAccount = mainWindow.SelectedAccount;
            SelectedAccountComboBox.ItemsSource = AccountsList;
            if(SelectedAccount != null) SelectedAccountComboBox.SelectedItem = SelectedAccount;
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

        private async void GetAccounts()
        {
            AccountsList.Clear();
            var request = new GetAccountsRequest();
            var response = await mainWindow.apiClient.Users.GetAccountsAsync(request);
            if (response.Accounts.ToList().Count == 0)  //Если аккаунтов у токена нет, то добавляем пустышку для comboBox
            {
                AccountsList = new ObservableCollection<Account>();
            }
            foreach (var account in response.Accounts.ToList())
            {
                AccountsList.Add(account);
            }
            

        }

        private async void AccountsListBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            PortfolioTextBlock.Text = "";
            CurrentAccount = AccountsList[AccountsListBox.SelectedIndex];

            //WalletTextBlock.Text = 
            var portfolioRequest = new PortfolioRequest() { AccountId = CurrentAccount.Id };  //Обрабатывает портфель счета
            var portfolioResponse = await mainWindow.apiClient.Sandbox.GetSandboxPortfolioAsync(portfolioRequest);

            if(portfolioResponse.Positions.Any(p => p.InstrumentUid == "a92e2e25-a698-45cc-a781-167cf465257c"))
            {
                var money = portfolioResponse.Positions.First(p => p.InstrumentUid == "a92e2e25-a698-45cc-a781-167cf465257c");
                portfolioResponse.Positions.Remove(money);
                WalletTextBlock.Text = $"{money.Quantity.Units} Р.";
            }
            else
                WalletTextBlock.Text = $"0 Р.";
            foreach (var item in portfolioResponse.Positions)
            {
                PortfolioTextBlock.Text += $"{item.InstrumentUid} - {item.Quantity}" + "\n";
            }
            
        }

        private async void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            if(NewAccountNameTextBox.Text != string.Empty)
            {
                if (AccountsList.Count >= 10) { return; } //Если в песочнице 10 аккаунтов, то API не даст создать новые и выдаст ошибку "InvalidArgument 35001"
                var createAccountRequest = new OpenSandboxAccountRequest() { Name = NewAccountNameTextBox.Text, };
                var createAccountResponse = await mainWindow.apiClient.Sandbox.OpenSandboxAccountAsync(createAccountRequest);

                if(NewAccountMoneyTextBox.Text != string.Empty)
                {
                    decimal money = Convert.ToDecimal(NewAccountMoneyTextBox.Text);
                    var request = new SandboxPayInRequest
                    {
                        AccountId = createAccountResponse.AccountId,
                        Amount = money.ToMoneyValue()
                    };
                    var result = await mainWindow.apiClient.Sandbox.SandboxPayInAsync(request);
                }        

                MessageBox.Show("Счет успешно создан");
                GetAccounts();
            }
        }

        private void NewAccountMoneyTextBox_PreviewTextInput(object sender, TextCompositionEventArgs e)
        {
            Regex regex = new Regex("[^0-9]+");
            e.Handled = regex.IsMatch(e.Text);
        }

        private async void DeleteAccountButton_Click(object sender, RoutedEventArgs e)
        {
            var request = new CloseSandboxAccountRequest { AccountId = CurrentAccount.Id };
            var response = await mainWindow.apiClient.Sandbox.CloseSandboxAccountAsync(request);
            GetAccounts();

        }
        
        private void SelectedAccountComboBox_SelectionChanged(object sender, SelectionChangedEventArgs e)
        {
            SelectedAccount = AccountsList[SelectedAccountComboBox.SelectedIndex];
        }
    }
}
