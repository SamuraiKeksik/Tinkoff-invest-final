using Microsoft.Win32;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.NetworkInformation;
using System.Text;
using System.Threading.Tasks;

namespace TinkoffInvestApp
{
    internal static class UsefulFunctions
    {

        private static readonly string registryPath =
            Path.Combine(Registry.CurrentUser.Name, "TinkoffInvestApp", "TokenSaver");

        public static string GetRegistryKey(string key) //Сохранение в реестре Windows
        {
            return (string)Registry.GetValue(registryPath, key, string.Empty);
        }

        public static void SetRegistryKey(string key, string value) //Изъятие из реестра Windows
        {
            Registry.SetValue(registryPath, key, value, RegistryValueKind.String);
        }

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
