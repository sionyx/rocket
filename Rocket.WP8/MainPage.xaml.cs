using System.ComponentModel;
using Microsoft.Phone.Maps.Controls;
using Rocket.Api;
using Rocket.Data;
using Rocket.Tools.Geo;
using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Windows.Devices.Geolocation;

namespace Rocket.WP8
{
    public partial class MainPage
    {
        const int MaxClasterizationLevel = 14;
        const int MinClasterizationLevel = 6;

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

        private int _currentZoomLevel;

        private Task _nextClusterizingTask;
        private Task _prevClusterizingTask;

        private ApiHandlerFabric _apiHandlerFabric;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            Loaded += OnLoaded;
            BackKeyPress += OnBackKeyPress;
        }

        private void OnBackKeyPress(object sender, CancelEventArgs cancelEventArgs)
        {
            if (InfoCardGrid.Visibility == Visibility.Visible)
            {
                SelectedPoint = null;
                VisualStateManager.GoToState(this, "InfoCardsClosed", true);
                cancelEventArgs.Cancel = true;
            }
        }

        #region INITIALIZATION
        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            //VisualStateManager.GoToState(this, "InfoCardsClosed", true);

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

        private async void Map_OnZoomLevelChanged(object sender, MapZoomLevelChangedEventArgs e)
        {
            var zoomLevel = (int) Math.Floor(Map.ZoomLevel);
            if (_currentZoomLevel != zoomLevel)
            {
                if ((_currentZoomLevel > zoomLevel) && (_prevClusterizingTask != null))
                    await _prevClusterizingTask;
                if ((_currentZoomLevel < zoomLevel) && (_nextClusterizingTask != null))
                    await _nextClusterizingTask;

                _currentZoomLevel = zoomLevel;

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

        private async void PreparePoints()
        {
            if (_permanentPoints == null || _clusterizablePoints == null) return;

            var zoomLevel = Math.Max(Math.Min(_currentZoomLevel, MaxClasterizationLevel), MinClasterizationLevel);

            if (!_zoomClusters.ContainsKey(zoomLevel))
            {
                await Task.Run(() => PrepareZoomLevel(zoomLevel));
            }
            _pointsToDisplay = _zoomClusters[zoomLevel];
            RemoveAllPointsFromMap();
            UpdatePointsInView();

            if (!_zoomClusters.ContainsKey(zoomLevel + 1))
                _nextClusterizingTask = Task.Run(() => PrepareZoomLevel(zoomLevel + 1));
            if (!_zoomClusters.ContainsKey(zoomLevel - 1))
                _prevClusterizingTask = Task.Run(() => PrepareZoomLevel(zoomLevel - 1));
        }

        private void PrepareZoomLevel(int zoomLevel)
        {
            var ts = DateTime.Now;

            if (zoomLevel < MaxClasterizationLevel)
            {
                var dist = 0.005 * (1 << (MaxClasterizationLevel - zoomLevel));
                _zoomClusters[zoomLevel] = GeoTools.ClusterizePoints(_clusterizablePoints, dist).Concat(_permanentPoints).ToList();
            }
            else
            {
                _zoomClusters[zoomLevel] = _clusterizablePoints.Concat(_permanentPoints).ToList();
            }

            var dt = DateTime.Now - ts;
            Debug.WriteLine("Clasterization Time: {0} {1}/{2}", dt.TotalSeconds, _zoomClusters[zoomLevel].Count() - _permanentPoints.Count(), _clusterizablePoints.Count());
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
                }
            }

            foreach (var point in _pointsToDisplay)
            {
                if (_pointsOnMap.Contains(point)) continue;

                if (PointInBox(point, topLeft, bottomRight))
                {
                    var pinPosition = new GeoCoordinate(point.Lat, point.Lon);
                    var pinImage = new Image
                    {
                        Source = new BitmapImage(new Uri(_pinImages[point.Type] ?? "Assets/pin_ic.png", UriKind.Relative)), 
                        Tag = point,
                        Opacity = ((_selectedPoint != null) && (_selectedPoint != point)) ? 0.3 : 1.0
                    };
                    pinImage.Tap += PinImageOnTap;

                    var pinOverlay = new MapOverlay
                    {
                        Content = pinImage,
                        PositionOrigin = new Point(0, 1),
                        GeoCoordinate = pinPosition
                    };

                    _pinsLayer.Add(pinOverlay);
                    _pointsOnMap.Add(point);
                    _pointOverlays[point] = pinOverlay;
                }
            }
        }

        private void PinImageOnTap(object sender, GestureEventArgs gestureEventArgs)
        {
            if (!(sender is FrameworkElement)) return;

            ShowPointInfo((sender as FrameworkElement).Tag as IGeoPoint);
        }

        private void ShowPointInfo(IGeoPoint point)
        {
            if (point is CashinPoint)
            {

                var cachinPoint = (point as CashinPoint);
                SelectedPoint = point;
                Map.SetView(new GeoCoordinate(cachinPoint.Lat, cachinPoint.Lon), Math.Max(Map.ZoomLevel, 16));

                VisualStateManager.GoToState(this, "InfoCardOpened", true);
            }
            else if (point is GeoCluster)
            {
                var cluster = (point as GeoCluster);
                Map.SetView(new GeoCoordinate(cluster.Lat, cluster.Lon), Map.ZoomLevel + 1);
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

        #region SELECTED POINT 

        private IGeoPoint _selectedPoint;
        public IGeoPoint SelectedPoint
        {
            get { return _selectedPoint; }
            set
            {
                if (_selectedPoint != null)
                    SetPointOpacity(_selectedPoint, 0.3);

                _selectedPoint = value;

                if (_selectedPoint != null)
                {
                    SetPointOpacity(_selectedPoint, 1.0);
                    TransporizePoints(_selectedPoint);
                }
                else
                {
                    UntransporizePoints();
                }
            }

            
        }

        private void TransporizePoints(IGeoPoint except)
        {
            foreach (var point in _pointsOnMap.ToList())
            {
                if (point == except) continue;

                SetPointOpacity(point, 0.3);
            }
        }

        private void UntransporizePoints()
        {
            foreach (var point in _pointsOnMap.ToList())
            {
                SetPointOpacity(point, 1.0);
            }
        }

        private void SetPointOpacity(IGeoPoint point, double opacity)
        {
            if (!_pointOverlays.ContainsKey(point)) return;

            var overlay = _pointOverlays[point];
            if (overlay == null) return;

            var element = overlay.Content as FrameworkElement;
            if (element == null) return;

            element.Opacity = opacity;
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