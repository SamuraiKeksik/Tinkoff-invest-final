using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Serilog;
using System.Diagnostics.Metrics;
using System.Runtime.CompilerServices;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using static Google.Protobuf.Compiler.CodeGeneratorResponse.Types;
using static Tinkoff.InvestApi.V1.OrderStateStreamResponse.Types;

namespace TinkoffInvestLib
{
    public class TinkoffInvestSandboxBot : TinkoffInvestBot
    {
        protected TinkoffInvestSandboxBot(string token, bool isSandbox) : base(token, isSandbox){}

        /// <summary>
        /// Метод создает экземпляр объекта, обновляет список счетов, акций и фьючерсов, после чего возвращает его
        /// </summary>
        /// <returns>Экземпляр бота TinkoffInvestSandboxBot</returns>
        public new static async Task<TinkoffInvestBot> CreateTinkoffInvestBotAsync(string token, Serilog.ILogger logger) //Создает объект бота и выполняет асинхронный метод
        {
            var bot = new TinkoffInvestSandboxBot(token, true);
            Log.Logger = logger;
            bot.Accounts = await bot.UpdateAccountsAsync();
            bot.Shares = await bot.GetSharesAsync();
            bot.Futures = await bot.GetFuturesAsync();
            bot.Funds = await bot.GetFundsAsync();
            return bot;
        }
        /// <summary>
        /// Метод возвращает список аккаунтов, открытых в песочнице
        /// </summary>
        /// <returns>Список аккаунтов</returns>
        public override async Task<List<Account>> UpdateAccountsAsync()     
        {
            Log.Information("Вызов UpdateAccountsAsync()");

            var request = new GetAccountsRequest();
            var response = await Client.Sandbox.GetSandboxAccountsAsync(request);
            Accounts = response.Accounts.ToList();

            Log.Information("Получил список счетов: {list}", Accounts);
            return Accounts;
        }

        /// <summary>
        /// Метод собирает информацию об активных заявках и портфеле счета
        /// </summary>
        /// <param name="account">Счет для сбора информации</param>
        /// <returns>Строка с информацией о счете</returns>
        public override async Task<string> GetAccountInfoAsync(Account account)
        {
            Log.Information("Вызов GetAccountInfoAsync()");
            if (account == null) { return "Переданного счета не существует!"; } 
            string result = $"Информация по счету {account.Name} на {DateTime.Now}:\n";    //Создание строки ответа

            var ordersRequest = new GetOrdersRequest() { AccountId = account.Id};     //Обрабатывает активные заявки (торговые поручения)
            var ordersResponse = await Client.Sandbox.GetSandboxOrdersAsync(ordersRequest);
            result += GetOrdersInfoAsync(ordersResponse.Orders.ToList());

            var portfolioRequest = new PortfolioRequest() { AccountId = account.Id };  //Обрабатывает портфель счета
            var portfolioResponse = await Client.Sandbox.GetSandboxPortfolioAsync(portfolioRequest);
            result += GetPortfolioInfoAsync(portfolioResponse.Positions.ToList());
            result += "\tОбщая стоимость портфеля: " + portfolioResponse.TotalAmountPortfolio + "\n";

            Log.Information("Получил информацию о счете: {0}", result);
            return result ;
        }

        /// <summary>
        /// Метод возвращает доступный остаток для вывода средств 
        /// </summary>
        /// <param name="account">Счет для сбора информации</param>
        /// <returns>Сумма доступного возврата</returns>
        public override async Task<decimal> GetWithdrawLimitAsync(Account account)
        {
            Log.Information("Вызов GetWithdrawLimitAsync()");
            if (account == null) { return 0; } 
            var request = new WithdrawLimitsRequest { AccountId = account.Id };
            var response = await Client.Sandbox.GetSandboxWithdrawLimitsAsync(request);

            Log.Information("Получил остаток для вывода средств: {0}", response.Money.First().ToDecimal());
            return response.Money.First().ToDecimal();
        }

        /// <summary>
        /// Метод возвращает список позиций портфеля
        /// </summary>
        /// <param name="account">Счет для сбора позиций портфеля</param>
        /// <returns>Список позиций портфеля</returns>
        public override async Task<List<PortfolioPosition>> GetPortfolioInstrumentsAsync(Account account)
        {
            Log.Information("Вызов GetPortfolioInstrumentsAsync()");
            var portfolioRequest = new PortfolioRequest() { AccountId = account.Id };  //Обрабатывает портфель счета
            var portfolioResponse = await Client.Sandbox.GetSandboxPortfolioAsync(portfolioRequest);
           
            Log.Information("Получил список позиций портфеля {0}", portfolioResponse.Positions.ToList());
            return portfolioResponse.Positions.ToList();
        }

