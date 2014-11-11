using System.Diagnostics;
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

        private ApiHandlerFabric _apiHandlerFabric;

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            Loaded += OnLoaded;

            // Sample code to localize the ApplicationBar
            //BuildLocalizedApplicationBar();
        }

        private void OnLoaded(object sender, RoutedEventArgs routedEventArgs)
        {
            Map.Center = new GeoCoordinate(55.751667, 37.617778);
            Map.ZoomLevel = 13;

            ShowMyLocation();
            ShowPins();
        }

        // Sample code for building a localized ApplicationBar
        //private void BuildLocalizedApplicationBar()
        //{
        //    // Set the page's ApplicationBar to a new instance of ApplicationBar.
        //    ApplicationBar = new ApplicationBar();

        //    // Create a new button and set the text value to the localized string from AppResources.
        //    ApplicationBarIconButton appBarButton = new ApplicationBarIconButton(new Uri("/Assets/AppBar/appbar.add.rest.png", UriKind.Relative));
        //    appBarButton.Text = AppResources.AppBarButtonText;
        //    ApplicationBar.Buttons.Add(appBarButton);

        //    // Create a new menu item with the localized string from AppResources.
        //    ApplicationBarMenuItem appBarMenuItem = new ApplicationBarMenuItem(AppResources.AppBarMenuItemText);
        //    ApplicationBar.MenuItems.Add(appBarMenuItem);
        //}
        private void Map_OnZoomLevelChanged(object sender, MapZoomLevelChangedEventArgs e)
        {
            Map.LandmarksEnabled = (Map.ZoomLevel > 17);
            Debug.WriteLine("ZoomLevel: {0}", Map.ZoomLevel);
        }

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

        private async void ShowMyLocation()
        {
            ShowMeButton.Visibility = Visibility.Collapsed;
            var locator = new Geolocator();
            var coordinate = await locator.GetGeopositionAsync();

            ShowMeButton.Visibility = Visibility.Visible;
            _position = CoordinateConverter.ConvertGeocoordinate(coordinate.Coordinate);
            Map.SetView(_position, Map.ZoomLevel);

            var myPos = new Image {Source = new BitmapImage(new Uri("Assets/pin_me.png", UriKind.Relative))};

            var myLocationOverlay = new MapOverlay
            {
                Content = myPos,
                PositionOrigin = new Point(0.5, 0.5),
                GeoCoordinate = _position
            };

            _locationLayer = new MapLayer { myLocationOverlay };
            Map.Layers.Add(_locationLayer);
        }

        private async void ShowPins()
        {
            _pinsLayer = new MapLayer();
            Map.Layers.Add(_pinsLayer);

            _apiHandlerFabric = new ApiHandlerFabric();
            var getter = _apiHandlerFabric.CashinPointsGetter();
            var points = await getter.GetPointsAsync();

            var mkb = points.Where(i => i.Type.Equals("mkb")).ToList();
            var ors = points.Where(i => i.Type.Equals("ors")).ToList();
            var other = points.Where(i => !i.Type.Equals("mkb")).ToList();

            _pointsToDisplay = ors;
            UpdatePointsInView();

            //foreach (var point in ors)
            //{
            //    var pinPosition = new GeoCoordinate(point.Lat, point.Lon);
            //    var pinImage = new Image { Source = new BitmapImage(new Uri(pinImages[point.Type] ?? "Assets/pin_ic.png", UriKind.Relative)) };

            //    var pinOverlay = new MapOverlay
            //    {
            //        Content = pinImage,
            //        PositionOrigin = new Point(0, 1),
            //        GeoCoordinate = pinPosition
            //    };

            //    _pinsLayer.Add(pinOverlay);
            //}

            //var geotools = new GeoTools();
            //var clusters = geotools.ClusterizePoints(ors.OfType<IGeoPoint>().ToList(), 50);

            //foreach (var point in clusters.Points)
            //{
            //    var pinPosition = new GeoCoordinate(point.Lat, point.Lon);
            //    var pinImage = new Image { Source = new BitmapImage(new Uri(pinImages["ors"] ?? "Assets/pin_ic.png", UriKind.Relative)) };

            //    var pinOverlay = new MapOverlay
            //    {
            //        Content = pinImage,
            //        PositionOrigin = new Point(0, 1),
            //        GeoCoordinate = pinPosition
            //    };

            //    _pinsLayer.Add(pinOverlay);
            //}

        }

        private readonly List<CashinPoint> _pointsOnMap = new List<CashinPoint>();
        private List<CashinPoint> _pointsToDisplay;
        private readonly Dictionary<CashinPoint, MapOverlay> _pointOverlays = new Dictionary<CashinPoint, MapOverlay>();

        private readonly Dictionary<string, string> _pinImages = new Dictionary<string, string>
            {
                {"mkb", "Assets/pin_mkb.png"},
                {"ors", "Assets/pin_opc.png"},
                {"intercommerz_office", "Assets/pin_icb.png"},
                {"intercommerz_atm", "Assets/pin_ic.png"}
            };

        public void UpdatePointsInView()
        {
            if (_pointsToDisplay == null) return;



            var pointsCopy = _pointsOnMap.ToList();

            var topLeft = Map.ConvertViewportPointToGeoCoordinate(new Point(0, 0));
            var bottomRight = Map.ConvertViewportPointToGeoCoordinate(new Point(Map.ActualWidth, Map.ActualHeight));


            foreach (var point in pointsCopy)
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
                    var pinImage = new Image { Source = new BitmapImage(new Uri(_pinImages["ors"] ?? "Assets/pin_ic.png", UriKind.Relative)) };

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

        private void Map_OnCenterChanged(object sender, MapCenterChangedEventArgs e)
        {
            UpdatePointsInView();
            //Debug.WriteLine("OnCenterChanged: {0}:{1}", Map.Center.Latitude, Map.Center.Longitude);
        }
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