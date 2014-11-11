using System.Collections.Generic;
using System.Runtime.Serialization;

namespace Rocket.Data
{
    [DataContract]
    public class CashinPointsResponse
    {
        [DataMember(Name = "last_update")]
        public long LastUpdate { get; set; }

        [DataMember(Name = "atms")]
        public List<CashinPoint> Points { get; set; } 
    }
}