        /// <summary>
        /// Метод создает счет в песочнице
        /// </summary>
        /// <param name="accountName">Название счета</param>
        /// <returns>true при успешном создании счета, иначе false</returns>        
        public async Task<bool> CreateSandboxAccountAsync(string accountName)  //Создает счет в песочнице, если получилось то возвращает true, иначе false
        {
            Log.Information("Вызов CreateSandboxAccountAsync()");
            if (Accounts.Count == 10)
            {
                Log.Warning("В песочнице уже 10 счетов. Создание нового аккаунта прервано. return вернул false");
                return false; 
            } //Если в песочнице 10 аккаунтов, то API не даст создать новые и выдаст ошибку "InvalidArgument 35001"
            var createAccountRequest = new OpenSandboxAccountRequest() { Name = accountName,};
            var createAccountResponse = await Client.Sandbox.OpenSandboxAccountAsync(createAccountRequest);
            await UpdateAccountsAsync();

            Log.Information("Счет успешно создан");
            return true;

        }

        /// <summary>
        /// Метод удаляет счет в песочнице
        /// </summary>
        /// <param name="account">Экземпляр счета для удаления</param>
        /// <returns>true при успешном удалении счета, иначе false</returns>
        public async Task<bool> DeleteSandboxAccountAsync(Account account)
        {
            Log.Information("Вызов DeleteSandboxAccountAsync()");
            if (Accounts.First(a => a.Id == account.Id) == null) //Если аккаунта нет, то возвращает false
            {
                Log.Warning("Счет с id {0} и именем {1} отсутствует. return вернул false", account.Id, account.Name);
                return false; 
            }

            var request = new CloseSandboxAccountRequest { AccountId = Accounts.First(a => a.Id == account.Id).Id };
            var response = await Client.Sandbox.CloseSandboxAccountAsync(request);
            await UpdateAccountsAsync();
           
            Log.Information("Счет с именем {0} успешно удален. return вернул true", account.Name);
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
            Log.Information("Вызов PayInSandboxAccountAsync()");
            if (!Accounts.Any(a => a.Id == account.Id)) //Если счет не найден в списке, то false
            {
                Log.Warning("Счет с id {0} и именем {1} отсутствует. return вернул false", account.Id, account.Name);
                return false; 
            }
            var request = new SandboxPayInRequest
            {
                AccountId = account.Id,
                Amount = money.ToMoneyValue()
            };
            var result = await Client.Sandbox.SandboxPayInAsync(request);

            Log.Information("Счет с именем {0} успешно пополнен на {1} рублей. return вернул true", account.Name, money);
            return true; //true при успешном пополнении счета
        }

        /// <summary>
        /// Метод очищает счет путем его удаления и созданием нового с тем же именем
        /// </summary>
        /// <param name="account">Экземпляр счета для очистки</param>
        /// <param name="money">Сумма для пополнения счета</param>
        /// <returns>true при успешном очищении счета, иначе false</returns>
        public async Task<bool> ClearSandboxAccountAsync(Account account, decimal money) //Метод зачисляет деньги на счет
        {
            Log.Information("Вызов ClearSandboxAccountAsync()");
            if (!Accounts.Any(a => a.Id == account.Id)) //Если счет не найден в списке, то false
            {
                Log.Warning("Счет с id {0} и именем {1} отсутствует. return вернул false", account.Id, account.Name);
                return false;
            }
            var accountName = account.Name;
            if (!await DeleteSandboxAccountAsync(account) || !await CreateSandboxAccountAsync(accountName))
                return false;
            if (!await PayInSandboxAccountAsync(Accounts.First(a => a.Name == accountName), money))
                return false;

            Log.Information("Счет с именем {0} успешно очищен. return вернул true", account.Id, account.Name);
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
        public override async Task<bool> PlaceOrderAsync(Account account, string ticker, int quantity, OrderDirection direction)
        {
            Log.Information("Вызов PlaceOrderAsync()");
            try
            {
                string instrumentId;
                if (!Accounts.Any(a => a.Id == account.Id)) return false;   //Если введенного аккаунта нет, то возвращает false
                if (Shares.Any(s => s.Ticker.ToUpper() == ticker.ToUpper()))
                    instrumentId = Shares.First(s => s.Ticker.ToUpper() == ticker.ToUpper()).Uid;
                else if (Futures.Any(s => s.Ticker.ToUpper() == ticker.ToUpper()))
                    instrumentId = Futures.First(s => s.Ticker.ToUpper() == ticker.ToUpper()).Uid;
                else if (Funds.Any(s => s.Ticker.ToUpper() == ticker.ToUpper()))
                    instrumentId = Funds.First(s => s.Ticker.ToUpper() == ticker.ToUpper()).Uid;
                else
                {
                    Log.Information("Тикер {0} не найден, return вернул: {1}", ticker, false);
                    return false; //Если введенный тикер не соответствует акциям и фьючерсам в списке, то возвращает false
                }             

                var request = new PostOrderRequest()
                {
                    AccountId = account.Id,
                    Direction = direction,
                    Quantity = quantity,
                    InstrumentId = instrumentId,
                    OrderType = OrderType.Bestprice
                };
                var response = await Client.Sandbox.PostSandboxOrderAsync(request);

                Log.Information("Заявка на тикер {0} выставлена успешно. Направление: {1}, количество: {2}, тип:{3}", ticker, direction, quantity, OrderType.Bestprice);
                return true;    //Если заявка выставлена успешно, то возвращает true
            }
            catch (RpcException e)
            {
                Log.Error("Ошибка по тикеру {0} - {1}, return вернул 0", ticker, e.ToString());
                return false;
            }
        }   
    }
}
