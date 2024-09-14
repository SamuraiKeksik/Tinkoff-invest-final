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
    public class OrdersHandler
    {
        InvestApiClient client;
        MyBot bot;

        public OrdersHandler(InvestApiClient client, MyBot bot)
        {
            this.client = client;
            this.bot = bot;
        }
        public async Task PostBuyOrder(string accountId, string instrumentId, int quantity) //Метод закупает акцию с instrumentId на счете с accountId, с количеством quantity
        {
            var request = new PostOrderRequest
            {
                AccountId = accountId,
                Direction = OrderDirection.Buy,
                InstrumentId = instrumentId,
                Quantity = quantity,
                OrderType = OrderType.Bestprice
            };
            await client.Orders.PostOrderAsync(request);
        }
        public async Task PostSellOrder(string accountId, string instrumentId, int quantity) //Метод закупает акцию с instrumentId на счете с accountId, с количеством quantity
        {
            var request = new PostOrderRequest
            {
                AccountId = accountId,
                Direction = OrderDirection.Sell,
                InstrumentId = instrumentId,
                Quantity = quantity,
                OrderType = OrderType.Bestprice
            };
            await client.Orders.PostOrderAsync(request);
        }

        public async Task PostOrder(string instrumentId, OrderDirection direction, int quantity) //Метод закупает акцию с instrumentId на счете с accountId, с количеством quantity
        {
            var defaultAccount = await bot.accountsHandler.GetDefaultAccountId();
            var request = new PostOrderRequest
            {
                InstrumentId = instrumentId,
                Direction = direction,
                Quantity = quantity,
                AccountId = defaultAccount,
                OrderType = OrderType.Bestprice
            };
            var response = await client.Sandbox.PostSandboxOrderAsync(request);
        }
    }
}
