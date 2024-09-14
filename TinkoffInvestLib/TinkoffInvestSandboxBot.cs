using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Runtime.CompilerServices;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using static Google.Protobuf.Compiler.CodeGeneratorResponse.Types;
using static Tinkoff.InvestApi.V1.OrderStateStreamResponse.Types;

namespace TinkoffInvestLibSandbox
{
    public class TinkoffInvestSandboxBot
    {
        /// <summary>Содержит экземпляр клиента</summary>
        private InvestApiClient Client { get; set; }
        /// <summary>Содержит список акций доступных для торговли через Tinkoff.InvestApi</summary>
        private List<Share> Shares { get; set; } = new List<Share>();
        /// <summary>Содержит список фьючерсов доступных для торговли через Tinkoff.InvestApi</summary>
        private List<Future> Futures { get; set; } = new List<Future>();
        /// <summary>Содержит список счетов в песочнице</summary>        
        public List<Account> Accounts { get; private set; } = new List<Account>();

        private TinkoffInvestSandboxBot(string token, bool isSandbox)
        {
            var client = InvestApiClientFactory.Create(token, isSandbox);
            Client = client;  //Присваивает клиент после создания
        }

        /// <summary>
        /// Метод создает экземпляр объекта, обновляет список счетов, акций и фьючерсов, после чего возвращает его
        /// </summary>
        /// <returns>Экземпляр бота TinkoffInvestSandboxBot</returns>
        public static async Task<TinkoffInvestSandboxBot> CreateTinkoffInvestBotAsync(string token, bool isSandbox) //Создает объект бота и выполняет асинхронный метод
        {            
            var bot = new TinkoffInvestSandboxBot(token, isSandbox);
            bot.Accounts = await bot.UpdateSandboxAccountsAsync();
            bot.Shares = await bot.GetSharesAsync();
            bot.Futures = await bot.GetFuturesAsync();
            return bot;
        }

        /// <summary>
        /// Метод возвращает список аккаунтов, открытых в песочнице
        /// </summary>
        /// <returns>Список аккаунтов</returns>
        private async Task<List<Account>> UpdateSandboxAccountsAsync()     
        {
            var request = new GetAccountsRequest();
            var response = await Client.Sandbox.GetSandboxAccountsAsync(request);
            Accounts = response.Accounts.ToList();
            return Accounts;
        }

        /// <summary>
        /// Метод возвращает список акций доступных для торговли на бирже
        /// </summary>
        /// <returns>Список акций</returns>
        private async Task<List<Share>> GetSharesAsync()   
        {
            var request = new InstrumentsRequest { InstrumentStatus = InstrumentStatus.Base };
            var response = await Client.Instruments.SharesAsync(request);
            Shares = response.Instruments.ToList();
            return Shares;
        }

        /// <summary>
        /// Метод возвращает список фьючерсов доступных для торговли на бирже
        /// </summary>
        /// <returns>Список фьючерсов</returns>
        private async Task<List<Future>> GetFuturesAsync()   
        {
            var request = new InstrumentsRequest { InstrumentStatus = InstrumentStatus.Base };
            var response = await Client.Instruments.FuturesAsync(request);
            Futures = response.Instruments.ToList();
            return Futures;
        }

        /// <summary>
        /// Метод собирает информацию об активных заявках и портфеле счета
        /// </summary>
        /// <param name="account">Счет для сбора информации</param>
        /// <returns>Строка с информацией о счете</returns>
        public async Task<string> GetSandboxAccountInfoAsync(Account account)
        {
            if (account == null) { return "Переданного счета не существует!"; } 
            string result = $"Информация по счету {account.Name} с id {account.Id} на {DateTime.Today} - {DateTime.Now}:\n";    //Создание строки ответа

            var ordersRequest = new GetOrdersRequest() { AccountId = account.Id};     //Обрабатывает активные заявки (торговые поручения)
            var ordersResponse = await Client.Sandbox.GetSandboxOrdersAsync(ordersRequest);
            result += GetOrdersInfoAsync(ordersResponse.Orders.ToList());

            var portfolioRequest = new PortfolioRequest() { AccountId = account.Id };  //Обрабатывает портфель счета
            var portfolioResponse = await Client.Sandbox.GetSandboxPortfolioAsync(portfolioRequest);
            result += GetPortfolioInfoAsync(portfolioResponse.Positions.ToList());

            return result ;
        }

