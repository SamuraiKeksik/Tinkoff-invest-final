using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using Grpc.Core;
using Microsoft.Extensions.Logging;
using Serilog;
using Serilog.Core;
using System;
using System.Diagnostics;
using System.Diagnostics.Metrics;
using System.Linq;
using System.Reflection.Metadata;
using System.Runtime.CompilerServices;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using static Google.Protobuf.Compiler.CodeGeneratorResponse.Types;
using static Google.Rpc.Context.AttributeContext.Types;
using static Tinkoff.InvestApi.V1.OrderStateStreamResponse.Types;

namespace TinkoffInvestLib
{
    public class TinkoffInvestBot
    {
        /// <summary>Содержит экземпляр клиента</summary>
        protected InvestApiClient Client { get; set; }
        /// <summary>Содержит список акций доступных для торговли через Tinkoff.InvestApi</summary>
        public List<Share> Shares { get; protected set; } = new List<Share>();
        /// <summary>Содержит список фьючерсов доступных для торговли через Tinkoff.InvestApi</summary>
        public List<Future> Futures { get; protected set; } = new List<Future>();
        /// <summary>Содержит список фондов доступных для торговли через Tinkoff.InvestApi</summary>
        public List<Etf> Funds { get; protected set; } = new List<Etf>();
        /// <summary>Содержит список счетов в песочнице</summary>        
        public List<Account> Accounts { get; protected set; } = new List<Account>();

        protected TinkoffInvestBot(string token, bool isSandbox)
        {
            var client = InvestApiClientFactory.Create(token, false);
            Client = client;  //Присваивает клиент после создания
        }

        /// <summary>
        /// Метод создает экземпляр объекта, обновляет список счетов, акций и фьючерсов, после чего возвращает его
        /// </summary>
        /// <returns>Экземпляр бота TinkoffInvestSandboxBot</returns>
        public static async Task<TinkoffInvestBot> CreateTinkoffInvestBotAsync(string token, Serilog.ILogger logger) //Создает объект бота и выполняет асинхронный метод
        {
            var bot = new TinkoffInvestBot(token, false);
            Log.Logger = logger;
            bot.Accounts = await bot.UpdateAccountsAsync();
            bot.Shares = await bot.GetSharesAsync();
            bot.Futures = await bot.GetFuturesAsync();
            bot.Funds = await bot.GetFundsAsync();
            return bot;
        }

        /// <summary>
        /// Метод возвращает список аккаунтов
        /// </summary>
        /// <returns>Список аккаунтов</returns>
        public virtual async Task<List<Account>> UpdateAccountsAsync()     
        {
            Log.Information("Вызов UpdateAccountsAsync()");

            var request = new GetAccountsRequest();
            var response = await Client.Users.GetAccountsAsync(request);
            Accounts = response.Accounts.ToList();

            Log.Information("Получил список счетов: {list}", Accounts);
            return Accounts;
        }

        /// <summary>
        /// Метод возвращает список акций доступных для торговли на бирже
        /// </summary>
        /// <returns>Список акций</returns>
        protected async Task<List<Share>> GetSharesAsync()
        {
            Log.Information("Вызов GetSharesAsync()");

            var request = new InstrumentsRequest { InstrumentStatus = InstrumentStatus.Base };
            var response = await Client.Instruments.SharesAsync(request);
            Shares = response.Instruments.OrderBy(i => i.Ticker).ToList();

            Log.Information("Получил список акций: {0} штук", Shares.Count);
            return Shares;
        }

        /// <summary>
        /// Метод возвращает список фьючерсов доступных для торговли на бирже
        /// </summary>
        /// <returns>Список фьючерсов</returns>
        protected async Task<List<Future>> GetFuturesAsync()
        {
            Log.Information("Вызов GetFuturesAsync()"); ;

            var request = new InstrumentsRequest { InstrumentStatus = InstrumentStatus.Base };
            var response = await Client.Instruments.FuturesAsync(request);
            Futures = response.Instruments.OrderBy(i => i.Ticker).ToList();

            Log.Information("Получил список фьючерсов: {0} штук", Futures.Count);
            return Futures;
        }
        /// <summary>
        /// Метод возвращает список фьючерсов доступных для торговли на бирже
        /// </summary>
        /// <returns>Список фьючерсов</returns>
        protected async Task<List<Etf>> GetFundsAsync()   
        {
            Log.Information("Вызов GetFundsAsync()");

            var request = new InstrumentsRequest { InstrumentStatus = InstrumentStatus.Base };
            var response = await Client.Instruments.EtfsAsync(request);
            Funds = response.Instruments.OrderBy(i => i.Ticker).ToList();

            Log.Information("Получил список фондов: {0}", Funds.Count);
            return Funds;
        }

