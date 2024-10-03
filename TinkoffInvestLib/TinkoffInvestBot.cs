using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Runtime.CompilerServices;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using static Google.Protobuf.Compiler.CodeGeneratorResponse.Types;
using static Tinkoff.InvestApi.V1.OrderStateStreamResponse.Types;

namespace TinkoffInvestLib
{
    public class TinkoffInvestBot
    {
        /// <summary>Содержит экземпляр клиента</summary>
        private InvestApiClient Client { get; set; }
        /// <summary>Содержит список акций доступных для торговли через Tinkoff.InvestApi</summary>
        private List<Share> Shares { get; set; } = new List<Share>();
        /// <summary>Содержит список фьючерсов доступных для торговли через Tinkoff.InvestApi</summary>
        private List<Future> Futures { get; set; } = new List<Future>();
        /// <summary>Содержит список счетов в песочнице</summary>        
        public List<Account> Accounts { get; private set; } = new List<Account>();
        private string BotLogsFilePath = "BotLogs.txt";
        private string BotErrorsFilePath = "BotErrors.txt";

        private TinkoffInvestBot(string token)
        {
            var client = InvestApiClientFactory.Create(token, false);
            Client = client;  //Присваивает клиент после создания
        }

        /// <summary>
        /// Метод создает экземпляр объекта, обновляет список счетов, акций и фьючерсов, после чего возвращает его
        /// </summary>
        /// <returns>Экземпляр бота TinkoffInvestSandboxBot</returns>
        public static async Task<TinkoffInvestBot> CreateTinkoffInvestBotAsync(string token) //Создает объект бота и выполняет асинхронный метод
        {            
            var bot = new TinkoffInvestBot(token);
            bot.Accounts = await bot.UpdateAccountsAsync();
            bot.Shares = await bot.GetSharesAsync();
            bot.Futures = await bot.GetFuturesAsync();
            return bot;
        }

