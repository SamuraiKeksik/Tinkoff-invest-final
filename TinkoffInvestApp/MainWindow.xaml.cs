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
        public ObservableCollection<Share> SharesList { get; set; }
        public ObservableCollection<Future> FuturesList { get; set; }

        public class Example
        {
            public string Name { get; set; }
            public string Ticker { get; set; }
        }

        public MainWindow()
        {
            InitializeComponent();
            //Проверка связи с серверами Тинькофф
            AuthanticationWindow window = new AuthanticationWindow();
            window.Show();          
            this.Visibility = Visibility.Hidden;  

            FuturesList = new ObservableCollection<Future>();
            InstrumentsListBox.ItemsSource = FuturesList;

            SharesList = new ObservableCollection<Share>();
            InstrumentsListBox.ItemsSource = SharesList;


        }

        public void StartApp()
        {
            this.Visibility = Visibility.Visible;
            GetInstruments();
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

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }

        
    }



}
