using Telegram.Bot.Types;
using Tinkoff.InvestApi;
using Tinkoff_telegramm;
using TinkoffInvestLibSandbox;
using TinkoffInvestSandbox;


//HistoryCalculator.FinamCalculator(@"C:\Users\samur\Desktop\SVZ4202312_240801_241001.csv");

var tinkoffBot = await TinkoffInvestSandboxBot.CreateTinkoffInvestBotAsync("t.HncJqZM1jb2FFJ0DWzWYrcUoc-6KdqLzyUWRCQfIWx3FAAUtV4ju0qX8X10n6sStLkEfsYU3Vc4fR5OG5fghGw", true);
await MyTelegramBot.SendMeMessage("Бот включен!");
//await tinkoffBot.ClearSandboxAccountAsync(tinkoffBot.Accounts.First(), 1000000);
await MyTelegramBot.SendMeMessage(await tinkoffBot.GetSandboxAccountInfoAsync(tinkoffBot.Accounts.First()));


TradingScheduler.Start();   //Запуск бота
Console.WriteLine("Бот работает!");
while (true)
{}



