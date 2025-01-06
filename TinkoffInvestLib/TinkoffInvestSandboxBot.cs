using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
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
        protected override string BotLogsFilePath { get; set; } = "BotLogs.txt";
        protected override string BotErrorsFilePath { get; set; } = "BotErrors.txt";

        protected TinkoffInvestSandboxBot(string token) :base(token){}

        /// <summary>
        /// Метод возвращает список аккаунтов, открытых в песочнице
        /// </summary>
        /// <returns>Список аккаунтов</returns>
        protected override async Task<List<Account>> UpdateAccountsAsync()     
        {
            var request = new GetAccountsRequest();
            var response = await Client.Sandbox.GetSandboxAccountsAsync(request);
            Accounts = response.Accounts.ToList();
            return Accounts;
        }

        /// <summary>
        /// Метод собирает информацию об активных заявках и портфеле счета
        /// </summary>
        /// <param name="account">Счет для сбора информации</param>
        /// <returns>Строка с информацией о счете</returns>
        public override async Task<string> GetAccountInfoAsync(Account account)
        {
            if (account == null) { return "Переданного счета не существует!"; } 
            string result = $"Информация по счету {account.Name} на {DateTime.Now}:\n";    //Создание строки ответа

            var ordersRequest = new GetOrdersRequest() { AccountId = account.Id};     //Обрабатывает активные заявки (торговые поручения)
            var ordersResponse = await Client.Sandbox.GetSandboxOrdersAsync(ordersRequest);
            result += GetOrdersInfoAsync(ordersResponse.Orders.ToList());

            var portfolioRequest = new PortfolioRequest() { AccountId = account.Id };  //Обрабатывает портфель счета
            var portfolioResponse = await Client.Sandbox.GetSandboxPortfolioAsync(portfolioRequest);
            result += GetPortfolioInfoAsync(portfolioResponse.Positions.ToList());
            result += "\tОбщая стоимость портфеля: " + portfolioResponse.TotalAmountPortfolio + "\n";

            return result ;
        }

        /// <summary>
        /// Метод возвращает доступный остаток для вывода средств 
        /// </summary>
        /// <param name="account">Счет для сбора информации</param>
        /// <returns>Сумма доступного возврата</returns>
        public override async Task<decimal> GetWithdrawLimitAsync(Account account)
        {
            if (account == null) { return 0; } 
            var request = new WithdrawLimitsRequest { AccountId = account.Id };
            var response = await Client.Sandbox.GetSandboxWithdrawLimitsAsync(request);

            return response.Money.First().ToDecimal();
        }

        /// <summary>
        /// Метод возвращает список позиций портфеля
        /// </summary>
        /// <param name="account">Счет для сбора позиций портфеля</param>
        /// <returns>Список позиций портфеля</returns>
        public override async Task<List<PortfolioPosition>> GetPortfolioInstrumentsAsync(Account account)
        {
            var portfolioRequest = new PortfolioRequest() { AccountId = account.Id };  //Обрабатывает портфель счета
            var portfolioResponse = await Client.Sandbox.GetSandboxPortfolioAsync(portfolioRequest);
           return portfolioResponse.Positions.ToList();
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
            await UpdateAccountsAsync();
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
            await UpdateAccountsAsync();
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
        /// Метод очищает счет путем его удаления и созданием нового с тем же именем
        /// </summary>
        /// <param name="account">Экземпляр счета для очистки</param>
        /// <param name="money">Сумма для пополнения счета</param>
        /// <returns>true при успешном очищении счета, иначе false</returns>
        public async Task<bool> ClearSandboxAccountAsync(Account account, decimal money) //Метод зачисляет деньги на счет
        {
            if (!Accounts.Any(a => a.Id == account.Id)) //Если счет не найден в списке, то false
                return false;
            var accountName = account.Name;
            if (!await DeleteSandboxAccountAsync(account) || !await CreateSandboxAccountAsync(accountName))
                return false;
            if (!await PayInSandboxAccountAsync(Accounts.First(a => a.Name == accountName), money))
                return false;

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
                using (StreamWriter writer = new StreamWriter(BotErrorsFilePath, true))
                {
                    await writer.WriteLineAsync($"\tОшибка по тикеру {ticker} - " + e.ToString());
                }
                return false;
            }
        }   
    }
}
