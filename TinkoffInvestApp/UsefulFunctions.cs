using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace TinkoffInvestApp
{
    internal static class UsefulFunctions
    {
        public static bool CheckConnection(String URL)
        {
            try
            {
                using (Ping pinger = new Ping())
                {
                    PingReply reply = pinger.Send(URL);
                    return reply.Status == IPStatus.Success;
                }
            }
            catch (PingException)
            {
                return false;
            }
        }

        public static bool CheckTinkoffConnection()
        {
            return CheckConnection("invest-public-api.tinkoff.ru"); //Адрес Tinkoff Invest (Не песочница)
        }

        public static bool CheckTinkoffSandboxConnection()
        {
            return CheckConnection("sandbox-invest-public-api.tinkoff.ru"); //Адрес песочницы Tinkoff Invest
        }
    }
}
