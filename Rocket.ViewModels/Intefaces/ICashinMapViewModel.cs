using System.Collections.Generic;
using Rocket.Data;

namespace Rocket.ViewModels
{
    public interface ICashinMapViewModel : IViewModelBase
    {
        List<CashinPoint> CashinPoints { get; }
        CashinPoint SelectedPoint { get; set; }

    }
}