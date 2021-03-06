﻿using Rocket.Api;
using Rocket.ViewModels;

namespace Rocket
{
    public class CompositionRoot
    {
        private readonly IApiHandlerFabric _apiHandlerFabric;
        private ICashinMapViewModel _cashinMapViewModel;
        private IGeoLocator _geoLocator;

        public CompositionRoot()
        {
            _apiHandlerFabric = new ApiHandlerFabric();
        }

        public ICashinMapViewModel CashinMapViewModel
        {
            get { return _cashinMapViewModel ?? (_cashinMapViewModel = new CashinMapViewModel(_apiHandlerFabric, GeoLocator)); }
        }

        public IGeoLocator GeoLocator
        {
            get { return _geoLocator ?? (_geoLocator = new GeoLocator()); }
        }
    }
}
