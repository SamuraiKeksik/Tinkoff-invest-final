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
            var accounts = await myBot.GetAccounts();
            var portfolio = await myBot.GetPortfolio(accounts[0].Id);
            /* foreach (var position in portfolio.Positions)
             {
                 await Console.Out.WriteLineAsync($"{position.Figi} - {position.CurrentPrice} - {position.Quantity}");
             }*/

            /* foreach (var item in await myBot.GetSharesList())
             {
                 await Console.Out.WriteLineAsync($"{item.Figi} - {item.Brand} - {item.Name}");
             }*/

            await myBot.GetEma(10, "BBG0029SG1C1");
            // await myBot.PostBuyOrder(accounts[0].Id, "7132b1c9-ee26-4464-b5b5-1046264b61d9", 10);

        }
    }
}