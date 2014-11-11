using System;
using System.Collections.Generic;
using System.Device.Location;
using System.Diagnostics;
using System.Linq;
using System.Net;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Input;
using System.Windows.Media;
using System.Windows.Media.Imaging;
using System.Windows.Navigation;
using System.Windows.Shapes;
using Windows.Devices.Geolocation;
using Microsoft.Phone.Controls;
using Microsoft.Phone.Maps.Controls;
using Microsoft.Phone.Shell;
using Rocket.Api;
using Rocket.Data;
using Rocket.WP8.Resources;

namespace Rocket.WP8
{
    public partial class MainPage : PhoneApplicationPage
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

            var myPos = new Image();
            myPos.Source = new BitmapImage(new Uri("Assets/pin_me.png", UriKind.Relative));

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
            var points = await getter.GetPointsAsync() as List<CashinPoint>;

            var mkb = points.Where(i => i.Type.Equals("mkb")).ToList();
            var other = points.Where(i => !i.Type.Equals("mkb")).ToList();

            var pinImages = new Dictionary<string, string>
            {
                {"mkb", "Assets/pin_mkb.png"},
                {"ors", "Assets/pin_opc.png"},
                {"intercommerz_office", "Assets/pin_icb.png"},
                {"intercommerz_atm", "Assets/pin_ic.png"}
            };

            foreach (var point in other)
            {
                var pinPosition = new GeoCoordinate(point.Lat, point.Lon);
                var pinImage = new Image { Source = new BitmapImage(new Uri(pinImages[point.Type] ?? "Assets/pin_ic.png", UriKind.Relative)) };

                var pinOverlay = new MapOverlay
                {
                    Content = pinImage,
                    PositionOrigin = new Point(0, 1),
                    GeoCoordinate = pinPosition
                };

                _pinsLayer.Add(pinOverlay);
            }
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