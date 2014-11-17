using System.Device.Location;
using Rocket.Data;

namespace Rocket.ViewModels
{
    public interface IGeoLocator : IViewModelBase
    {
        GeoCoordinate CurrentLocation { get; }
        double DistanceToPoint(IGeoPoint point);
    }
}