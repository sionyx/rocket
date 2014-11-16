using System.ComponentModel;

namespace Rocket.ViewModels
{
    public interface IViewModelBase : INotifyPropertyChanged
    {
        bool InProgress { get; set; }
        string ProgressText { get; set; }
    }
}