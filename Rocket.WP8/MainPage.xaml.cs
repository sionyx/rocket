using System.ComponentModel;
using Rocket.ViewModels;

namespace Rocket
{
    public partial class MainPage
    {

        // Constructor
        public MainPage()
        {
            InitializeComponent();

            BackKeyPress += OnBackKeyPress;
        }

        private void OnBackKeyPress(object sender, CancelEventArgs cancelEventArgs)
        {
            var cashinMapViewModel = (DataContext as CashinMapViewModel);
            if (cashinMapViewModel == null) return;

            if (cashinMapViewModel.SelectedPoint != null)
            {
                cashinMapViewModel.SelectedPoint = null;
                cancelEventArgs.Cancel = true;
            }
        }
    }
}