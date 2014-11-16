namespace Rocket.ViewModels
{
    public class ViewModelBase : BindableBase, IViewModelBase
    {
        private bool _inProgress;
        public bool InProgress
        {
            get { return _inProgress; }
            set 
            { 
                _inProgress = value;
                NotifyPropertyChanged("InProgress");
            }
        }

        private string _progressText;
        public string ProgressText
        {
            get { return _progressText; }
            set
            {
                _progressText = value;
                NotifyPropertyChanged("ProgressText");
            }
        }

    }
}
