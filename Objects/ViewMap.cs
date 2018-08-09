using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace CosmosAppWithBenchmark
{
    public class ViewMap : IInteraction
    {
        public int minutesViewed { get; set; }
        public string type { get; set; }
    }
}
