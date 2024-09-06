using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tinkoff_invest_final
{
    public class SharesStock
    {
        public SharesStock(string instrumentId, int sharesCount, InstrumentType instrumentType, string ticker) 
        {
            InstrumentId = instrumentId;
            SharesCount = sharesCount;  
            InstrumentType = instrumentType;
            Ticker = ticker;
            
        }
        public string InstrumentId { get; set; }
        public int SharesCount { get; set; }
        public InstrumentType InstrumentType { get; set; }
        public string Ticker { get; set; }
    }

    public enum InstrumentType
    {
        Share,
        Future
    }
}
