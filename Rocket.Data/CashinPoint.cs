using System;

namespace Rocket.Data
{
    public class CashinPoint : IGeoPoint
    {
        public double Lat { get; set; }
        public double Lon { get; set; }
        public long Id { get; set; }
        public string Address { get; set; }
        public bool Rur { get; set; }
        public bool Usd { get; set; }
        public bool Eur { get; set; }
        public bool Hidden { get; set; }
        public string Hourse { get; set; }
        public string Name { get; set; }
        public string Type { get; set; }

        private int? _geoHash;
        public int GeoHash 
        {
            get
            {
                if (!_geoHash.HasValue)
                {
                    _geoHash = (int)(((UInt32)Math.Floor((90 + Lat) * 200)) << 16 | ((UInt32)Math.Floor((180 + Lon) * 100)));
                }

                return _geoHash.Value;
            }
        }
    }
}
