using Tinkoff.InvestApi;
using Tinkoff_telegramm;
using TinkoffInvestLibSandbox;


var tinkoffBot = await TinkoffInvestSandboxBot.CreateTinkoffInvestBotAsync("t.HncJqZM1jb2FFJ0DWzWYrcUoc-6KdqLzyUWRCQfIWx3FAAUtV4ju0qX8X10n6sStLkEfsYU3Vc4fR5OG5fghGw", true);
await MyTelegramBot.SendMeMessage("Бот включен!");
await MyTelegramBot.SendMeMessage(await tinkoffBot.GetSandboxAccountInfoAsync(tinkoffBot.Accounts.First()));

//var a = await tinkoffBot.GetSandboxCandlesListAsync("NGU4", Tinkoff.InvestApi.V1.CandleInterval.Hour);

TradingScheduler.Start();   //Запуск бота
Console.WriteLine("Бот работает!");
while (true)
{

}



