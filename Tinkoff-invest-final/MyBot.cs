using Google.Protobuf.WellKnownTypes;
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Reflection.Metadata.Ecma335;
using System.Runtime.ExceptionServices;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using static System.Runtime.InteropServices.JavaScript.JSType;

namespace Tinkoff_bot
{
    internal class MyBot
    {
        //Объявление и создание переменной клиента
        InvestApiClient client;
        public MyBot(string token) => client = InvestApiClientFactory.Create(token);

        public async Task CreateAccount(string accountName)
        { //Метод создает аккаунт 
            var request = new OpenSandboxAccountRequest
            {
                Name = accountName                
            };
            var response = await client.Sandbox.OpenSandboxAccountAsync(request);        
       }

        public async Task DeleteAccount(int accountNum)
        { //Метод удаляет аккаунт по индексу в List из метода GetAccounts 
            var accountsList = await GetAccounts();
            var request = new CloseSandboxAccountRequest { AccountId = accountsList[accountNum].Id };
            await client.Sandbox.CloseSandboxAccountAsync(request);
       }

        public async Task<List<Account>> GetAccounts()
        { //Метод возвращает List открытых счетов
            var request = new GetAccountsRequest();               
            var response = await client.Sandbox.GetSandboxAccountsAsync(request);
            List<Account> accounts = response.Accounts.ToList();
            return accounts;
        }

        public async Task PayIn(string accountId, string money)
        { //Метод зачисляет деньги насчет
            MoneyValue moneyAmount = new MoneyValue()
            {
                Currency = "Rub",
                Units = Convert.ToInt64(money.Split(',')[0]),
                Nano = Convert.ToInt32(money.Split(',')[1])
            };
            var request = new SandboxPayInRequest
            {
               AccountId = accountId,
               Amount = moneyAmount
            };
            await client.Sandbox.SandboxPayInAsync(request);
        }   

        public async Task<double> GetWithdrawLimits(string accountId)
        { //Метод возвращает свободные на счету деньги
            var request = new WithdrawLimitsRequest
            {
               AccountId = accountId
            };
            var response = await client.Sandbox.GetSandboxWithdrawLimitsAsync(request);
            string moneyString = response.Money.First().Units.ToString() + ',' + response.Money.First().Nano.ToString();
            return Convert.ToDouble(moneyString);
        }

        public async Task<PortfolioResponse> GetPortfolio(string accountId)
        {//Метод возвращает портфель счета с id = accountId
            var request = new PortfolioRequest { AccountId = accountId };
            var response = await client.Sandbox.GetSandboxPortfolioAsync(request);
            return response;
        }

        public async Task PostBuyOrder(string accountId, string instrumentId, int quantity)
        {//Метод закупает акцию с instrumentId на счете с accountId, с количеством quantity
            var request = new PostOrderRequest
            {
                AccountId = accountId,
                Direction = OrderDirection.Buy,
                InstrumentId = instrumentId,
                Quantity = quantity,
                OrderType = OrderType.Bestprice
            };
            await client.Sandbox.PostSandboxOrderAsync(request);
        }

        public async Task<List<Share>> GetSharesList()
        {
            //возвращает список акций на бирже
            var request = new InstrumentsRequest { InstrumentStatus = InstrumentStatus.Base };
            var response = await client.Instruments.SharesAsync(request);
            return response.Instruments.ToList();
        }

        public async Task<List<HistoricCandle>> GetCandlesList(string figi, DateTime from, DateTime to)
        {
            //Возвращает список свечей по figi с момента from по to
            var request = new GetCandlesRequest()
            {
                InstrumentId = figi,
                From = from.ToTimestamp(),
                To = to.ToTimestamp(),
                Interval = CandleInterval._30Min
            };
            var response = await client.MarketData.GetCandlesAsync(request);
            return response.Candles.ToList();
        }
        public async Task GetEma(int candlesCount, string figi)
        {         
            //Свечи будут браться за промежуток с (сейчас - день) по (сейчас)
            var from = DateTime.UtcNow.AddDays(-1);
            var to = DateTime.UtcNow;
            var candlesList = await GetCandlesList(figi, from, to);

            int n = candlesCount; //длина
            double k = (double) 2 / (n + 1); //вес
            double ema = 0; //текущее ема
            

            double previousEma = Convert.ToDouble(candlesList[candlesList.Count - n].Close);
            double closePrice = Convert.ToDouble(candlesList[candlesList.Count - n].Close); 
            for (int i = n - 1; i > 0; i--)
            {
                await Console.Out.WriteLineAsync("ema - " + previousEma.ToString() + ". Close - " + closePrice);
                closePrice = (Convert.ToDouble(candlesList[candlesList.Count - i].Close));
                ema = (closePrice * k) + (previousEma * (1 - k));
                previousEma = ema;
            }
            await Console.Out.WriteLineAsync("Итоговое ЕМА - " + ema.ToString());
            //цена закрытия свечи  
             //предыдущее ема
            

            
        }
    }
}