        public async void UpdateInstrumentsLists()
        {
            Log.Information("Вызов UpdateInstrumentsLists()");
            await GetSharesAsync();
            await GetFuturesAsync();
            await GetFundsAsync();
        }

        /// <summary>
        /// Метод собирает информацию об активных заявках и портфеле счета
        /// </summary>
        /// <param name="account">Счет для сбора информации</param>
        /// <returns>Строка с информацией о счете</returns>
        public async virtual Task<string> GetAccountInfoAsync(Account account)
        {
            Log.Information("Вызов GetAccountInfoAsync()");
            if (account == null) { return "Переданного счета не существует!"; } 
            string result = $"Информация по счету {account.Name} на {DateTime.Now}:\n";    //Создание строки ответа

            var ordersRequest = new GetOrdersRequest() { AccountId = account.Id};     //Обрабатывает активные заявки (торговые поручения)
            var ordersResponse = await Client.Orders.GetOrdersAsync(ordersRequest);
            result += GetOrdersInfoAsync(ordersResponse.Orders.ToList());

            var portfolioRequest = new PortfolioRequest() { AccountId = account.Id };  //Обрабатывает портфель счета
            var portfolioResponse = await Client.Operations.GetPortfolioAsync(portfolioRequest);
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
        public virtual async Task<decimal> GetWithdrawLimitAsync(Account account)
        {
            Log.Information("Вызов GetWithdrawLimitAsync()");
            if (account == null) { return 0; } 
            var request = new WithdrawLimitsRequest { AccountId = account.Id };
            var response = await Client.Operations.GetWithdrawLimitsAsync(request);

            Log.Information("Получил остаток для вывода средств: {0}", response.Money.First().ToDecimal());
            return response.Money.First().ToDecimal();
        }

        /// <summary>
        /// Метод продает все инструменты 
        /// </summary>
        /// <param name="account">Счет для продажи инструментов</param>
        /// <returns>true если получилось, false в случае неудачи</returns>
        public async Task<bool> SellAllInstrumentsAsync(Account account)
        {
            Log.Information("Вызов SellAllInstrumentsAsync()");

            if (account == null) 
            {
                Log.Warning("не найден счет для продажи инструментов");
                return false; 
            }
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

            Log.Information("Успешно выставлена заявка на продажу всех инструментов");
            return true;
        }



        /// <summary>
        /// Метод возвращает информацию о торговых поручениях
        /// </summary>
        /// <param name="ordersStates">Список заявок торговых поручений</param>
        /// <returns>Строка с информацией о торговых поручениях</returns>
        protected string GetOrdersInfoAsync(List<Tinkoff.InvestApi.V1.OrderState> ordersStates) //Возвращает информацию в виде строки о торговых поручениях
        {
            Log.Information("Вызов GetOrdersInfoAsync()");
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

            Log.Information("Получил информацию об активных поручениях: {0}", result);
            return result;
        }

        /// <summary>
        /// Метод возвращает информацию о торговых поручениях
        /// </summary>
        /// <param name="portfolioPositions">Список позиций портфолио</param>
        /// <returns>Строка с информацией о портфолио</returns>
        protected string GetPortfolioInfoAsync(List<PortfolioPosition> portfolioPositions) //Возвращает информацию в виде строки о портфеле
        {
            Log.Information("Вызов GetPortfolioInfoAsync()");
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
                    else if (Funds.Any(s => s.Uid == position.InstrumentUid))     //Если торговое поручение относится к фондам 
                    {                                                           //То добавляет к результату описание для фонда
                        var fund = Funds.First(s => s.Uid == position.InstrumentUid);
                        result += $"\t{fund.Ticker} - фонд | {fund.Name}, цена за 1 - {position.CurrentPrice}, количество - {position.Quantity}\n";
                    }
                    else                                                        //Если торговое поручение неизвестно к чему относится 
                    {                                                           //То добавляет к результату обобщенное описание 
                        result += $"\t{position.PositionUid} - неизвестно | количество - {position.Quantity}, цена за 1 - {position.CurrentPrice}\n";
                    }
                }
            }
            Log.Information("Получил информацию о портфеле счета {0}", result);
            return result;
        }

