﻿using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Linq;
using System.Threading.Tasks;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media.Imaging;
using Microsoft.Expression.Interactivity.Core;
using Microsoft.Phone.Maps.Controls;
using Rocket.Data;
using Rocket.Tools.Geo;

namespace Rocket.Controls
{
    public partial class CashinMap
    {
        private const string ApplicationID = "c4ea29c3-5bf4-418e-b96d-b53842eefbf6";
        private const string AuthenticationToken = "6SFrifSbB7rxCmZQu_EaBw";

        const int MaxClasterizationLevel = 14;
        const int MinClasterizationLevel = 6;

        private const double PointTransparensy = 0.3;

        private MapLayer _locationLayer;
        private MapLayer _pinsLayer;
        private MapOverlay _locationOverlay;

        private int _currentZoomLevel;

        private Task _nextClusterizingTask;
        private Task _prevClusterizingTask;

        private IEnumerable<IGeoPoint> _permanentPoints;
        private IEnumerable<IGeoPoint> _clusterizablePoints;

        private Func<CashinPoint, bool> _clasterizationCondition;

        private readonly Dictionary<int, IEnumerable<IGeoPoint>> _zoomClusters = new Dictionary<int, IEnumerable<IGeoPoint>>();
        private readonly List<IGeoPoint> _pointsOnMap = new List<IGeoPoint>();
        private IEnumerable<IGeoPoint> _pointsToDisplay;
        private readonly Dictionary<IGeoPoint, MapOverlay> _pointOverlays = new Dictionary<IGeoPoint, MapOverlay>();

        private readonly Dictionary<string, string> _pinImages = new Dictionary<string, string>
            {
                {"mkb", "/Assets/pin_mkb.png"},
                {"ors", "/Assets/pin_opc.png"},
                {"intercommerz_office", "/Assets/pin_ic.png"},
                {"intercommerz_atm", "/Assets/pin_icb.png"}
            };



        #region PUBLIC PROPERTIES

        #region DEPENDENCY PROPERTY Points

        public static DependencyProperty PointsProperty =
            DependencyProperty.Register("Points", typeof(List<CashinPoint>), typeof(CashinMap),
                    new PropertyMetadata(null, PointsChanged));

        private List<CashinPoint> _points;
        public List<CashinPoint> Points
        {
            get { return (List<CashinPoint>)GetValue(PointsProperty); }
            set
            {
                SetValue(PointsProperty, value);
                SetPoints(value);
            }
        }

        private static void PointsChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var map = (CashinMap)d;
            map.SetPoints((List<CashinPoint>)e.NewValue);
        }

        private void SetPoints(List<CashinPoint> points)
        {
            _points = points;

            if (_points == null)
            {
                _clusterizablePoints = null;
                _permanentPoints = null;
                return;
            }

            _zoomClusters.Clear();

            if (_clasterizationCondition != null)
            {
                _clusterizablePoints = points.Where(p => _clasterizationCondition(p)).ToList();
                _permanentPoints = points.Where(p => !_clasterizationCondition(p)).ToList();
            }
            else
            {
                _permanentPoints = points;
            }

            PreparePoints();
        }
        #endregion

        #region DEPENDENCY PROPERTY SelectedPoint

        public static DependencyProperty SelectedPointProperty =
            DependencyProperty.Register("SelectedPoint", typeof(IGeoPoint), typeof(CashinMap),
                    new PropertyMetadata(null, SelectedPointChanged));

        private IGeoPoint _selectedPoint;
        public IGeoPoint SelectedPoint
        {
            get { return (IGeoPoint)GetValue(SelectedPointProperty); }
            set
            {
                _selectedPoint = value;
                SetValue(SelectedPointProperty, value);
                SetSelectedPoint(value);
            }
        }

