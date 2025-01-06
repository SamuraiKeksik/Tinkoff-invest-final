using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace TinkoffInvestApp
{
    public class Timer
    {
        public string Ticker { get; set; }
        public TimeSpan ExecutionTime { get; set; }
        public bool SendToTelegram { get; set; }
        public TinkoffInvestLib.AvailableStrategiesEnum Strategy { get; set; }
    }
}
