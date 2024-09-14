using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using Tinkoff_invest_final;

namespace Tinkoff_bot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Настройте маркер авторизации
            string accessToken = "t.HncJqZM1jb2FFJ0DWzWYrcUoc-6KdqLzyUWRCQfIWx3FAAUtV4ju0qX8X10n6sStLkEfsYU3Vc4fR5OG5fghGw";
            var myBot = new MyBot(accessToken);

            await myBot.accountsHandler.CreateAccount("egor");

            /* /

            /* foreach (var item in await myBot.GetSharesList())
             {
                 await Console.Out.WriteLineAsync($"{item.Figi} - {item.Brand} - {item.Name}");
             }*/

            // await myBot.GetEma(4, "BBG0029SG1C1");
            // await myBot.GetEma(10, "BBG0029SG1C1");
           // var defaultAccount = await myBot.accountsHandler.GetDefaultAccountId();
            // await myBot.PostBuyOrder(defaultAccount, "BBG00475KHX6", 3);

           /*
            List<SharesStock> sharesStocksList = new List<SharesStock>();
            sharesStocksList.Add(new SharesStock("BBG00475KHX6", 3, Tinkoff_invest_final.InstrumentType.Share, ""));
            sharesStocksList.Add(new SharesStock("RU000A106T36", 1, Tinkoff_invest_final.InstrumentType.Share, ""));


            await myBot.sharesHandler.GetSharesTxtFile();
           */

          /*  while (true)
            {
                var accounts = await myBot.accountsHandler.GetAccounts();
                var portfolio = await myBot.portfolioHandler.GetPortfolio(accounts[0].Id);
                Console.WriteLine("Портфолио:");
                foreach (var position in portfolio.Positions)
                {
                    await Console.Out.WriteLineAsync($"{position.Figi} - {position.CurrentPrice} - {position.Quantity}");
                }
                
                await myBot.startTrading(sharesStocksList);
                Thread.Sleep(6000);
            }*/

        }
    }
}