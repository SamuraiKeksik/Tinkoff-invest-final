using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http.Headers;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.InvestApi.V1;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace TinkoffInvestLib
{
    public static class ConvertExtension
    {
        public static MoneyValue ToMoneyValue(this decimal money)
        {
            string str = money.ToString();
            string[] parts = str.Split(',');
            if (parts.Length == 1)
            {
                parts = [parts[0], "0"];
            }
            return new MoneyValue
            {
                Currency = "Rub",
                Units = Convert.ToInt64(parts[0]),
                Nano = Convert.ToInt32(parts[1])
            };
        }

        public static decimal ToDecimal(this MoneyValue money)
        {            
            string[] parts = 
            {
                money.Units.ToString(),
                money.Nano.ToString() 
            };
            string result = parts[0] + ',' + parts[1];
            return Convert.ToDecimal(result);
        }
    }
}
