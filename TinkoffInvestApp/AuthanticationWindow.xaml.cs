using Google.Api;
using System.Windows;
using Tinkoff.InvestApi;
using Visibility = System.Windows.Visibility;

namespace TinkoffInvestApp
{
    /// <summary>
    /// Логика взаимодействия для AuthanticationWindow.xaml
    /// </summary>
    public partial class AuthanticationWindow : Window
    {
        MainWindow mainWindow;
        public AuthanticationWindow()
        {
            mainWindow = ((MainWindow)Application.Current.MainWindow);
            InitializeComponent();

            TokenTextBox.Text = UsefulFunctions.GetRegistryKey("Token") != " r" ? UsefulFunctions.GetRegistryKey("Token") : "Введите свой токен";
            SandboxCheckBox.IsChecked = Convert.ToBoolean(UsefulFunctions.GetRegistryKey("IsSandbox"));

            var task = Task.Run(() =>
            {
                while (true)
                {
                    Dispatcher.Invoke(new Action(() => {
                        if (!UsefulFunctions.CheckTinkoffConnection() || !UsefulFunctions.CheckTinkoffSandboxConnection()) //Если соединения нет то показываетя скрытое сообщение об отсутствии соединения
                            ConnectionStatusGrid.Visibility = Visibility.Visible;
                        else
                            ConnectionStatusGrid.Visibility = Visibility.Hidden;    //Если соединение есть, то скрывает это сообщение                        
                    }));
                    Thread.Sleep(5000);
                }
            });
        }

        private void ExitButton_Click(object sender, RoutedEventArgs e)
        {
            Environment.Exit(0);
        }

        private async void StartButton_Click(object sender, RoutedEventArgs e)
        {
            ErrorTextBlock.Visibility = Visibility.Hidden;  //Скрываем текст ошибки
            bool isSandbox = SandboxCheckBox.IsChecked ?? false;   //Берем значение ЧекБокса для песочницы
            string token = TokenTextBox.Text;                   //Берем токен из текстБокса
            var client = InvestApiClientFactory.Create(token, isSandbox);   //Создаем клиента тьнькофф API

            if (isSandbox)  //Если токен для песочницы то выполняем простой запрос в песочнице для проверки его работоспособности
            {
                try
                {
                    await client.Sandbox.GetSandboxAccountsAsync(new Tinkoff.InvestApi.V1.GetAccountsRequest());
                }
                catch (Grpc.Core.RpcException exception)
                {
                    if (exception.StatusCode == Grpc.Core.StatusCode.Unauthenticated)
                    ErrorTextBlock.Visibility = Visibility.Visible;
                    ErrorTextBlock.Text = "Токен не является токеном песочницы!";
                    return;
                }                
            }
            else //Если токен не для песочницы то проверяем его функциями не для песочницы
            {
                try
                {
                    await client.Users.GetAccountsAsync(new Tinkoff.InvestApi.V1.GetAccountsRequest());
                }
                catch (Grpc.Core.RpcException exception)
                {
                    if (exception.StatusCode == Grpc.Core.StatusCode.Unauthenticated)
                        ErrorTextBlock.Visibility = Visibility.Visible;
                    ErrorTextBlock.Text = "Токен не является действительным";
                    return;
                }
            }

            UsefulFunctions.SetRegistryKey("Token", token); //Сохраняем токен в регистре
            UsefulFunctions.SetRegistryKey("IsSandbox", isSandbox.ToString()); //Сохраняем чекбокс песочницы

            mainWindow.apiClient = client;
            mainWindow.isSandbox = isSandbox;
            mainWindow.StartApp();
            this.Visibility = Visibility.Hidden;
            
        }

        private void Window_Closing(object sender, System.ComponentModel.CancelEventArgs e)
        {
            Application.Current.Shutdown();
        }
    }
}
