using Rocket.Data;
using System;
using System.Collections.Generic;
using System.Linq;

namespace Rocket.Tools.Geo
{
    public class GeoTools
    {
        private const double EarthRadius = 6372795d;

        static public IList<IGeoPoint> ClusterizePoints(IEnumerable<IGeoPoint> points, double radius)
        {
            var clusters = new List<GeoCluster>();
            var radiusSqr = radius*radius;

            foreach (var point in points)
            {
                var added = false;

                foreach (var cluster in clusters)
                {
                    var dist = MapDistanceSqr(point, cluster);
                    if (dist > radiusSqr) continue;

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
            return GeoDistance(point1.Lat, point1.Lon, point2.Lat, point2.Lon);
        }
        static public double GeoDistance(double la1, double lo1, double la2, double lo2)
        {
            la1 = la1 * Math.PI / 180;
            lo1 = lo1 * Math.PI / 180;
            la2 = la2 * Math.PI / 180;
            lo2 = lo2 * Math.PI / 180; 

            var cl1 = Math.Cos(la1);
            var cl2 = Math.Cos(la2);
            var sl1 = Math.Sin(la1);
            var sl2 = Math.Sin(la2);
            var delta = lo2 - lo1;
            var cdelta = Math.Cos(delta);
            var sdelta = Math.Sin(delta);

            var y = Math.Sqrt(Math.Pow(cl2 * sdelta, 2) + Math.Pow(cl1 * sl2 - sl1 * cl2 * cdelta, 2));
            var x = sl1 * sl2 + cl1 * cl2 * cdelta;

            return Math.Atan2(y, x) * EarthRadius;
        }

        static public double MapDistanceSqr(IGeoPoint point1, IGeoPoint point2)
        {
            return (point2.Lat - point1.Lat)*(point2.Lat - point1.Lat) +
                   (point2.Lon - point1.Lon)*(point2.Lon - point1.Lon);
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
