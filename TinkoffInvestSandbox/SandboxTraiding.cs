using Quartz;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Mail;
using System.Net;
using System.Text;
using System.Threading.Tasks;
using TinkoffInvestLib;
using Tinkoff.InvestApi.V1;
using Tinkoff_telegramm;
using static System.Net.Mime.MediaTypeNames;
using System.IO;

namespace TinkoffInvestSandbox
{
    internal class SandboxTraiding : IJob
    {
        int maxLots = 1;    //Сколько лотов в шорт
        int minLots = 1;    //Сколько лотов в лонг
        List<string> tickers = new List<string>()       //Список тикеров для торговли
            {
                "NGV4",
                "MMZ4",
                "SVZ4",
                "CRZ4",
                "CCZ4",
                "BRX4"
        };
        public async Task Execute(IJobExecutionContext context)
        {
            var bot = await TinkoffInvestSandboxBot.CreateTinkoffInvestBotAsync("t.HncJqZM1jb2FFJ0DWzWYrcUoc-6KdqLzyUWRCQfIWx3FAAUtV4ju0qX8X10n6sStLkEfsYU3Vc4fR5OG5fghGw");

            if (bot.Accounts.Count == 0) return;    //Если нет счетов то выходит из метода
            if (DateTime.Now > DateTime.Parse("23:00") || DateTime.Now < DateTime.Parse("8:00")) return;   // торговля ведется с 8:00 до 23:00
            //if (DateTime.Now.DayOfWeek == DayOfWeek.Saturday || DateTime.Now.DayOfWeek == DayOfWeek.Sunday) return;   // торговля ведется с понедельника по пятницу

            var account = bot.Accounts.First();
            var positions = await bot.GetPortfolioInstrumentsAsync(account);
            using (StreamWriter writer = new StreamWriter("BotLogs.txt", true))
            {
                using (StreamWriter errorsWriter = new StreamWriter("BotErrors.txt", true))
                {
                    await writer.WriteLineAsync(DateTime.Now.ToString());
                    await errorsWriter.WriteLineAsync(DateTime.Now.ToString());
                    foreach (var ticker in tickers)
                    {
                        try
                        {
                            var lastClosePrice = await bot.GetCurrentPriceOfInstrumentAsync(ticker);
                            if (lastClosePrice == 0) return;
                            var candles = await bot.GetSandboxCandlesListAsync(ticker, CandleInterval.Hour);
                            if (TinkoffInvestSandboxBot.ModifiedCalculateHeikinAshi(candles, 2, 2))
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
                                await MyTelegramBot.SendJaroslavMessage($"{ticker} - купить");
                                //await MyTelegramBot.SendMeMessage($"{ticker} - купить");
                                //купить
                            }
                            else
                            {
                                if (await bot.GetLotsOfInstrumentAsync(account, ticker) == 0)
                                {
                                    if (bot.IsItFuture(ticker) == null || bot.IsItFuture(ticker) == true)
                                        await writer.WriteLineAsync($"Бот не смог продать фьючерс {ticker}, {minLots} лотов, цена за единицу - {lastClosePrice}, всего - {lastClosePrice * minLots}");
                                    else if (await bot.PlaceOrderAsync(account, ticker, minLots, OrderDirection.Sell))
                                        await writer.WriteLineAsync($"Продал {ticker}, {minLots} лотов, цена за единицу - {lastClosePrice}, всего - {lastClosePrice * minLots}");
                                    else await writer.WriteLineAsync($"Бот не смог продать {ticker}, {minLots} лотов, цена за единицу - {lastClosePrice}, всего - {lastClosePrice * minLots}");

                                }
                                else if (await bot.GetLotsOfInstrumentAsync(account, ticker) > 0)
                                {
                                    if (bot.IsItFuture(ticker) == null || bot.IsItFuture(ticker) == true)
                                    {
                                        if (await bot.PlaceOrderAsync(account, ticker, maxLots, OrderDirection.Sell))
                                            await writer.WriteLineAsync($"Продал {ticker}, {maxLots} лотов, цена за единицу - {lastClosePrice}, всего - {lastClosePrice * (maxLots + minLots)}");
                                        else await writer.WriteLineAsync($"Бот не смог продать {ticker}, {maxLots + minLots} лотов, цена за единицу - {lastClosePrice}, всего - {lastClosePrice * (maxLots + minLots)}");
                                    }
                                    else if (await bot.PlaceOrderAsync(account, ticker, maxLots + minLots, OrderDirection.Sell))
                                        await writer.WriteLineAsync($"Продал {ticker}, {maxLots + minLots} лотов, цена за единицу - {lastClosePrice}, всего - {lastClosePrice * (maxLots + minLots)}");
                                    else await writer.WriteLineAsync($"Бот не смог продать {ticker}, {maxLots + minLots} лотов, цена за единицу - {lastClosePrice}, всего - {lastClosePrice * (maxLots + minLots)}");
                                }
                                await MyTelegramBot.SendJaroslavMessage($"{ticker} - Продать");
                                //await MyTelegramBot.SendMeMessage($"{ticker} - Продать");
                                //продать
                            }
                        }
                        catch (Exception ex)
                        {
                            await errorsWriter.WriteLineAsync($"\tНеизвестная ошибка по тикеру {ticker} - " + ex.ToString());
                        }
                    }
                }
                var accountInfo = await bot.GetSandboxAccountInfoAsync(account);
                await writer.WriteLineAsync($"{accountInfo}");
                await writer.WriteLineAsync();
            }
            //await MyTelegramBot.SendMeMessage(await bot.GetSandboxAccountInfoAsync(bot.Accounts.First()));

        }
    }
}
