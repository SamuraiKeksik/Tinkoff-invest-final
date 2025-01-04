using System.Collections.ObjectModel;
using System.Text;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Data;
using System.Windows.Documents;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using System.Windows.Threading;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;

namespace TinkoffInvestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public InvestApiClient apiClient;
        public bool isSandbox;
        public ObservableCollection<Share> SharesList { get; set; } = new ObservableCollection<Share>();
        public ObservableCollection<Future> FuturesList { get; set; } = new ObservableCollection<Future>();
        public List<Account> AccountsList { get; set; } = new List<Account>() { new Account { Name = "Пусто"} };
        public Account SelectedAccount { get; set; }

        public MainWindow()
        {
            InitializeComponent();

            //Проверка связи с серверами Тинькофф
            AuthanticationWindow window = new AuthanticationWindow();
            window.Show();          
            this.Visibility = Visibility.Hidden;  

        }

        public async void StartApp()
        {
            this.Visibility = Visibility.Visible;
            GetInstruments();
            GetAccounts();

            InstrumentsListBox.ItemsSource = FuturesList;
            InstrumentsListBox.ItemsSource = SharesList;
            SelectedAccount = AccountsList.First();
            AccountNameTextBlock.Text = SelectedAccount.Name;

            /* if (isSandbox == true)
                 StartSandbox();*/

        }

        private void StartSandbox()
        {
            throw new NotImplementedException();
        }

        public void SharesButton_Click(object sender, RoutedEventArgs e)
        {
            InstrumentsListBox.ItemsSource = SharesList;
        }

        private void FuturesButton_Click(object sender, RoutedEventArgs e)
        {
            InstrumentsListBox.ItemsSource = FuturesList;
        }


        private void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            AddAccountWindow window = new AddAccountWindow();
            window.Show();
            this.Visibility = Visibility.Hidden;
        }

        private async void GetInstruments()
        {
            var request = new InstrumentsRequest { InstrumentStatus = InstrumentStatus.Base };
            var sharesResponse = await apiClient.Instruments.SharesAsync(request);
            var futuresResponse = await apiClient.Instruments.FuturesAsync(request);

            SharesList.Clear();
            foreach (var share in sharesResponse.Instruments.ToList())
            {
                SharesList.Add(share);
            }

            FuturesList.Clear();
            foreach (var future in futuresResponse.Instruments.ToList())
            {
                FuturesList.Add(future);
            }
        }

        private async void GetAccounts()
        {
            var request = new GetAccountsRequest();
            var response = await apiClient.Users.GetAccountsAsync(request);
            if (response.Accounts.ToList().Count == 0)  //Если аккаунтов у токена нет, то добавляем пустышку для comboBox
            {
                AccountsList = new List<Account> { new Account { Name = "Пусто" } };
            }
            AccountsList = response.Accounts.ToList();
            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TimersButton_Click(object sender, RoutedEventArgs e)
        {
            /*AddTimerWindow window = new AddTimerWindow();
            window.Show();
            this.Visibility = Visibility.Hidden;*/
        }
    }



}