        /// <summary>
        /// Метод возвращает список позиций портфеля
        /// </summary>
        /// <param name="account">Счет для сбора позиций портфеля</param>
        /// <returns>Список позиций портфеля</returns>
        public virtual async Task<List<PortfolioPosition>> GetPortfolioInstrumentsAsync(Account account)
        {
            Log.Information("Вызов GetPortfolioInstrumentsAsync()");
            var portfolioRequest = new PortfolioRequest() { AccountId = account.Id };  //Обрабатывает портфель счета
            var portfolioResponse = await Client.Operations.GetPortfolioAsync(portfolioRequest);

            Log.Information("Получил список позиций портфеля {0}", portfolioResponse.Positions.ToList());
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
            Log.Information("Вызов GetLotsOfInstrumentAsync()");

            string instrumentId;
            if (Shares.Any(s => s.Ticker.ToLower() == ticker.ToLower()))
            {
                instrumentId = Shares.First(s => s.Ticker.ToLower() == ticker.ToLower()).Uid;
            }
            else if (Futures.Any(s => s.Ticker.ToLower() == ticker.ToLower()))
            {
                instrumentId = Futures.First(s => s.Ticker.ToLower() == ticker.ToLower()).Uid;
            }
            else if (Funds.Any(s => s.Ticker.ToLower() == ticker.ToLower()))
            {
                instrumentId = Funds.First(s => s.Ticker.ToLower() == ticker.ToLower()).Uid;
            }
            else 
            {
                Log.Information("Не найден тикер {0}, return вернул 0");
                return 0;
            }

            var positions = await GetPortfolioInstrumentsAsync(account);
            foreach (var position in positions)
            {
                if (position.InstrumentUid == instrumentId)
                {
                    Log.Information("Тикер {0} найден, кол-во лотов: {1}", ticker, position.QuantityLots.Units);
                    return Convert.ToInt32(position.QuantityLots.Units);
                }
            }

            Log.Warning("Не найден тикер {0} в портфеле, return вернул 0", ticker);
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
            Log.Information("Вызов GetCurrentPriceOfInstrumentAsync()");
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
                else if (Funds.Any(s => s.Ticker.ToLower() == ticker.ToLower()))
                {
                    instrumentId = Funds.First(s => s.Ticker.ToLower() == ticker.ToLower()).Uid;
                }
                else 
                {
                    Log.Warning("Тикер {0} не найден, return вернул 0", ticker);
                    return 0;
                }

                var request = new GetLastPricesRequest() { InstrumentId = { instrumentId } };
                var response = await Client.MarketData.GetLastPricesAsync(request);

                Log.Information("Тикер {0} найден, return вернул цену {1}", ticker, response.LastPrices.First().Price);
                return response.LastPrices.First().Price;
            }
            catch (RpcException e)
            {
                Log.Error("Ошибка по тикеру {0} - {1}, return вернул 0", ticker, e.ToString());
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
        public virtual async Task<bool> PlaceOrderAsync(Account account, string ticker, int quantity, OrderDirection direction)
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
                    instrumentId = Futures.First(s => s.Ticker.ToUpper() == ticker.ToUpper()).Uid;
                else
                {
                    Log.Information("Тикер {0} не найден, return вернул: {1}", ticker, false);
                    return false; //Если введенный тикер не соответствует акциям, фьючерсам и фондами в списке, то возвращает false
                }                  

                var request = new PostOrderRequest()
                {
                    AccountId = account.Id,
                    Direction = direction,
                    Quantity = quantity,
                    InstrumentId = instrumentId,
                    OrderType = OrderType.Bestprice
                };
                var response = await Client.Orders.PostOrderAsync(request);

                Log.Information("Заявка на тикер {0} выставлена успешно. Направление: {1}, количество: {2}, тип:{3}", ticker, direction, quantity, OrderType.Bestprice);
                return true;    //Если заявка выставлена успешно, то возвращает true
            }
            catch (RpcException e)
            {
                Log.Error("Ошибка по тикеру {0} - {1}, return вернул 0", ticker, e.ToString());
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
            Log.Information("Вызов GetCandlesListAsync()");
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
            else if (Funds.Any(f => f.Ticker.ToLower() == ticker.ToLower()))
            {
                instrumentId = Funds.First(s => s.Ticker.ToLower() == ticker.ToLower()).Uid;
            }
            else 
            {
                Log.Warning("Тикер {0} не найден return вернул null", ticker);
                return null; 
            }

            var request = new GetCandlesRequest()
            {
                InstrumentId = instrumentId,
                From = from.ToTimestamp(),
                To = to.ToTimestamp(),
                Interval = interval
            };
            var response = await Client.MarketData.GetCandlesAsync(request);

            Log.Information("Тикер {0} найден, получил список свеч с {1} по {2} с интервалов {3}: {4}", ticker, from, to, interval, response.Candles.ToList());
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
            Log.Information("Вызов CalculateEma() - список цен: {0}, длина: {1}", prices, length);
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

            Log.Information("Вернул ema: {0}", ema);
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
            Log.Information("Вызов CalculateHeikinAshi() - список свеч: {0}, длина1: {1}, длина2: {2}", candles, length, length2);
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

            Log.Information("o2 = {0}, c2 = {1}", o2, c2);
            Log.Information("o2 > c2 = {0}", o2 > c2 ? "Правда" : "Ложь");
            return o2 > c2 ? false : true; // false - продать, true - купить
        }