        /// <summary>
        /// Метод возвращает информацию о торговых поручениях
        /// </summary>
        /// <param name="ordersStates">Список заявок торговых поручений</param>
        /// <returns>Строка с информацией о торговых поручениях</returns>
        private string GetOrdersInfoAsync(List<Tinkoff.InvestApi.V1.OrderState> ordersStates) //Возвращает информацию в виде строки о торговых поручениях
        {
            string result = String.Empty;
            if (ordersStates.Count == 0) { result += "\tАктивные торговые поручения отсутствуют!\n"; }
            else
            {
                foreach (var orderState in ordersStates)
                {
                    if (Shares.Any(s => s.Uid == orderState.InstrumentUid))     //Если торговое поручение относится к акциям 
                    {                                                           //То добавляет к результату описание для акции
                        var share = Shares.First(s => s.Uid == orderState.InstrumentUid);
                        result += $"\t{share.Ticker} - акция | Статус - {orderState.ExecutionReportStatus.ToString()}, {orderState.Direction}, итоговая сумма заявки - {orderState.TotalOrderAmount.ToString()}\n";
                    }
                    else if (Futures.Any(s => s.Uid == orderState.InstrumentUid))     //Если торговое поручение относится к фьючерсам 
                    {                                                           //То добавляет к результату описание для фьючерса
                        var future = Futures.First(s => s.Uid == orderState.InstrumentUid);
                        result += $"\t{future.Ticker} - фьючерс | Статус - {orderState.ExecutionReportStatus.ToString()}, {orderState.Direction}, итоговая сумма заявки - {orderState.TotalOrderAmount.ToString()}\n";
                    }
                    else                                                        //Если торговое поручение неизвестно к чему относится 
                    {                                                           //То добавляет к результату обобщенное описание 
                        result += $"\t{orderState.OrderId} - неизвестно | Статус - {orderState.ExecutionReportStatus.ToString()}, {orderState.Direction}, итоговая сумма заявки - {orderState.TotalOrderAmount.ToString()}\n";
                    }
                }
                result += "\n";
            }
            return result;
        }

        /// <summary>
        /// Метод возвращает информацию о торговых поручениях
        /// </summary>
        /// <param name="portfolioPositions">Список позиций портфолио</param>
        /// <returns>Строка с информацией о портфолио</returns>
        private string GetPortfolioInfoAsync(List<PortfolioPosition> portfolioPositions) //Возвращает информацию в виде строки о портфеле
        {
            string result = string.Empty;
            if (portfolioPositions.Count == 0) { result += "\tПортфель пуст!\n"; }
            else
            {
                foreach (var position in portfolioPositions)
                {
                    if (Shares.Any(s => s.Uid == position.InstrumentUid))     //Если торговое поручение относится к акциям 
                    {                                                           //То добавляет к результату описание для акции
                        var share = Shares.First(s => s.Uid == position.InstrumentUid);
                        result += $"\t{share.Ticker} - акция | {share.Name}, цена за 1 - {position.CurrentPrice}, количество - {position.Quantity}\n";
                    }
                    else if (Futures.Any(s => s.Uid == position.InstrumentUid))     //Если торговое поручение относится к фьючерсам 
                    {                                                           //То добавляет к результату описание для фьючерса
                        var future = Futures.First(s => s.Uid == position.InstrumentUid);
                        result += $"\t{future.Ticker} - фьючерс | {future.Name}, цена за 1 - {position.CurrentPrice}, количество - {position.Quantity}, вариационная маржа - {position.VarMargin}\n";
                    }
                    else                                                        //Если торговое поручение неизвестно к чему относится 
                    {                                                           //То добавляет к результату обобщенное описание 
                        result += $"\t{position.PositionUid} - неизвестно | количество - {position.Quantity}, цена за 1 - {position.CurrentPrice}";
                    }
                    result += "\n";
                }
            }
            return result;
        }

        /// <summary>
        /// Метод создает счет в песочнице
        /// </summary>
        /// <param name="accountName">Название счета</param>
        /// <returns>true при успешном создании счета, иначе false</returns>        
        public async Task<bool> CreateSandboxAccountAsync(string accountName)  //Создает счет в песочнице, если получилось то возвращает true, иначе false
        {
            if (Accounts.Count == 10) { return false; } //Если в песочнице 10 аккаунтов, то API не даст создать новые и выдаст ошибку "InvalidArgument 35001"
            var createAccountRequest = new OpenSandboxAccountRequest() { Name = accountName,};
            var createAccountResponse = await Client.Sandbox.OpenSandboxAccountAsync(createAccountRequest);
            await UpdateSandboxAccountsAsync();
            return true;

        }

