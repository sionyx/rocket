using System.Diagnostics;
using System.Globalization;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Rocket.Api;
using Rocket.Data;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Windows.Devices.Geolocation;
using Rocket.Tools.Geo;

namespace Rocket.WP8
{
    public partial class MainPage
    {
        private GeoCoordinate _position;
        private MapLayer _locationLayer;
        private MapLayer _pinsLayer;

        private IEnumerable<IGeoPoint> _permanentPoints;
        private IEnumerable<IGeoPoint> _clusterizablePoints; 

        private readonly Dictionary<int, IEnumerable<IGeoPoint>> _zoomClusters = new Dictionary<int, IEnumerable<IGeoPoint>>();
        private readonly List<IGeoPoint> _pointsOnMap = new List<IGeoPoint>();
        private IEnumerable<IGeoPoint> _pointsToDisplay;
        private readonly Dictionary<IGeoPoint, MapOverlay> _pointOverlays = new Dictionary<IGeoPoint, MapOverlay>();

        private readonly Dictionary<string, string> _pinImages = new Dictionary<string, string>
            {
                {"mkb", "Assets/pin_mkb.png"},
                {"ors", "Assets/pin_opc.png"},
                {"intercommerz_office", "Assets/pin_icb.png"},
                {"intercommerz_atm", "Assets/pin_ic.png"}
            };

        private int _currentZoomLevel = 0;

        private ApiHandlerFabric _apiHandlerFabric;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            Loaded += OnLoaded;
        }

        #region INITIALIZATION
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Map.Center = new GeoCoordinate(55.751667, 37.617778);
            Map.ZoomLevel = 13;