        private static void SelectedPointChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var map = (CashinMap)d;
            map.SetSelectedPoint((IGeoPoint)e.NewValue);
        }

        private void SetSelectedPoint(IGeoPoint point)
        {
            if (_selectedPoint != null)
                SetPointOpacity(_selectedPoint, 0.3);

            _selectedPoint = point;

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
        #endregion

        #region DEPENDENCY PROPERTY ClasterizationCondition

        public static DependencyProperty ClasterizationConditionProperty =
            DependencyProperty.Register("ClasterizationCondition", typeof(Func<CashinPoint, bool>), typeof(CashinMap),
                    new PropertyMetadata(null, ClasterizationConditionChanged));

        public Func<CashinPoint, bool> ClasterizationCondition
        {
            get { return (Func<CashinPoint, bool>)GetValue(ClasterizationConditionProperty); }
            set
            {
                SetValue(ClasterizationConditionProperty, value);
                SetClasterizationCondition(value);
            }
        }

        private static void ClasterizationConditionChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var map = (CashinMap)d;
            map.SetClasterizationCondition((Func<CashinPoint, bool>)e.NewValue);
        }

        private void SetClasterizationCondition(Func<CashinPoint, bool> condition)
        {
            _clasterizationCondition = condition;
            SetPoints(_points);
        }
        #endregion

        #region DEPENDENCY PROPERTY CurrentLocation

        public static DependencyProperty CurrentLocationProperty =
            DependencyProperty.Register("CurrentLocation", typeof(GeoCoordinate), typeof(CashinMap),
                    new PropertyMetadata(null, CurrentLocationChanged));

        private GeoCoordinate _currentLocation;
        public GeoCoordinate CurrentLocation
        {
            get { return (GeoCoordinate)GetValue(CurrentLocationProperty); }
            set
            {
                _currentLocation = value;
                SetValue(CurrentLocationProperty, value);
                SetCurrentLocation(value);
            }
        }

        private static void CurrentLocationChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var map = (CashinMap)d;
            map.SetCurrentLocation((GeoCoordinate)e.NewValue);
        }

        private void SetCurrentLocation(GeoCoordinate location)
        {
            _currentLocation = location;
            Map.SetView(_currentLocation, Map.ZoomLevel);

            if (_locationOverlay == null)
            {
                var myPos = new Image { Source = new BitmapImage(new Uri("/Assets/pin_me.png", UriKind.Relative)) };

                _locationOverlay = new MapOverlay
                {
                    Content = myPos,
                    PositionOrigin = new Point(0.5, 0.5),
                    GeoCoordinate = _currentLocation
                };

                _locationLayer.Add(_locationOverlay);
            }
            else
            {
                _locationOverlay.GeoCoordinate = _currentLocation;
            }
        }
        #endregion

        #region DEPENDENCY PROPERTY LocationResolved

        public static DependencyProperty LocationResolvedProperty =
            DependencyProperty.Register("LocationResolved", typeof (bool), typeof (CashinMap), 
                    new PropertyMetadata(false, LocationResolvedChanged));

        public bool LocationResolved
        {
            get { return (bool)GetValue(LocationResolvedProperty); }
            set
            {
                SetValue(LocationResolvedProperty, value);
                SetLocationResolved(value);
            }
        }

        private static void LocationResolvedChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
        {
            var map = (CashinMap)d;
            map.SetLocationResolved((bool)e.NewValue);
        }

        private void SetLocationResolved(bool isLocationResolved)
        {
            if (_locationOverlay == null) return;

            if (isLocationResolved && !_locationLayer.Contains(_locationOverlay))
                _locationLayer.Add(_locationOverlay);
            else if (!isLocationResolved && _locationLayer.Contains(_locationOverlay))
                _locationLayer.Remove(_locationOverlay);
        }
        #endregion

        #endregion

        public ICommand MoveToCurrentLocationCommand { get; set; }
        public ICommand ZoomInCommand { get; set; }
        public ICommand ZoomOutCommand { get; set; }

        public CashinMap()
        {
            InitializeComponent();
            PrepareLayers();
            Loaded += OnLoaded;

            MoveToCurrentLocationCommand = new ActionCommand(() => Map.SetView(_currentLocation, Map.ZoomLevel));
            ZoomInCommand = new ActionCommand(() => Map.SetView(Map.Center, Map.ZoomLevel + 1));
            ZoomOutCommand = new ActionCommand(() => Map.SetView(Map.Center, Map.ZoomLevel - 1));

            Map.Loaded += MapOnLoaded;
        }

        private void MapOnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.ApplicationId = ApplicationID;
            Microsoft.Phone.Maps.MapsSettings.ApplicationContext.AuthenticationToken = AuthenticationToken;
        }

        #region INITIALIZATION

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            _currentLocation = new GeoCoordinate(55.751667, 37.617778);
            Map.Center = _currentLocation;
            Map.ZoomLevel = 13;
        }

        private void PrepareLayers()
        {
            _pinsLayer = new MapLayer();
            Map.Layers.Add(_pinsLayer);

            _locationLayer = new MapLayer();
            Map.Layers.Add(_locationLayer);
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

        private void PinImageOnTap(object sender, GestureEventArgs gestureEventArgs)
        {
            if (!(sender is FrameworkElement)) return;

            var point = (sender as FrameworkElement).Tag as IGeoPoint;
            if (point == null) return;

            if (point is CashinPoint)
            {
                var cachinPoint = (point as CashinPoint);
                SelectedPoint = point;
                Map.SetView(new GeoCoordinate(cachinPoint.Lat, cachinPoint.Lon), Math.Max(Map.ZoomLevel, 16));

                //VisualStateManager.GoToState(this, "InfoCardOpened", true);
            }
            else if (point is GeoCluster)
            {
                var cluster = (point as GeoCluster);
                Map.SetView(new GeoCoordinate(cluster.Lat, cluster.Lon), Map.ZoomLevel + 1);
            }
        }

        #endregion

        #region POINTS PRESENTING

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
            //var ts = DateTime.Now;

            if (zoomLevel < MaxClasterizationLevel)
            {
                var dist = 0.005 * (1 << (MaxClasterizationLevel - zoomLevel));
                _zoomClusters[zoomLevel] = GeoTools.ClusterizePoints(_clusterizablePoints, dist).Concat(_permanentPoints).ToList();
            }
            else
            {
                _zoomClusters[zoomLevel] = _clusterizablePoints.Concat(_permanentPoints).ToList();
            }

            //var dt = DateTime.Now - ts;
            //Debug.WriteLine("Clasterization Time: {0} {1}/{2}", dt.TotalSeconds, _zoomClusters[zoomLevel].Count() - _permanentPoints.Count(), _clusterizablePoints.Count());
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
                        Opacity = ((_selectedPoint != null) && (_selectedPoint != point)) ? PointTransparensy : 1.0
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

        private static bool PointInBox(IGeoPoint point, GeoCoordinate leftTopCorner, GeoCoordinate rightBottomCorner)
        {
            return (point != null) && (leftTopCorner != null) && (rightBottomCorner != null) &&
                   (point.Lat <= leftTopCorner.Latitude) &&
                   (point.Lat >= rightBottomCorner.Latitude) &&
                   (point.Lon >= leftTopCorner.Longitude) &&
                   (point.Lon <= rightBottomCorner.Longitude);
        }

        #endregion

        #region SELECTED POINT ROUTINES

        private void TransporizePoints(IGeoPoint except)
        {
            foreach (var point in _pointsOnMap.ToList())
            {
                if (point == except) continue;

                SetPointOpacity(point, PointTransparensy);
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
}
