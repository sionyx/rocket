using System;
using Microsoft.VisualStudio.TestTools.UnitTesting;


using System;
using System.Collections;
using System.Collections.Generic;
using Rocket.Data;
using Rocket.Tools.Geo;

namespace Rocket.UnitTest
{
    [TestClass]
    public class ToolsTest
    {
        [TestMethod]
        public void TestClusterization1()
        {
            var geoTools = new GeoTools();

            var points = PreparePoints1();
            var cluster = geoTools.ClusterizePoints(points);
            Assert.IsTrue(cluster.Points.Count == 4);
        }

        [TestMethod]
        public void TestClusterization2()
        {
            var geoTools = new GeoTools();

            var points = PreparePoints2();
            var cluster = geoTools.ClusterizePoints(points);
            Assert.IsTrue(cluster.Points.Count < 50);
        }

        class GeoPointMock : IGeoPoint
        {
            public double Lat { get; set; }
            public double Lon { get; set; }
        }

        private static IGeoPoint[] PreparePoints1()
        {
            var random = new Random();
            return new IGeoPoint[]
            {
                new GeoPointMock{ Lat = 56.0 + 0.05 * random.NextDouble() - 0.025, Lon = 37.8 + 0.05 * random.NextDouble() - 0.025},
                new GeoPointMock{ Lat = 56.0 + 0.05 * random.NextDouble() - 0.025, Lon = 37.8 + 0.05 * random.NextDouble() - 0.025},
                new GeoPointMock{ Lat = 56.0 + 0.05 * random.NextDouble() - 0.025, Lon = 37.8 + 0.05 * random.NextDouble() - 0.025},

                new GeoPointMock{ Lat = 55.5 + 0.05 * random.NextDouble() - 0.025, Lon = 37.8 + 0.05 * random.NextDouble() - 0.025},
                new GeoPointMock{ Lat = 55.5 + 0.05 * random.NextDouble() - 0.025, Lon = 37.8 + 0.05 * random.NextDouble() - 0.025},
                new GeoPointMock{ Lat = 55.5 + 0.05 * random.NextDouble() - 0.025, Lon = 37.8 + 0.05 * random.NextDouble() - 0.025},

                new GeoPointMock{ Lat = 55.5 + 0.05 * random.NextDouble() - 0.025, Lon = 37.3 + 0.05 * random.NextDouble() - 0.025},
                new GeoPointMock{ Lat = 55.5 + 0.05 * random.NextDouble() - 0.025, Lon = 37.3 + 0.05 * random.NextDouble() - 0.025},
                new GeoPointMock{ Lat = 55.5 + 0.05 * random.NextDouble() - 0.025, Lon = 37.3 + 0.05 * random.NextDouble() - 0.025},

                new GeoPointMock{ Lat = 56.0 + 0.05 * random.NextDouble() - 0.025, Lon = 37.3 + 0.05 * random.NextDouble() - 0.025},
                new GeoPointMock{ Lat = 56.0 + 0.05 * random.NextDouble() - 0.025, Lon = 37.3 + 0.05 * random.NextDouble() - 0.025},
                new GeoPointMock{ Lat = 56.0 + 0.05 * random.NextDouble() - 0.025, Lon = 37.3 + 0.05 * random.NextDouble() - 0.025},
            };
        }

        private static IGeoPoint[] PreparePoints2()
        {
            var random = new Random();

            var basePoints = new List<GeoPointMock>(50);

            for (var i = 0; i < 50; i++)
            {
                var point = new GeoPointMock{ Lat = 55.751667 + 5 * random.NextDouble() - 2.5, Lon = 37.617778 + 5 * random.NextDouble() - 2.5};
                basePoints.Add(point);
            }

            var points = new IGeoPoint[5000];

            for (var i = 0; i < basePoints.Count; i++)
            {
                var basePoint = basePoints[i];
                for (var j = 0; j < 100; j++)
                {
                    var point = new GeoPointMock
                    {
                        Lat = basePoint.Lat + 0.05*random.NextDouble() - 0.025,
                        Lon = basePoint.Lon + 0.05*random.NextDouble() - 0.025
                    };

                    points[i * 100 + j] = point;
                }
                
            }

            return points;
        }

    }
}