            ShowMyLocation();
            LoadPins();
        }

        private async void ShowMyLocation()
        {
            ShowMeButton.Visibility = Visibility.Collapsed;
            var locator = new Geolocator();
            var coordinate = await locator.GetGeopositionAsync();

            ShowMeButton.Visibility = Visibility.Visible;
            _position = CoordinateConverter.ConvertGeocoordinate(coordinate.Coordinate);
            Map.SetView(_position, Map.ZoomLevel);

            var myPos = new Image { Source = new BitmapImage(new Uri("Assets/pin_me.png", UriKind.Relative)) };

            var myLocationOverlay = new MapOverlay
            {
                Content = myPos,
                PositionOrigin = new Point(0.5, 0.5),
                GeoCoordinate = _position
            };

            _locationLayer = new MapLayer { myLocationOverlay };
            Map.Layers.Add(_locationLayer);
        }

        private async void LoadPins()
        {
            _pinsLayer = new MapLayer();
            Map.Layers.Add(_pinsLayer);

            _apiHandlerFabric = new ApiHandlerFabric();
            var getter = _apiHandlerFabric.CashinPointsGetter();
            var points = await getter.GetPointsAsync();

            _clusterizablePoints = points.Where(i => i.Type.Equals("mkb")).ToList();
            _permanentPoints = points.Where(i => !i.Type.Equals("mkb")).ToList();

            PreparePoints();
        }

        #endregion

        #region MAP CONTROLS

        private void ZoomIn(object sender, GestureEventArgs e)
        {
            Map.SetView(Map.Center, Map.ZoomLevel + 1);
        }
        private void ZoomOut(object sender, GestureEventArgs e)
        {
            Map.SetView(Map.Center, Map.ZoomLevel - 1);
        }
        private void ShowMe(object sender, GestureEventArgs e)
        {
            Map.SetView(_position, Map.ZoomLevel);
        }

        #endregion

        #region MAP EVENTS

        private void Map_OnZoomLevelChanged(object sender, MapZoomLevelChangedEventArgs e)
        {
            var zoomLevel = (int) Math.Floor(Map.ZoomLevel);
            if (_currentZoomLevel != zoomLevel)
            {
                _currentZoomLevel = zoomLevel;

                RemoveAllPointsFromMap();
                PreparePoints();

                Map.LandmarksEnabled = (zoomLevel >= 17);
            }

            Debug.WriteLine("ZoomLevel: {0}", Map.ZoomLevel);
        }

        private void Map_OnCenterChanged(object sender, MapCenterChangedEventArgs e)
        {
            UpdatePointsInView();
        }

        #endregion

        #region POINTS ROUTINES

        private void PreparePoints()
        {
            if (_permanentPoints == null || _clusterizablePoints == null) return;

            const int maxLevel = 14;
            const int minLevel = 6;

            var zoomLevel = Math.Max(Math.Min(_currentZoomLevel, maxLevel), minLevel);

            if (!_zoomClusters.ContainsKey(zoomLevel))
            {
                var ts = DateTime.Now;

                if (zoomLevel < maxLevel)
                {
                    var dist = 20*(1 << (14 - zoomLevel));
                    _zoomClusters[zoomLevel] = GeoTools.ClusterizePoints(_clusterizablePoints, dist).Concat(_permanentPoints);
                }
                else
                {
                    _zoomClusters[zoomLevel] = _clusterizablePoints.Concat(_permanentPoints);
                }



                //var dist = 20;
                //_zoomClusters[14] = mkb;
                //_zoomClusters[13] = GeoTools.ClusterizePoints(mkb, dist).Points;
                //for (var i = 12; i >= 6; i--)
                //{
                //    dist *= 2;
                //    _zoomClusters[i] = GeoTools.ClusterizePoints(_zoomClusters[i + 2], dist).Points;
                //}

                var dt = DateTime.Now - ts;
                Debug.WriteLine("Clasterization Time: {0}", dt.TotalSeconds);
            }

            _pointsToDisplay = _zoomClusters[zoomLevel];
            UpdatePointsInView();
        }

        private void RemoveAllPointsFromMap()
        {
            foreach (var point in _pointsOnMap.ToList())
            {
                _pinsLayer.Remove(_pointOverlays[point]);
                _pointOverlays.Remove(point);
                _pointsOnMap.Remove(point);
            }
        }


        public void UpdatePointsInView()
        {
            if (_pointsToDisplay == null) return;


            var topLeft = Map.ConvertViewportPointToGeoCoordinate(new Point(-48, 0));
            var bottomRight = Map.ConvertViewportPointToGeoCoordinate(new Point(Map.ActualWidth, Map.ActualHeight + 64));

            foreach (var point in _pointsOnMap.ToList())
            {
                if (!PointInBox(point, topLeft, bottomRight))
                {
                    _pinsLayer.Remove(_pointOverlays[point]);
                    _pointOverlays.Remove(point);
                    _pointsOnMap.Remove(point);
                    //Debug.WriteLine("pinOverlay Removed {0}", _pointsOnMap.Count);
                }
            }

            foreach (var point in _pointsToDisplay)
            {
                if (_pointsOnMap.Contains(point)) continue;

                if (PointInBox(point, topLeft, bottomRight))
                {
                    var pinPosition = new GeoCoordinate(point.Lat, point.Lon);
                    var pinImage = new Image { Source = new BitmapImage(new Uri(_pinImages[point.Type] ?? "Assets/pin_ic.png", UriKind.Relative)) };

                    var pinOverlay = new MapOverlay
                    {
                        Content = pinImage,
                        PositionOrigin = new Point(0, 1),
                        GeoCoordinate = pinPosition
                    };

                    _pinsLayer.Add(pinOverlay);
                    _pointsOnMap.Add(point);
                    _pointOverlays[point] = pinOverlay;
                    //Debug.WriteLine("pinOverlay Added {0}", _pointsOnMap.Count);
                }
            }
        }

        private static bool PointInBox(IGeoPoint point, GeoCoordinate leftTopCorner, GeoCoordinate rightBottomCorner)
        {
            return (point != null) && (leftTopCorner != null) && (rightBottomCorner != null) &&
                   (point.Lat <= leftTopCorner.Latitude) &&
                   (point.Lat >= rightBottomCorner.Latitude) &&
                   (point.Lon >= leftTopCorner.Longitude) &&
                   (point.Lon <= rightBottomCorner.Longitude);
        }

        #endregion

    }

    public static class CoordinateConverter
    {
        public static GeoCoordinate ConvertGeocoordinate(Geocoordinate geocoordinate)
        {
            return new GeoCoordinate
                (
                geocoordinate.Latitude,
                geocoordinate.Longitude,
                geocoordinate.Altitude ?? Double.NaN,
                geocoordinate.Accuracy,
                geocoordinate.AltitudeAccuracy ?? Double.NaN,
                geocoordinate.Speed ?? Double.NaN,
                geocoordinate.Heading ?? Double.NaN
                );
        }
    }   
}