        /// <summary>
        /// Метод удаляет счет в песочнице
        /// </summary>
        /// <param name="account">Экземпляр счета для удаления</param>
        /// <returns>true при успешном удалении счета, иначе false</returns>
        public async Task<bool> DeleteSandboxAccountAsync(Account account) 
        {
            if (Accounts.First(a => a.Id == account.Id) == null) //Если аккаунта нет, то возвращает false
                return false;

            var request = new CloseSandboxAccountRequest { AccountId = Accounts.First(a => a.Id == account.Id).Id };
            var response = await Client.Sandbox.CloseSandboxAccountAsync(request);
            await UpdateSandboxAccountsAsync();
            return true;    //Если аккаунта удален, то возвращает true
        }

        /// <summary>
        /// Метод пополнения счета в песочнице и для вычитания суммы со счета при использовании отрицательного аргумента
        /// </summary>
        /// <param name="account">Экземпляр счета для пополнения</param>
        /// <param name="money">Сумма для пополнения счета</param>
        /// <returns>true при успешном пополнении счета, иначе false</returns>
        public async Task<bool> PayInSandboxAccountAsync(Account account, decimal money) //Метод зачисляет деньги на счет
        {
            if (!Accounts.Any(a => a.Id == account.Id)) //Если счет не найден в списке, то false
                return false;
            var request = new SandboxPayInRequest
            {
                AccountId = account.Id,
                Amount = money.ToMoneyValue()
            };
            var result = await Client.Sandbox.SandboxPayInAsync(request);
            return true; //true при успешном пополнении счета
        }

        /// <summary>
        /// Метод выставляет заявку на покупку или продажу инструмента
        /// </summary>
        /// <param name="account">Счет от которого выставляется заявка</param>
        /// <param name="ticker">Тикер инструмента на который выставляется заявка</param>
        /// <param name="quantity">Количество лотов инструмента</param>
        /// <param name="direction">Покупка или продажа</param>
        /// <returns>true в случае успешного выставления заявки, иначе false</returns>
        private async Task<bool> PlaceOrderAsync(Account account, string ticker, int quantity, OrderDirection direction)
        {
            try
            {
                string instrumentId;
                if (!Accounts.Any(a => a.Id == account.Id)) return false;   //Если введенного аккаунта нет, то возвращает false
                if (Shares.Any(s => s.Ticker.ToUpper() == ticker.ToUpper()))
                    instrumentId = Shares.First(s => s.Ticker.ToUpper() == ticker.ToUpper()).Uid;
                else if (Futures.Any(s => s.Ticker.ToUpper() == ticker.ToUpper()))
                    instrumentId = Futures.First(s => s.Ticker.ToUpper() == ticker.ToUpper()).Uid;
                else return false;                  //Если введенный тикер не соответствует акциям и фьючерсам в списке, то возвращает false

                var request = new PostOrderRequest()
                {
                    AccountId = account.Id,
                    Direction = direction,
                    Quantity = quantity,
                    InstrumentId = instrumentId,
                    OrderType = OrderType.Bestprice
                };
                var response = await Client.Sandbox.PostSandboxOrderAsync(request);
                return true;    //Если заявка выставлена успешно, то возвращает true
            }
            catch (RpcException e)
            {
                return false;
            }
        }

        /// <summary>
        /// Возвращает список свечей по инструменту
        /// </summary>
        /// <param name="ticker">Тикер инструмента</param>
        /// <param name="from">Начальная дата</param>
        /// <param name="to">Конечная дата</param>
        /// <param name="interval">Интервал свечей</param>
        /// <returns>Список свечей или null если тикер инструмента неправильный</returns>
        public async Task<List<HistoricCandle>> GetSandboxCandlesList(string ticker, DateTime from, DateTime to, CandleInterval interval) //Возвращает список свечей по figi с момента from по to
        {
            string instrumentId = string.Empty;
            if (Shares.Any(s => s.Ticker.ToLower() == ticker.ToLower()))
            {
                instrumentId = Shares.First(s => s.Ticker.ToLower() == ticker.ToLower()).Uid;
            }
            else if (Futures.Any(f => f.Ticker.ToLower() == ticker.ToLower()))
            {
                instrumentId = Futures.First(s => s.Ticker.ToLower() == ticker.ToLower()).Uid;
            }
            else { return null; }

            var request = new GetCandlesRequest()
            {
                InstrumentId = instrumentId,
                From = from.ToTimestamp(),
                To = to.ToTimestamp(),
                Interval = interval
            };
            var response = await Client.MarketData.GetCandlesAsync(request);
            return response.Candles.ToList();
        }

    }
}
