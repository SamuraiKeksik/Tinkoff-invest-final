using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TinkoffInvestLibSandbox;
using Tinkoff.InvestApi.V1;
using Tinkoff_telegramm;
using static System.Net.Mime.MediaTypeNames;
using System.IO;

namespace TinkoffInvestSandbox
{
    internal class Traiding : IJob
    {
        int maxLots = 1;
        int minLots = 1;
        List<string> tickers = new List<string>()       //Список тикеров для торговли
            {
                "NGU4",
                "SVU4",
                "MMU4",
            };

        public async Task Execute(IJobExecutionContext context)
        {
            var bot = await TinkoffInvestSandboxBot.CreateTinkoffInvestBotAsync("t.HncJqZM1jb2FFJ0DWzWYrcUoc-6KdqLzyUWRCQfIWx3FAAUtV4ju0qX8X10n6sStLkEfsYU3Vc4fR5OG5fghGw", true);

            if (bot.Accounts.Count == 0) return;    //Если нет счетов то выходит из метода
            if (DateTime.Now > DateTime.Parse("20:05") || DateTime.Now < DateTime.Parse("8:00")) return;   // торговля ведется с 8:00 до 20:00

            var account = bot.Accounts.First();
            var positions = await bot.GetPortfolioInstrumentsAsync(account);
            using (StreamWriter writer = new StreamWriter("BotLogs.txt", true))
            {
                await writer.WriteLineAsync(DateTime.Now.ToString());
                foreach (var ticker in tickers)
                {
                    var lastClosePrice = await bot.GetCurrentPriceOfInstrumentAsync(ticker);
                    var candles = await bot.GetSandboxCandlesListAsync(ticker, CandleInterval.Hour);
                    if (bot.CalculateHeikinAshi(candles, 10, 10))
                    {
                        if (await bot.GetLotsOfInstrumentAsync(account, ticker) == 0)
                        {
                            if (await bot.PlaceOrderAsync(account, ticker, maxLots, OrderDirection.Buy))
                                await writer.WriteLineAsync($"Купил {ticker}, {maxLots} лотов, цена за единицу - {lastClosePrice}, всего - {lastClosePrice * maxLots}");
                            else await writer.WriteLineAsync($"Бот не смог купить {ticker}, {maxLots} лотов, цена за единицу - {lastClosePrice}, всего - {lastClosePrice * maxLots}");
                        }
                        else if (await bot.GetLotsOfInstrumentAsync(account, ticker) < 0)
                        {
                            if (await bot.PlaceOrderAsync(account, ticker, maxLots + minLots, OrderDirection.Buy))
                                await writer.WriteLineAsync($"Купил {ticker}, {maxLots + minLots} лотов, цена за единицу - {lastClosePrice}, всего - {lastClosePrice * (maxLots + minLots)}");
                            else await writer.WriteLineAsync($"Бот не смог купить {ticker}, {maxLots + minLots} лотов, цена за единицу - {lastClosePrice}, всего - {lastClosePrice * (maxLots + minLots)}");
                        }
                        await MyTelegramBot.SendMeMessage($"{ticker} - купить");
                        //купить
                    }
                    else
                    {
                        if (await bot.GetLotsOfInstrumentAsync(account, ticker) == 0)
                        {
                            if (await bot.PlaceOrderAsync(account, ticker, minLots, OrderDirection.Sell))
                                await writer.WriteLineAsync($"Продал {ticker}, {minLots} лотов, цена за единицу - {lastClosePrice}, всего - {lastClosePrice * minLots}");
                            else await writer.WriteLineAsync($"Бот не смог продать {ticker}, {minLots} лотов, цена за единицу - {lastClosePrice}, всего - {lastClosePrice * minLots}");

                        }
                        else if (await bot.GetLotsOfInstrumentAsync(account, ticker) > 0)
                        {
                            if (await bot.PlaceOrderAsync(account, ticker, maxLots + minLots, OrderDirection.Sell))
                                await writer.WriteLineAsync($"Продал {ticker}, {maxLots + minLots} лотов, цена за единицу - {lastClosePrice}, всего - {lastClosePrice * (maxLots + minLots)}");
                            else await writer.WriteLineAsync($"Бот не смог продать {ticker}, {maxLots + minLots} лотов, цена за единицу - {lastClosePrice}, всего - {lastClosePrice * (maxLots + minLots)}");                                                       
                        }
                        await MyTelegramBot.SendMeMessage($"{ticker} - Продать");
                        //продать
                    }

                }
                await writer.WriteLineAsync();
            }
            //await MyTelegramBot.SendMeMessage(await bot.GetSandboxAccountInfoAsync(bot.Accounts.First()));

        }
    }
}
