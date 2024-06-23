using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace Tinkoff_invest_final
{
    internal class SharesStock
    {
        public SharesStock(string instrumentId, int sharesCount) 
        {
            this.instrumentId = instrumentId;
            this.sharesCount = sharesCount;            
        }
        public string instrumentId { get; set; }
        public int sharesCount { get; set; }
    }
}
