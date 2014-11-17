using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Rocket.Api;
using Rocket.Data;

namespace Rocket.ViewModels
{
    public class CashinMapViewModel : ViewModelBase, ICashinMapViewModel
    {
        private readonly IApiHandlerFabric _apiHandlerFabric;
        private readonly IGeoLocator _geoLocator;

        public CashinMapViewModel(IApiHandlerFabric apiHandlerFabric, IGeoLocator geoLocator)
        {
            _apiHandlerFabric = apiHandlerFabric;
            _geoLocator = geoLocator;

            Load();
        }

        private async void Load()
        {
            ClasterizationCondition = point => point.Type.Equals("mkb");

            InProgress = true;
            ProgressText = "Загрузка точек...";

            var getter = _apiHandlerFabric.CashinPointsGetter();
            CashinPoints = (await getter.GetPointsAsync()).ToList();

            InProgress = false;
        }



        private List<CashinPoint> _cashinPoints;
        public List<CashinPoint> CashinPoints
        {
            get { return _cashinPoints; }
            private set
            {
                _cashinPoints = value;
                NotifyPropertyChanged("CashinPoints");
            }
        }

        private CashinPoint _selectedPoint;

        public CashinPoint SelectedPoint
        {
            get { return _selectedPoint; }
            set
            {
                _selectedPoint = value;
                NotifyPropertyChanged("SelectedPoint");

                DistanceToSelectedPoint = (_selectedPoint != null) ? _geoLocator.DistanceToPoint(_selectedPoint) : 0;
            }
        }

        private Func<CashinPoint, bool> _clasterizationCondition;
        public Func<CashinPoint, bool> ClasterizationCondition
        {
            get { return _clasterizationCondition; }
            private set
            {
                _clasterizationCondition = value;
                NotifyPropertyChanged("ClasterizationCondition");
            }
        }

        private double _distanceToSelectedPoint;

        public double DistanceToSelectedPoint
        {
            get { return _distanceToSelectedPoint; }
            set
            {
                _distanceToSelectedPoint = value;
                NotifyPropertyChanged("DistanceToSelectedPoint");
            }
        }

    }
}
