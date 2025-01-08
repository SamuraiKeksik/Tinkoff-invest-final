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
using TinkoffInvestLib;

namespace TinkoffInvestApp
{
    /// <summary>
    /// Interaction logic for MainWindow.xaml
    /// </summary>
    public partial class MainWindow : Window
    {
        public TinkoffInvestBot bot;
        AddAccountWindow addAccountWindow { get; set; }
        AddTimerWindow addTimerWindow { get; set; }

        public bool isSandbox;
        public bool selectedInstrumentIsShare;
        public Account SelectedAccount { get; set; }

        public MainWindow()
        {
            InitializeComponent();            
            AuthanticationWindow authanticationWindow = new AuthanticationWindow(); //Проверка связи с серверами Тинькофф
            authanticationWindow.Show();          
            this.Visibility = Visibility.Hidden;  

        }

        public async void StartApp()
        {            
            this.Visibility = Visibility.Visible;
            SharesButton_Click(this, new RoutedEventArgs());


            /* if (isSandbox == true)
                 StartSandbox();*/
            addAccountWindow = new AddAccountWindow();
            addTimerWindow = new AddTimerWindow();

        }

        private void StartSandbox()
        {
            throw new NotImplementedException();
        }

        public void SharesButton_Click(object sender, RoutedEventArgs e)
        {
            InstrumentsListBox.ItemsSource = bot.Shares;
            selectedInstrumentIsShare = true;
        }

        private void FuturesButton_Click(object sender, RoutedEventArgs e)
        {
            InstrumentsListBox.ItemsSource = bot.Futures;
            selectedInstrumentIsShare = false;
        }

        private void FundsButton_Click(object sender, RoutedEventArgs e)
        {
            InstrumentsListBox.ItemsSource = bot.Funds;
            selectedInstrumentIsShare = true;
        }

        private void AddAccountButton_Click(object sender, RoutedEventArgs e)
        {
            AddAccountWindow window = new AddAccountWindow();
            window.Show();
            this.Visibility = Visibility.Hidden;
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        private void TimersButton_Click(object sender, RoutedEventArgs e)
        {
            addTimerWindow.Show();
            this.Visibility = Visibility.Hidden;
        }

    }



}
