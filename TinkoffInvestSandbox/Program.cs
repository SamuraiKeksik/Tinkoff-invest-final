using Telegram.Bot.Types;
using Tinkoff.InvestApi;
using Tinkoff_telegramm;
using TinkoffInvestLib;
using TinkoffInvestSandbox;


//HistoryCalculator.FinamCalculator(@"C:\Users\samur\Desktop\SVZ4202312_240801_241001.csv");

/*var tinkoffBot = await TinkoffInvestSandboxBot.CreateTinkoffInvestBotAsync("t.HncJqZM1jb2FFJ0DWzWYrcUoc-6KdqLzyUWRCQfIWx3FAAUtV4ju0qX8X10n6sStLkEfsYU3Vc4fR5OG5fghGw");
await MyTelegramBot.SendMeMessage("Бот включен!");
await MyTelegramBot.SendMeMessage(await tinkoffBot.GetSandboxAccountInfoAsync(tinkoffBot.Accounts.First()));*/

var tinkoffBot = await TinkoffInvestBot.CreateTinkoffInvestBotAsync("t.deWIHc75EVsSO_KlIcODqpIeu9r5v1kSdmfPWBYOAWbYrj9nrAZ1tGfuoEzw4oWElOu_xoA7a_vif4OW9imOgw");
foreach (var account in tinkoffBot.Accounts)
{
    Console.WriteLine(await tinkoffBot.GetAccountInfoAsync(account));
}


/*TradingScheduler.Start();   //Запуск бота
Console.WriteLine("Бот работает!");
while (true)
{}*/



