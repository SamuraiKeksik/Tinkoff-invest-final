using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using Tinkoff_bot;
using Tinkoff_invest_final;
using static Tinkoff_invest_final.HeikinAshiCandles;

namespace Tinkoff_invest_final
{
    public class TradingStrategies
    {
        public AccountsHandler accountsHandler;
        public PortfolioHandler portfolioHandler;
        public SharesHandler sharesHandler;
        public OrdersHandler ordersHandler;

        public TradingStrategies(string token)
        {
            var bot = new MyBot(token);
            var client = InvestApiClientFactory.Create(token);
            accountsHandler = new AccountsHandler(client, bot);
            portfolioHandler = new PortfolioHandler(client, bot);
            sharesHandler = new SharesHandler(client, bot);
            ordersHandler = new OrdersHandler(client, bot);
        }
        /// <summary>
        /// Совершает сделки по индикатору Smoothed Heiken Ashi Candles
        /// </summary>
        /// <param name="sharesStocksList"></param>
        /// <returns></returns>
        public async Task<bool> SmoothedHeikenAshiCandles(SharesStock shareStock, int length1, int length2)
        {
            var candlesList = await sharesHandler.GetCandlesList(shareStock.InstrumentId, DateTime.UtcNow.AddDays(-2), DateTime.UtcNow);
            List<CandleData> candleData = new List<CandleData>();
            var HeikinAshiCandles = new HeikinAshiCandles();
            foreach (var candle in candlesList)
            {
                candleData.Add(new CandleData()
                {
                    Close = Convert.ToDouble(candle.Close),
                    Open = Convert.ToDouble(candle.Open),
                    High = Convert.ToDouble(candle.High),
                    Low = Convert.ToDouble(candle.Low),
                });
            }
            bool result = HeikinAshiCandles.CalculateHeikinAshi(candleData, length1, length2);
            if (result)
            {
                return true; // покупка
            }
            else
            {
                return false;   //продать
            }

            /* var currentPositions = await portfolioHandler.GetPositions(await accountsHandler.GetDefaultAccountId());
             var emaClose1 = await sharesHandler.GetEma(length1, shareStock.instrumentId, HistoricCandleEnum.Close);
             var emaOpen1 = await sharesHandler.GetEma(length1, shareStock.instrumentId, HistoricCandleEnum.Open);
             var emaMax1 = await sharesHandler.GetEma(length1, shareStock.instrumentId, HistoricCandleEnum.Max);
             var emaMin1 = await sharesHandler.GetEma(length1, shareStock.instrumentId, HistoricCandleEnum.Min);

             var averageCloseEma = (emaClose1 + emaOpen1 + emaMax1 + emaMin1) / 4;
             var averageCloseOpen = (emaClose1 + emaOpen1) / 2; //na(haopen[1]) ? (o + c) / 2 : (haopen[1] + haclose[1]) / 2
             var emaMax = double.MaxNumber(emaMax1, double.MaxNumber(averageCloseEma, averageCloseOpen));
             var emaMin = double.MinNumber(emaMin1, double.MinNumber(averageCloseEma, averageCloseOpen));

             var emaClose2 = await sharesHandler.GetEma(length2, shareStock.instrumentId, HistoricCandleEnum.Close);
             var emaOpen2 = await sharesHandler.GetEma(length2, shareStock.instrumentId, HistoricCandleEnum.Open);
             var emaMax2 = await sharesHandler.GetEma(length2, shareStock.instrumentId, HistoricCandleEnum.Max);
             var emaMin2 = await sharesHandler.GetEma(length2, shareStock.instrumentId, HistoricCandleEnum.Min);*/
            /*  var calculationResult = 

col = o2 > c2 ? red : lime
plotcandle(o2, h2, l2, c2, title = "heikin smoothed", color = col)



              var longEma = await sharesHandler.GetEma(34, shareStock.instrumentId); //длинная ЕМА
              var shortEma = await sharesHandler.GetEma(30, shareStock.instrumentId); //короткая ЕМА
              Console.WriteLine("LongEma - " + longEma + ", ShortEma - " + shortEma);
              if (longEma > shortEma) //Если цена длинной ЕМА больше короткой - то продаем 
              {
                  if (currentPositions.Any(x => x.Figi == shareStock.instrumentId))
                  {
                      await ordersHandler.PostOrder(shareStock.instrumentId, OrderDirection.Sell, shareStock.sharesCount); //Если акции есть на счете то мы продаем
                      Console.WriteLine("ПРОДАНО!");
                  }
              }
              else if (shortEma > longEma) ////Если цена длинной ЕМА меньше короткой - то покупаем
              {
                  if (!currentPositions.Any(x => x.Figi == shareStock.instrumentId)) //Если акции есть на счете то мы НЕ покупаем
                  {
                      await ordersHandler.PostOrder(shareStock.instrumentId, OrderDirection.Buy, shareStock.sharesCount);
                      Console.WriteLine("КУПЛЕНО!");
                  }
              }*/
        }
    }
}
