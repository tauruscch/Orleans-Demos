using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace Silo
{
    public class OrleansOptions
    {
        public string ClusterId { get; set; }
        public string ServiceId { get; set; }
        public int SiloPort { get; set; }
        public int GatewayPort { get; set; }
        public string AdvertisedIPAddress { get; set; }
        public string SiloListeningEndpoint { get; set; }
        public string GatewayListeningEndpoint { get; set; }
    }
}
