using System.Collections.Generic;

namespace Rocket.Data
{
    public class GeoCluster : IGeoPoint
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public string Type { get; set; }
        public IList<IGeoPoint> Points { get; set; }
    }
}