        /// <summary>
        /// Метод возвращает список аккаунтов
        /// </summary>
        /// <returns>Список аккаунтов</returns>
        private async Task<List<Account>> UpdateAccountsAsync()     
        {
            var request = new GetAccountsRequest();
            var response = await Client.Users.GetAccountsAsync(request);
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
        public async Task<string> GetAccountInfoAsync(Account account)
        {
            if (account == null) { return "Переданного счета не существует!"; } 
            string result = $"Информация по счету {account.Name} на {DateTime.Now}:\n";    //Создание строки ответа

            var ordersRequest = new GetOrdersRequest() { AccountId = account.Id};     //Обрабатывает активные заявки (торговые поручения)
            var ordersResponse = await Client.Orders.GetOrdersAsync(ordersRequest);
            result += GetOrdersInfoAsync(ordersResponse.Orders.ToList());

            var portfolioRequest = new PortfolioRequest() { AccountId = account.Id };  //Обрабатывает портфель счета
            var portfolioResponse = await Client.Operations.GetPortfolioAsync(portfolioRequest);
            result += GetPortfolioInfoAsync(portfolioResponse.Positions.ToList());
            result += "\tОбщая стоимость портфеля: " + portfolioResponse.TotalAmountPortfolio + "\n";

            return result ;
        }

        /// <summary>
        /// Метод возвращает доступный остаток для вывода средств 
        /// </summary>
        /// <param name="account">Счет для сбора информации</param>
        /// <returns>Сумма доступного возврата</returns>
        public async Task<decimal> GetWithdrawLimitAsync(Account account)
        {
            if (account == null) { return 0; } 
            var request = new WithdrawLimitsRequest { AccountId = account.Id };
            var response = await Client.Operations.GetWithdrawLimitsAsync(request);

            return response.Money.First().ToDecimal();
        }

        /// <summary>
        /// Метод продает все инструменты 
        /// </summary>
        /// <param name="account">Счет для продажи инструментов</param>
        /// <returns>true если получилось, false в случае неудачи</returns>
        public async Task<bool> SellAllInstrumentsAsync(Account account)
        {
            if (account == null) { return false; }
            var instruments = await GetPortfolioInstrumentsAsync(account);
            foreach (var instrument in instruments)
            {
                string ticker;
                if (Shares.Any(s => s.Uid == instrument.InstrumentUid))
                {
                    ticker = Shares.First(s => s.Uid == instrument.InstrumentUid).Ticker;
                }
                else if (Futures.Any(f => f.Uid == instrument.InstrumentUid))
                {
                    ticker = Futures.First(f => f.Uid == instrument.InstrumentUid).Ticker;
                }
                else continue;
                var quantity = Math.Abs(Convert.ToInt32(instrument.Quantity.Units));
                var orderDirection = quantity < 0 ? OrderDirection.Buy : OrderDirection.Sell;
                await PlaceOrderAsync(account, ticker, quantity, orderDirection);
            }
            return true;
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
                        result += $"\t{future.Ticker} - фьючерс | {future.Name}, цена за 1 - {position.CurrentPrice}, количество - {position.Quantity}\n";
                    }
                    else                                                        //Если торговое поручение неизвестно к чему относится 
                    {                                                           //То добавляет к результату обобщенное описание 
                        result += $"\t{position.PositionUid} - неизвестно | количество - {position.Quantity}, цена за 1 - {position.CurrentPrice}\n";
                    }
                }
            }
            return result;
        }

        /// <summary>
        /// Метод возвращает список позиций портфеля
        /// </summary>
        /// <param name="account">Счет для сбора позиций портфеля</param>
        /// <returns>Список позиций портфеля</returns>
        public async Task<List<PortfolioPosition>> GetPortfolioInstrumentsAsync(Account account)
        {
            var portfolioRequest = new PortfolioRequest() { AccountId = account.Id };  //Обрабатывает портфель счета
            var portfolioResponse = await Client.Operations.GetPortfolioAsync(portfolioRequest);
           return portfolioResponse.Positions.ToList();
        }

         /// <summary>
        /// Метод возвращает количество лотов инструмента в портфеле
        /// </summary>
        /// <param name="account">Счет для поулчения количества лотов</param>
        /// <param name="ticker">Тикер инструмента</param>
        /// <returns>Количество лотов инструмента в портфеле</returns>
        public async Task<int> GetLotsOfInstrumentAsync(Account account, string ticker)
        {
            string instrumentId;
            if (Shares.Any(s => s.Ticker.ToLower() == ticker.ToLower()))
            {
                instrumentId = Shares.First(s => s.Ticker.ToLower() == ticker.ToLower()).Uid;
            }
            else if (Futures.Any(s => s.Ticker.ToLower() == ticker.ToLower()))
            {
                instrumentId = Futures.First(s => s.Ticker.ToLower() == ticker.ToLower()).Uid;
            }
            else throw new Exception("Тикер не найден"!);

            var positions = await GetPortfolioInstrumentsAsync(account);
            foreach (var position in positions)
            {
                if (position.InstrumentUid == instrumentId)
                {
                    return Convert.ToInt32(position.QuantityLots.Units);
                }
            }
            return 0; //Если инструмент не найден в портфеле то возвращаем 0
        }

        /// <summary>
        /// Возвращает последнюю цену закрытия
        /// </summary>
        /// <param name="ticker">Тикер инструмента</param>
        /// <returns>Цена закрытия инструмента</returns>
        /// <exception cref="Exception"></exception>
        public async Task<decimal> GetCurrentPriceOfInstrumentAsync(string ticker)
        {
            try
            {
                string instrumentId;
                if (Shares.Any(s => s.Ticker.ToLower() == ticker.ToLower()))
                {
                    instrumentId = Shares.First(s => s.Ticker.ToLower() == ticker.ToLower()).Uid;
                }
                else if (Futures.Any(s => s.Ticker.ToLower() == ticker.ToLower()))
                {
                    instrumentId = Futures.First(s => s.Ticker.ToLower() == ticker.ToLower()).Uid;
                }
                else throw new Exception("Тикер не найден"!);

                var request = new GetLastPricesRequest() { InstrumentId = { instrumentId } };
                var response = await Client.MarketData.GetLastPricesAsync(request);
                return response.LastPrices.First().Price;
            }
            catch (RpcException e)
            {
                using (StreamWriter writer = new StreamWriter("BotErrors.txt", true))
                {
                    await writer.WriteLineAsync($"\tОшибка по тикеру {ticker} - " + e.ToString());
                }
                return 0;
            }
        }

        /// <summary>
        /// Метод выставляет заявку на покупку или продажу инструмента
        /// </summary>
        /// <param name="account">Счет от которого выставляется заявка</param>
        /// <param name="ticker">Тикер инструмента на который выставляется заявка</param>
        /// <param name="quantity">Количество лотов инструмента</param>
        /// <param name="direction">Покупка или продажа</param>
        /// <returns>true в случае успешного выставления заявки, иначе false</returns>
        public async Task<bool> PlaceOrderAsync(Account account, string ticker, int quantity, OrderDirection direction)
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
                var response = await Client.Orders.PostOrderAsync(request);
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

        /// <summary>
        /// Возвращает список свечей по инструменту
        /// </summary>
        /// <param name="ticker">Тикер инструмента</param>
        /// <param name="interval">Интервал свечей</param>
        /// <returns>Список свечей или null если тикер инструмента неправильный</returns>
        public async Task<List<HistoricCandle>> GetCandlesListAsync(string ticker, CandleInterval interval) //Возвращает список свечей по figi с момента from по to
        {
            DateTime from = interval switch
            {
                CandleInterval.Unspecified => DateTime.UtcNow.AddDays(-1),
                CandleInterval._1Min => DateTime.UtcNow.AddDays(-1),
                CandleInterval._2Min => DateTime.UtcNow.AddDays(-1),
                CandleInterval._3Min => DateTime.UtcNow.AddDays(-1),
                CandleInterval._5Min => DateTime.UtcNow.AddDays(-1),
                CandleInterval._10Min => DateTime.UtcNow.AddDays(-1),
                CandleInterval._15Min => DateTime.UtcNow.AddDays(-1),
                CandleInterval._30Min => DateTime.UtcNow.AddDays(-2),
                CandleInterval.Hour => DateTime.UtcNow.AddDays(-7),
                CandleInterval._2Hour => DateTime.UtcNow.AddDays(-30),
                CandleInterval._4Hour => DateTime.UtcNow.AddDays(-30),
                CandleInterval.Day => DateTime.UtcNow.AddDays(-365),
                CandleInterval.Week => DateTime.UtcNow.AddDays(-730),
                CandleInterval.Month => DateTime.UtcNow.AddDays(-3650),
                _ => DateTime.UtcNow.AddDays(-1)

            };
            DateTime to = DateTime.UtcNow;
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

        /// <summary>
        /// Метод рассчитывает EMA (экспоненциальные скользящие средние) по введенному списку цен и длине
        /// </summary>
        /// <param name="prices">Список цен</param>
        /// <param name="length">количество временных отрезков, за который считается средняя </param>
        /// <returns></returns>
        public static decimal CalculateEma(List<decimal> prices, int length)
        {
            int n = prices.Count > length ? length : prices.Count; //длина
            decimal k = (decimal)2 / (n + 1); //вес
            decimal ema = 0; //текущее ема
            var currentCandleCost = prices[prices.Count - n];

            decimal previousEma = currentCandleCost;
            decimal price = currentCandleCost;

            for (int i = n - 1; i > 0; i--)
            {
                currentCandleCost = prices[prices.Count - i];
                price = currentCandleCost;
                ema = (price * k) + (previousEma * (1 - k));
                previousEma = ema;
            }
            return ema;
        }

        /// <summary>
        /// Метод рассчитывает стоит ли продавать или покупать инструмент по индикаторам
        /// </summary>
        /// <param name="candles">Список свечей</param>
        /// <param name="length">Начальная длина</param>
        /// <param name="length2">Конечная длина</param>
        /// <returns>true если купить и false если продать</returns>
        public static bool CalculateHeikinAshi(List<HistoricCandle> candles, int length, int length2)
        {
            List<decimal> candlesOpenCosts = new List<decimal>();
            List<decimal> candlesCloseCosts = new List<decimal>();
            List<decimal> candlesHighCosts = new List<decimal>();
            List<decimal> candlesLowCosts = new List<decimal>();
            foreach (var item in candles)
            {
                candlesOpenCosts.Add(Convert.ToDecimal(item.Open));
                candlesCloseCosts.Add(Convert.ToDecimal(item.Close));
                candlesHighCosts.Add(Convert.ToDecimal(item.High));
                candlesLowCosts.Add(Convert.ToDecimal(item.Low));
            }
            var o = CalculateEma(candlesCloseCosts, length); //open - массив цен открытия
            var c = CalculateEma(candlesOpenCosts, length); //open - массив цен закрытия
            var h = CalculateEma(candlesHighCosts, length);
            var l = CalculateEma(candlesLowCosts, length);

            List<decimal> haOpen = new List<decimal>();
            List<decimal> haClose = new List<decimal>();
            List<decimal> haHigh = new List<decimal>();
            List<decimal> haLow = new List<decimal>();
            for (int i = 0; i < length; i++)
            {
                haClose.Add((o + h + l + c) / 4);
                if (haOpen.Count < 1) haOpen.Add((o + c) / 2);
                else haOpen.Add((haOpen[i - 1] + haClose[i]) / 2);

                haHigh.Add(decimal.Max(h, decimal.Max(haOpen[i], haClose[i])));
                haLow.Add(decimal.Min(l, decimal.Min(haOpen[i], haClose[i])));
            }

            var o2 = CalculateEma(haOpen, length2); //рассчитывается ЕМА по списку цен открытия
            var c2 = CalculateEma(haClose, length2);
            var h2 = CalculateEma(haHigh, length2);
            var l2 = CalculateEma(haLow, length2);

            return o2 > c2 ? false : true; // false - продать, true - купить
        }            

        /// <summary>
        /// Метод определяет по тикеру является ли инструмент фьючерсом
        /// </summary>
        /// <param name="ticker">Тикер инструмента</param>
        /// <returns>true если инструмент является фьючерсом, иначе false</returns>
        public bool? IsItFuture(string ticker)
        {
            if (ticker == null) return null;
            if (Futures.Any(f => f.Ticker.ToUpper() == ticker.ToUpper()))
                return true;
            else return false;
        }
    }
}
