using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tinkoff_invest_final
{
    public class HeikinAshiCandles
    {
        public List<CandleData> candleData { get; private set; } = new List<CandleData>();
        public class CandleData
        {
            public double Open { get; set; }
            public double High { get; set; }
            public double Low { get; set; }
            public double Close { get; set; }
        }

        public bool CalculateHeikinAshi(List<CandleData> data, int len, int len2)
        {
            List<decimal> candlesOpenCosts = new List<decimal>();
            List<decimal> candlesCloseCosts = new List<decimal>();
            List<decimal> candlesHighCosts = new List<decimal>();
            List<decimal> candlesLowCosts = new List<decimal>();
            foreach (var item in data)
            {
                candlesOpenCosts.Add(Convert.ToDecimal(item.Open));
                candlesCloseCosts.Add(Convert.ToDecimal(item.Close));
                candlesHighCosts.Add(Convert.ToDecimal(item.High));
                candlesLowCosts.Add(Convert.ToDecimal(item.Low));
            }
            var o = ema(candlesCloseCosts, len); //open - массив цен открытия
            var c = ema(candlesOpenCosts, len); //open - массив цен закрытия
            var h = ema(candlesHighCosts, len);
            var l = ema(candlesLowCosts, len);

            List<decimal> haOpen = new List<decimal>();
            List<decimal> haClose = new List<decimal>();            
            List<decimal> haHigh = new List<decimal>();
            List<decimal> haLow = new List<decimal>();
            for (int i = 0; i < len; i++)
            {
                haClose.Add((o + h + l + c) / 4);
                if (haOpen.Count < 1) haOpen.Add((o + c) / 2);
                else haOpen.Add( (haOpen[i - 1] + haClose[i]) / 2 );

                haHigh.Add(decimal.Max(h, decimal.Max(haOpen[i], haClose[i])));
                haLow.Add(decimal.Min(l, decimal.Min(haOpen[i], haClose[i])));
            }

            var o2 = ema(haOpen, len2); //рассчитывается ЕМА по списку цен открытия
            var c2 = ema(haClose, len2);
            var h2 = ema(haHigh, len2);
            var l2 = ema(haLow, len2);

            return o2 > c2 ? false : true; // false - продать, true - купить

            /*var haData = new List<CandleData>();
            for (int i = 0; i < data.Count; i++)
            {
                
                var o = CalculateEMA(data, len, i, "Open");
                var c = CalculateEMA(data, len, i, "Close");
                var h = CalculateEMA(data, len, i, "High");
                var l = CalculateEMA(data, len, i, "Low");

                var haclose = (o + h + l + c) / 4;
                var haopen = i > 0 ? (haData[i - 1].Open + haData[i - 1].Close) / 2 : (o + c) / 2;
                var hahigh = Math.Max(h, Math.Max(haopen, haclose));
                var halow = Math.Min(l, Math.Min(haopen, haclose));
                haData = new List<CandleData>(data);

                var o2 = CalculateEMA(haData, len2, i, "Open");
                var c2 = CalculateEMA(haData, len2, i, "Close");
                var h2 = CalculateEMA(haData, len2, i, "High");
                var l2 = CalculateEMA(haData, len2, i, "Low");

                haData.Add(new CandleData
                {
                    Open = o2,
                    High = h2,
                    Low = l2,
                    Close = c2,
                });
            }
            candleData = haData;
        }

        private double CalculateEMA(List<CandleData> data, int len, int i, string field)
        {
            double sum = 0;
            for (int j = 0; j < len; j++)
            {
                switch (field)
                {
                    case "Open":
                        sum += data[j].Open;
                        break;
                    case "Close":
                        sum += data[j].Close;
                        break;
                    case "High":
                        sum += data[j].High;
                        break;
                    case "Low":
                        sum += data[j].Low;
                        break;
                }
            }
            return sum / len;
        }*/
        }
        private decimal ema(List<decimal> candlesCosts, int length)
        {
            int n = length; //длина
            decimal k = (decimal)2 / (n + 1); //вес
            decimal ema = 0; //текущее ема
            var currentCandleCost = candlesCosts[candlesCosts.Count - n];

            decimal previousEma = currentCandleCost;
            decimal price = currentCandleCost;

            for (int i = n - 1; i > 0; i--)
            {
                currentCandleCost = candlesCosts[candlesCosts.Count - i];
                price = currentCandleCost;
                ema = (price * k) + (previousEma * (1 - k));
                previousEma = ema;
            }
            return ema;
        }
    }
}

