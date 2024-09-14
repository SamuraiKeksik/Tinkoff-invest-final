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
    public class AccountsHandler
    {
        InvestApiClient client;
        MyBot bot;
        public AccountsHandler(InvestApiClient client, MyBot bot) 
        {
            this.client = client;
            this.bot = bot;
        }
        public async Task CreateAccount(string accountName) //Метод создает аккаунт 
        { 
            var request = new OpenSandboxAccountRequest
            {
                Name = accountName
            };
            var response = await client.Sandbox.OpenSandboxAccountAsync(request);
        }

        public async Task DeleteAccount(int accountNum) //Метод удаляет аккаунт по индексу в List из метода GetAccounts 
        { 
            var accountsList = await GetAccounts();
            var request = new CloseSandboxAccountRequest { AccountId = accountsList[accountNum].Id };
            await client.Sandbox.CloseSandboxAccountAsync(request);
        }

        public async Task<List<Account>> GetAccounts() //Метод возвращает List открытых счетов
        { 
            var request = new GetAccountsRequest();
            var response = await client.Sandbox.GetSandboxAccountsAsync(request);
            List<Account> accounts = response.Accounts.ToList();
            return accounts;
        }
        public async Task<List<Account>> GetRealAccounts() //Метод возвращает List открытых счетов
        { 
            var request = new GetAccountsRequest();
            var response = await client.Users.GetAccountsAsync(request);
            List<Account> accounts = response.Accounts.ToList();
            return accounts;
        }

        public async Task<string> GetDefaultAccountId() //Метод возвращает первый счет из всех
        {
            var accounts = await GetAccounts();
            return accounts.First().Id;
        }
        public async Task<string> GetRealDefaultAccountId() //Метод возвращает первый счет из всех
        {
            var accounts = await GetRealAccounts();
            return accounts.First().Id;
        }
    }
}
