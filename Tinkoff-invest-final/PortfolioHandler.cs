using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tinkoff.InvestApi;
using Tinkoff.InvestApi.V1;
using Tinkoff_bot;

namespace Tinkoff_invest_final
{
    public class PortfolioHandler
    {
        InvestApiClient client;
        MyBot bot;

        public PortfolioHandler(InvestApiClient client, MyBot bot)
        {
            this.client = client;
            this.bot = bot;
        }

        public async Task<double> GetWithdrawLimits(string accountId) //Метод возвращает свободные на счету деньги
        { 
            var request = new WithdrawLimitsRequest
            {
                AccountId = accountId
            };
            var response = await client.Sandbox.GetSandboxWithdrawLimitsAsync(request);
            string moneyString = response.Money.First().Units.ToString() + ',' + response.Money.First().Nano.ToString();
            return Convert.ToDouble(moneyString);
        }

        public async Task<PortfolioResponse> GetPortfolio(string accountId) //Метод возвращает портфель счета с id = accountId
        {
            var request = new PortfolioRequest { AccountId = accountId };
            var response = await client.Sandbox.GetSandboxPortfolioAsync(request);
            return response;
        }

        public async Task<PortfolioResponse> GetRealPortfolio(string accountId) //Метод возвращает портфель счета с id = accountId
        {
            var request = new PortfolioRequest { AccountId = accountId };
            var response = await client.Operations.GetPortfolioAsync(request);
            return response;
        }
        public async Task<List<PositionsSecurities>> GetPositions(string accountId) //Метод возвращает портфель счета с id = accountId
        {
            var request = new PositionsRequest { AccountId = accountId };
            var response = await client.Sandbox.GetSandboxPositionsAsync(request);
            return response.Securities.ToList();
        }

        public async Task PayIn(string accountId, string money) //Метод зачисляет деньги насчет
        { 
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
    }
}