        /// <summary>
        /// Модифицированная версия CalculateHeikinAshi
        /// </summary>
        /// <param name="candles">Список свечей</param>
        /// <param name="length">Начальная длина</param>
        /// <param name="length2">Конечная длина</param>
        /// <returns>true если купить и false если продать</returns>
        public static bool ModifiedCalculateHeikinAshi(List<HistoricCandle> candles, int length, int length2)
        {
            Log.Information("Вызов CalculateHeikinAshi() - список свеч: {0}, длина1: {1}, длина2: {2}", candles, length, length2);
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
                haClose.Add((o + h + l + c + c) / 5);
                if (haOpen.Count < 1) haOpen.Add((o + c + o) / 3);
                else haOpen.Add((haOpen[i - 1] + haClose[i] + o) / 3);

                haHigh.Add(decimal.Max(h, decimal.Max(haOpen[i], haClose[i])));
                haLow.Add(decimal.Min(l, decimal.Min(haOpen[i], haClose[i])));
            }

            var o2 = CalculateEma(haOpen, length2); //рассчитывается ЕМА по списку цен открытия
            var c2 = CalculateEma(haClose, length2);
            var h2 = CalculateEma(haHigh, length2);
            var l2 = CalculateEma(haLow, length2);

            Log.Information("o2 = {0}, c2 = {1}", o2, c2);
            Log.Information("o2 > c2 = {0}", o2 > c2 ? "Правда" : "Ложь");
            return o2 < c2 ? false : true; // false - продать, true - купить
        }


        /// <summary>
        /// Метод определяет по тикеру является ли инструмент фьючерсом
        /// </summary>
        /// <param name="ticker">Тикер инструмента</param>
        /// <returns>true если инструмент является фьючерсом, иначе false</returns>
        public bool? IsItFuture(string ticker)
        {
            Log.Information("Вызов IsItFuture()");
            if (ticker == null)
            {
                Log.Warning("Тикер = null");
                return null;
            }
            if (Futures.Any(f => f.Ticker.ToUpper() == ticker.ToUpper()))
            {
                Log.Information("Тикер {0} является фьючерсом, return вернул true", ticker);
                return true;
            }
            else
            {
                Log.Information("Тикер {0} не является фьючерсом, return вернул false", ticker);
                return false;
            }
        }
    }
}
