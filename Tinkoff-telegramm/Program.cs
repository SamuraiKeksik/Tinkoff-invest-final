using Telegram.Bot.Polling;
using Telegram.Bot.Types;
using Telegram.Bot;
using Tinkoff_telegramm;
using Tinkoff_invest_final;
using Tinkoff_bot;

class Program
{
    static async Task Main(string[] args)
    {        

        
        //await MyTelegramBot.SendMessage("Бот включен!");
        
        string accessToken = "t.oeyKCsszoPthNJn7OqvpufztgWf3Xyll2MhgAYVM0yptSneENO0U0aTuQaQisrkGwysEa7m_lsLQKCldEaApaw";
        var myBot = new MyBot(accessToken);
        var tradingStrategies = new TradingStrategies(accessToken);
        var accountId = await myBot.accountsHandler.GetRealDefaultAccountId();
        
        List<SharesStock> sharesStocksList = new List<SharesStock>();
        sharesStocksList.Add(new SharesStock("TCS00A106YF0", 1, InstrumentType.Share, "VK"));   //VK
        sharesStocksList.Add(new SharesStock("TCS00A103X66", 1, InstrumentType.Share, "POSI")); //POSI
        //sharesStocksList.Add(new SharesStock("BBG00W7FG4V8", 1, InstrumentType.Share, "ASTR")); //ASTR
        sharesStocksList.Add(new SharesStock("BBG00F9XX7H4", 1, InstrumentType.Share, "RNFT")); //RNFT
        sharesStocksList.Add(new SharesStock("TCS00A0ZZBC2", 1, InstrumentType.Share, "SOFL")); //SOFL
        sharesStocksList.Add(new SharesStock("TCS00A105EX7", 1, InstrumentType.Share, "WUSH")); //WUSH

        sharesStocksList.Add(new SharesStock("FUTNG0924000", 1, InstrumentType.Future, "NGU4")); //NGU4
        sharesStocksList.Add(new SharesStock("FUTSILV09240", 1, InstrumentType.Future, "SVU4")); //SVU4
        sharesStocksList.Add(new SharesStock("FUTMXI092400", 1, InstrumentType.Future, "MMU4")); //MMU4

        List<SharesStock> sharesStocksList2 = new List<SharesStock>();
        sharesStocksList2.Add(new SharesStock("FUTNG0924000", 1, InstrumentType.Future, "NGU4")); //NGU4
        //sharesStocksList2.Add(new SharesStock("FUTSILV09240", 1, InstrumentType.Future, "SVU4")); //SVU4
        sharesStocksList2.Add(new SharesStock("FUTMXI092400", 1, InstrumentType.Future, "MMU4")); //MMU4

        while (true)
        {           
            if (DateTime.Now < DateTime.Today.AddHours(10) || DateTime.Now > DateTime.Today.AddHours(20))
            {
                Console.WriteLine("Бот заснул на 60 минут и пропустил исполнение");
                //await myBot.startTrading(sharesStocksList);
                Thread.Sleep(60 *   // minutes to sleep
                 60 *   // seconds to a minute
                 1000); // milliseconds to a second
                continue;
            }

            foreach (var share in sharesStocksList)
            {
                if (await tradingStrategies.SmoothedHeikenAshiCandles(share, 10,10))
                {
                    await MyTelegramBot.SendMessage($"{share.Ticker} - Нужно купить");
                }
                else
                {
                    await MyTelegramBot.SendMessage($"{share.Ticker} - Нужно Продать");
                }
            }

           /* foreach (var share in sharesStocksList2)
            {
                if (await tradingStrategies.SmoothedHeikenAshiCandles(share, 10,10))
                {
                    var portfolio = await myBot.portfolioHandler.GetRealPortfolio(accountId);
                    if (!portfolio.Positions.Any(p => p.InstrumentUid == share.InstrumentId))
                        await myBot.ordersHandler.PostBuyOrder(accountId, share.InstrumentId, 1);
                    else if (portfolio.Positions.First(p => p.InstrumentUid == share.InstrumentId).Quantity < 0)
                        await myBot.ordersHandler.PostBuyOrder(accountId, share.InstrumentId, 2);
                }
                else
                {
                    var portfolio = await myBot.portfolioHandler.GetRealPortfolio(accountId);
                    if (portfolio.Positions.First(p => p.InstrumentUid == share.InstrumentId).Quantity > 0)
                        await myBot.ordersHandler.PostSellOrder(accountId, share.InstrumentId, 2);
                    else if (!portfolio.Positions.Any(p => p.InstrumentUid == share.InstrumentId))
                        await myBot.ordersHandler.PostSellOrder(accountId, share.InstrumentId, 1);
                }
            }*/

            Console.WriteLine("Бот заснул на 60 минут");
            //await myBot.startTrading(sharesStocksList);
            Thread.Sleep(60 *   // minutes to sleep
             60 *   // seconds to a minute
             1000); // milliseconds to a second
        }        
        Console.ReadLine();
    }
}