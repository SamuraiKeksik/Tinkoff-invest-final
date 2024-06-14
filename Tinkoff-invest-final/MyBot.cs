using Google.Protobuf.Collections;
using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using Tinkoff_invest_final;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Tinkoff_bot
{
    internal class MyBot
    {
        //Объявление и создание переменной клиента
        public AccountsHandler accountsHandler;
        public PortfolioHandler portfolioHandler;
        public SharesHandler sharesHandler;
        public OrdersHandler ordersHandler;

        int defaultQuantity = 3;
        public MyBot(string token) 
        { 
            var client = InvestApiClientFactory.Create(token);
            accountsHandler = new AccountsHandler(client, this);
            portfolioHandler = new PortfolioHandler(client, this);
            sharesHandler = new SharesHandler(client, this);
            ordersHandler = new OrdersHandler(client, this);
        }              
        public async Task startTrading(List<string> figis)
        {
            foreach (var figi in figis)
            {
                var currentPositions = await portfolioHandler.GetPositions(await accountsHandler.GetDefaultAccountId());
                var longEma = await sharesHandler.GetEma(34, figi); //длинная ЕМА
                var shortEma = await sharesHandler.GetEma(30, figi); //короткая ЕМА
                Console.WriteLine("LongEma - " + longEma + ", ShortEma - " + shortEma);
                if (longEma > shortEma) //Если цена длинной ЕМА больше короткой - то продаем 
                {
                    if(currentPositions.Any(x => x.Figi == figi))
                    {
                        await ordersHandler.PostOrder(figi, OrderDirection.Sell, defaultQuantity); //Если акции есть на счете то мы продаем
                        Console.WriteLine("ПРОДАНО!");
                    }
                }
                else if(shortEma > longEma) ////Если цена длинной ЕМА меньше короткой - то покупаем
                {
                    if (!currentPositions.Any(x => x.Figi == figi)) //Если акции есть на счете то мы НЕ покупаем
                    {
                        await ordersHandler.PostOrder(figi, OrderDirection.Buy, defaultQuantity);
                        Console.WriteLine( "КУПЛЕНО!");
                    }
                }
            }
           
        }
    }
}
