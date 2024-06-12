using Grpc.Core;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Threading.Tasks;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;

namespace Tinkoff_bot
{
    class Program
    {
        static async Task Main(string[] args)
        {
            // Настройте маркер авторизации
            string accessToken = "t.NNZmN-4T_vnoFq4RG9rdJKhTqdbHvLr1ZuugZECpeXIhz21t1hdU0IGOYkdEz4HN_zK4EU0VA4xqBlaUyuPikw";
            var myBot = new MyBot(accessToken);

            /* /

            /* foreach (var item in await myBot.GetSharesList())
             {
                 await Console.Out.WriteLineAsync($"{item.Figi} - {item.Brand} - {item.Name}");
             }*/

            // await myBot.GetEma(4, "BBG0029SG1C1");
            // await myBot.GetEma(10, "BBG0029SG1C1");
            var defaultAccount = await myBot.GetDefaultAccountId();
           // await myBot.PostBuyOrder(defaultAccount, "BBG00475KHX6", 3);

            List<string> strings = new List<string>();
            strings.Add("BBG00475KHX6");
            while (true)
            {
                var accounts = await myBot.GetAccounts();
                var portfolio = await myBot.GetPortfolio(accounts[0].Id);
                foreach (var position in portfolio.Positions)
                {
                    await Console.Out.WriteLineAsync($"{position.Figi} - {position.CurrentPrice} - {position.Quantity}");
                }
                
                await myBot.startTrading(strings);
                Thread.Sleep(6000);
            }

        }
    }
}