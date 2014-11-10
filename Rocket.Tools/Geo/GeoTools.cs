using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rocket.Data;

namespace Rocket.Tools.Geo
{
    public class GeoTools
    {
        private const double EarthRadius = 6372.795d;
        private const double Distance = 1000d;

        public GeoCluster ClusterizePoints(IGeoPoint[] points)
        {
            var clusters = new List<GeoCluster>();

            foreach (var point in points)
            {
                var added = false;

                foreach (var cluster in clusters)
                {
                    var dist = GeoDistance(point, cluster);
                    if (dist > Distance) continue;

                    cluster.Points.Add(point);
                    added = true;
                    break;
                }

                if (added) continue;

                var newcluster = new GeoCluster
                {
                    Lat = point.Lat,
                    Lon = point.Lon,
                    Points = new List<IGeoPoint> {point}
                };

                clusters.Add(newcluster);
            }

            return new GeoCluster {Lat = 0, Lon = 0, Points = clusters.OfType<IGeoPoint>().ToList()};
        }

        public double GeoDistance(IGeoPoint point1, IGeoPoint point2)
        {
            var cl1 = Math.Cos(point1.Lat);
            var cl2 = Math.Cos(point2.Lat);
            var sl1 = Math.Sin(point1.Lat);
            var sl2 = Math.Sin(point2.Lat);
            var delta = point2.Lon - point1.Lon;
            var cdelta = Math.Cos(delta);
            var sdelta = Math.Sin(delta);

            var y = Math.Sqrt(Math.Pow(cl2 * sdelta, 2) + Math.Pow(cl1 * sl2 - sl1 * cl2 * cdelta, 2));
            var x = sl1 * sl2 + cl1 * cl2 * cdelta;

            return Math.Atan2(y, x) * EarthRadius;
        }
    }
}
