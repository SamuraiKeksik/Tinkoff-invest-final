using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using Tinkoff_bot;

namespace Tinkoff_invest_final
{
    internal class SharesHandler
    {
        InvestApiClient client;
        MyBot bot;

        public SharesHandler(InvestApiClient client, MyBot bot)
        {
            this.client = client;
            this.bot = bot;
        }
        public async Task<List<Share>> GetSharesList()   //возвращает список ВСЕХ акций на бирже
        {
            var request = new InstrumentsRequest { InstrumentStatus = InstrumentStatus.Base };
            var response = await client.Instruments.SharesAsync(request);
            return response.Instruments.ToList();
        }
        public async Task<List<HistoricCandle>> GetCandlesList(string figi, DateTime from, DateTime to) //Возвращает список свечей по figi с момента from по to
        {            
            var request = new GetCandlesRequest()
            {
                InstrumentId = figi,
                From = from.ToTimestamp(),
                To = to.ToTimestamp(),
                Interval = CandleInterval._30Min
            };
            var response = await client.MarketData.GetCandlesAsync(request);
            return response.Candles.ToList();
        }

        public async Task GetSharesTxtFile()
        {
            var sharesList = await GetSharesList();
            using (StreamWriter writer = new StreamWriter("shares.txt", false))
            {
                foreach (var share in sharesList)
                {
                    writer.WriteLine(share.Ticker + " - " + share.Figi);
                }
            }
        }
            


        public async Task<double> GetEma(int candlesCount, string figi) //Рассчет EMA
        {
            //Свечи будут браться за промежуток с (сейчас - день) по (сейчас)
            //candlesCount - кол-во используемых свечей для рассчета ЕМА
            var from = DateTime.UtcNow.AddDays(-2);
            var to = DateTime.UtcNow;
            var candlesList = await GetCandlesList(figi, from, to);

            int n = candlesCount; //длина
            double k = (double)2 / (n + 1); //вес
            double ema = 0; //текущее ема

            var currentCandle = candlesList[candlesList.Count - n];
            double previousEma = Convert.ToDouble(currentCandle.Close);
            double closePrice = Convert.ToDouble(currentCandle.Close);
            string closeDateTime = currentCandle.Time.ToString();

            for (int i = n - 1; i > 0; i--)
            {
                currentCandle = candlesList[candlesList.Count - i];
                closeDateTime = currentCandle.Time.ToString();
                closePrice = (Convert.ToDouble(currentCandle.Close));
                ema = (closePrice * k) + (previousEma * (1 - k));
                previousEma = ema;
            }
            return ema;

        }
        /* public async Task<double> GetCurrentPrice(List<string> instrumentIdList)
        {
            RepeatedField<string> repeatedField = new RepeatedField<string>();
            foreach (var instrumentId in instrumentIdList)
            {
                repeatedField.Add(instrumentId);
            }
            var request = new GetLastPricesRequest
            {
                InstrumentId = repeatedField
            };
        }*/
    }
}
