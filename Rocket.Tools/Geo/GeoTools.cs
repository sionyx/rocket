using Rocket.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rocket.Tools.Geo
{
    public class GeoTools
    {
        private const double EarthRadius = 6372.795d;
        //private const double Distance = 1000d;

        static public IList<IGeoPoint> ClusterizePoints(IEnumerable<IGeoPoint> points, double radius)
        {
            var clusters = new List<GeoCluster>();

            foreach (var point in points)
            {
                var added = false;

                foreach (var cluster in clusters)
                {
                    var dist = GeoDistance(point, cluster);
                    if (dist > radius) continue;

                    cluster.Points.Add(point);
                    added = true;

                    cluster.Lat = (cluster.Lat*(cluster.Points.Count - 1) + point.Lat)/cluster.Points.Count;
                    cluster.Lon = (cluster.Lon*(cluster.Points.Count - 1) + point.Lon)/cluster.Points.Count;
                    break;
                }

                if (added) continue;

                var newcluster = new GeoCluster
                {
                    Lat = point.Lat,
                    Lon = point.Lon,
                    Type = point.Type,
                    Points = new List<IGeoPoint> {point}
                };

                clusters.Add(newcluster);
            }

            return clusters.OfType<IGeoPoint>().ToList();
        }

        static public double GeoDistance(IGeoPoint point1, IGeoPoint point2)
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

        public static int GeoHashFloor(double lat, double lon)
        {
            return (int)(((UInt32)Math.Floor((90 + lat) * 200)) << 16 | ((UInt32)Math.Floor((180 + lon) * 100)));
        }
        public static int GeoHashCeil(double lat, double lon)
        {
            return (int)(((UInt32)Math.Ceiling((90 + lat) * 200)) << 16 | ((UInt32)Math.Ceiling((180 + lon) * 100)));
        }

    }
}
