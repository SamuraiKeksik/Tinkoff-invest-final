using Tinkoff.InvestApi;
using TinkoffInvestLibSandbox;

var bot = await TinkoffInvestSandboxBot.CreateTinkoffInvestBotAsync("t.HncJqZM1jb2FFJ0DWzWYrcUoc-6KdqLzyUWRCQfIWx3FAAUtV4ju0qX8X10n6sStLkEfsYU3Vc4fR5OG5fghGw", true);
foreach (var account in bot.Accounts)
{
	if (!await bot.PlaceOrderAsync(account, "ydex", 1, Tinkoff.InvestApi.V1.OrderDirection.Buy))
	{
        Console.WriteLine( "НЕ ПОЛУЧИЛОСЬ продать");
    }
	
    Console.WriteLine(await bot.GetSandboxAccountInfoAsync(account));
}



