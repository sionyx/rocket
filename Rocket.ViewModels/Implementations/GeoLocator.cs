using System;
using System.Device.Location;
using Windows.Devices.Geolocation;
using Rocket.Data;
using Rocket.Tools.Geo;

namespace Rocket.ViewModels
{
    public class GeoLocator : ViewModelBase, IGeoLocator
    {
        public GeoLocator()
        {
            _currentLocation = new GeoCoordinate(55.751667, 37.617778);
            UpdateLocation();
        }

        private async void UpdateLocation()
        {
            InProgress = true;
            ProgressText = "Определение геоположения";
            var locator = new Geolocator();
            var coordinate = await locator.GetGeopositionAsync();
            InProgress = false;

            CurrentLocation = CoordinateConverter.ConvertGeocoordinate(coordinate.Coordinate);
        }

        public double DistanceToPoint(IGeoPoint point)
        {
            return GeoTools.GeoDistance(point.Lat, point.Lon, CurrentLocation.Latitude, CurrentLocation.Longitude);
        }

        private GeoCoordinate _currentLocation;
        public GeoCoordinate CurrentLocation
        {
            get { return _currentLocation; }
            private set
            {
                _currentLocation = value;
                NotifyPropertyChanged("CurrentLocation");
            }
        }
    }
}
