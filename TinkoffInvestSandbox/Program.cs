using Tinkoff.InvestApi;
using Tinkoff_telegramm;
using TinkoffInvestLibSandbox;


var tinkoffBot = await TinkoffInvestSandboxBot.CreateTinkoffInvestBotAsync("t.HncJqZM1jb2FFJ0DWzWYrcUoc-6KdqLzyUWRCQfIWx3FAAUtV4ju0qX8X10n6sStLkEfsYU3Vc4fR5OG5fghGw", true);
await MyTelegramBot.SendMeMessage("Бот включен!");
//await tinkoffBot.ClearSandboxAccountAsync(tinkoffBot.Accounts.First(), 1000000);
await MyTelegramBot.SendMeMessage(await tinkoffBot.GetSandboxAccountInfoAsync(tinkoffBot.Accounts.First()));


TradingScheduler.Start();   //Запуск бота
Console.WriteLine("Бот работает!");
while (true)
{}



