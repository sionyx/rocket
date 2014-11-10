using System.Collections.Generic;

namespace Rocket.Data
{
    public class GeoCluster : IGeoPoint
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public List<IGeoPoint> Points { get; set; }
    }
}