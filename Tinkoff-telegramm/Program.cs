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
        
        string accessToken = "t.yvyfcnjEEMp_In7Coo3ycworDxnNN5uvjG4pBzP6dOlTVxwYYkHD79HlFP-6pu9CvbGKJoxqIFqdb7Mp34NYYQ";
        var myBot = new MyBot(accessToken);
        var tradingStrategies = new TradingStrategies(accessToken);
        
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

            Console.WriteLine("Бот заснул на 60 минут");
            //await myBot.startTrading(sharesStocksList);
            Thread.Sleep(60 *   // minutes to sleep
             60 *   // seconds to a minute
             1000); // milliseconds to a second
        }        
        Console.ReadLine();
    }
}