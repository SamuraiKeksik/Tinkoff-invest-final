using Telegram.Bot.Types;
using Tinkoff.InvestApi;
using Tinkoff_telegramm;
using TinkoffInvestLib;
using TinkoffInvestSandbox;


//HistoryCalculator.FinamCalculator(@"C:\Users\samur\Desktop\SVZ4_макс_1час.csv");

var tinkoffBot = await TinkoffInvestSandboxBot.CreateTinkoffInvestBotAsync("t.HncJqZM1jb2FFJ0DWzWYrcUoc-6KdqLzyUWRCQfIWx3FAAUtV4ju0qX8X10n6sStLkEfsYU3Vc4fR5OG5fghGw");
await MyTelegramBot.SendMeMessage("Бот включен!");
await MyTelegramBot.SendMeMessage(await tinkoffBot.GetAccountInfoAsync(tinkoffBot.Accounts.First()));


TradingScheduler.Start();   //Запуск бота
Console.WriteLine("Бот работает!");
while (true)
{}



